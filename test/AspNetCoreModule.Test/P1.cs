// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;
using AspNetCoreModule.Test.WebSocketClient;
using System.Net;

namespace AspNetCoreModule.Test
{
    public class P1 : IClassFixture<UseLatestAncm>
    {
        private static string _publishedApplicationRootPath = null;
        public static string PublishedApplicationRootPath
        {
            get
            {
                if (_publishedApplicationRootPath == null)
                {
                    // initialize _publishedApplicationRootPath
                    string solutionPath = UseLatestAncm.GetSolutionDirectory();
                    _publishedApplicationRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTestPublishTemp");
                }

                bool IsApplicationRootPathAvailable = false;

                // check the existing directory is created today; if not, delete the old directory
                if (Directory.Exists(_publishedApplicationRootPath))
                {
                    string webConfigFile = Path.Combine(_publishedApplicationRootPath, "web.config");
                    if (File.Exists(webConfigFile) && (File.GetCreationTime(webConfigFile).Date == DateTime.Today))
                    {
                        IsApplicationRootPathAvailable = true;
                    }
                    else
                    {
                        TestUtility.DeleteDirectory(_publishedApplicationRootPath);
                    }
                }

                // if _publishedApplicationRootPath does not exist, create a new one with using IIS deployer
                if (!IsApplicationRootPathAvailable)
                {
                    var serverType = ServerType.IIS;
                    var architecture = RuntimeArchitecture.x64;
                    var applicationType = ApplicationType.Portable;
                    var runtimeFlavor = RuntimeFlavor.CoreClr;
                    string applicationPath = TestUtility.GetApplicationPath(applicationType);
                    string testSiteName = "WebSiteTemp001";
                    var deploymentParameters = new DeploymentParameters(applicationPath, serverType, runtimeFlavor, architecture)
                    {
                        ApplicationBaseUriHint = "http://localhost:5093",
                        EnvironmentName = "Response",
                        ServerConfigTemplateContent = TestUtility.GetConfigContent(serverType, "Http.config"),
                        SiteName = testSiteName,
                        TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                        ApplicationType = applicationType,
                        PublishApplicationBeforeDeployment = true
                    };
                    var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("P1:{0}:{1}:{2}:{3}", serverType, runtimeFlavor, architecture, applicationType));

                    using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                    {
                        var deploymentResult = deployer.Deploy();
                        TestUtility.DirectoryCopy(deploymentParameters.PublishedApplicationRootPath, _publishedApplicationRootPath);
                    }
                }
                return _publishedApplicationRootPath;
            }
        }
        
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64)]
        [InlineData(IISConfigUtility.AppPoolSettings.none)]
        public Task E2E(IISConfigUtility.AppPoolSettings appPoolSetting)
        {
            return DoE2ETest(appPoolSetting);
        }
        
        private static async Task DoE2ETest(IISConfigUtility.AppPoolSettings appPoolSetting)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                if (!TestUtility.CleanupTestEnv(ServerType.IIS))
                {
                    return;
                }

                string solutionPath = UseLatestAncm.GetSolutionDirectory();
                string siteName = "StandardTestSite";

                WebSiteContext testsiteContext = new WebSiteContext("localhost", siteName, 1234);
                WebAppContext rootAppContext = new WebAppContext("/", Path.Combine(solutionPath, "test", "WebRoot", "WebSite1"), testsiteContext);
                iisConfig.CreateSite(testsiteContext.SiteName, rootAppContext.PhysicalPath, 555, testsiteContext.TcpPort, rootAppContext.AppPoolName);

                WebAppContext standardTestApp = new WebAppContext("/StandardTestApp", PublishedApplicationRootPath, testsiteContext);
                iisConfig.CreateApp(testsiteContext.SiteName, standardTestApp.Name, standardTestApp.PhysicalPath);

                WebAppContext webSocketApp = new WebAppContext("/webSocket", Path.Combine(solutionPath, "test", "WebRoot", "WebSocket"), testsiteContext);
                iisConfig.CreateApp(testsiteContext.SiteName, webSocketApp.Name, webSocketApp.PhysicalPath);

                if (appPoolSetting == IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64)
                {
                    iisConfig.SetAppPoolSetting(rootAppContext.AppPoolName, IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64, true);
                    Thread.Sleep(500);
                    iisConfig.RecycleAppPool(rootAppContext.AppPoolName);
                    Thread.Sleep(500);
                }

                await VerifyResponseBody(standardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

                await VerifyResponseBodyContain(webSocketApp.GetHttpUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

                await VerifyResponseBodyContain(webSocketApp.GetHttpUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

                // Verify websocket
                ConnectionManager cm = new ConnectionManager(standardTestApp.GetHttpUri("websocket"), true);
                cm.Client = new WebSocketClientHelper();
                cm.InitiateWithAlwaysReading();
                Thread.Sleep(1000);

                // Verify the ANCM event
                var result = TestUtility.GetApplicationEvent(1001);
                Assert.True(result.Count > 0, "Verfiy Event log");
            }
        }

        private static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus)
        {
            await DoVerifyResponseBody(uri, expectedResponseBody, null, expectedResponseStatus);
        }

        private static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus)
        {
            await DoVerifyResponseBody(uri, null, expectedStrings, expectedResponseStatus);
        }

        private static async Task DoVerifyResponseBody(Uri uri, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus)
        {

            // verify Foo
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromSeconds(5),
            };

            var response = await RetryHelper.RetryRequest(() =>
            {
                return httpClient.GetAsync(string.Empty);
            }, TestUtility.Logger, retryCount: 2);

            string responseText = "NotInitialized";
            string responseStatus = "NotInitialized";
            try
            {
                if (response != null)
                {
                    responseText = await response.Content.ReadAsStringAsync();
                    if (expectedResponseBody != null)
                    {
                        Assert.Equal(expectedResponseBody, responseText);
                    }

                    if (expectedStringsInResponseBody != null)
                    {
                        foreach (string item in expectedStringsInResponseBody)
                        {
                            Assert.True(responseText.Contains(item));
                        }
                    }
                    Assert.Equal(response.StatusCode, expectedResponseStatus);
                    responseStatus = response.StatusCode.ToString();
                }
            }
            catch (XunitException)
            {
                TestUtility.LogWarning(response.ToString());
                TestUtility.LogWarning(responseText);
                TestUtility.LogWarning(responseStatus);
                throw;
            }
        }
    }
}
