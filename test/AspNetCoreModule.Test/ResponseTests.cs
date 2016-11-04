// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;

namespace AspNetCoreModule.Test
{
    public class ResponseTests : IClassFixture<UseLatestAncm>
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5090/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5091/")]
        public Task BasicTest(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(AspNetCoreModule.Test.Framework.IISConfigUtility.AppPoolSettings.none, serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync, ApplicationType.Portable);
        }

        private Task ResponseFormats(object none, ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, Func<HttpClient, ILogger, Task> checkChunkedAsync, ApplicationType portable)
        {
            throw new NotImplementedException();
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")] 
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64, ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5093/")]
        public Task BasicTestForIIS(IISConfigUtility.AppPoolSettings appPoolSetting, ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(appPoolSetting, serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync, ApplicationType.Portable);
        }
        
        public async Task ResponseFormats(IISConfigUtility.AppPoolSettings appPoolSetting, ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, Func<HttpClient, ILogger, Task> scenario, ApplicationType applicationType)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("ResponseFormats:{0}:{1}:{2}:{3}", serverType, runtimeFlavor, architecture, applicationType));

            // initialize TestUtility
            TestUtility.Initialize(logger);

            if (serverType == ServerType.IIS)
            {
                TestUtility.CleanupTestEnv(serverType);
            }

            
            using (logger.BeginScope("ResponseFormatsTest"))
            {
                string applicationPath = TestUtility.GetApplicationPath(applicationType);
                string testSiteName = "ANCMTestSite"; // This is configured in the Http.config
                var deploymentParameters = new DeploymentParameters(applicationPath, serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "Response",
                    ServerConfigTemplateContent = TestUtility.GetConfigContent(serverType, "Http.config"),
                    SiteName = testSiteName, 
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    ApplicationType = applicationType,
                    PublishApplicationBeforeDeployment = true
                };
                
                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    string solutionPath = UseLatestAncm.GetSolutionDirectory();
                    string publishedApplicationRootPathBackup = Path.Combine(solutionPath, ".build", "publishedApplicationRootPath");

                    if (!Directory.Exists(publishedApplicationRootPathBackup))
                    {
                        TestUtility.DirectoryCopy(deploymentParameters.PublishedApplicationRootPath, publishedApplicationRootPathBackup);
                    }
                    var applicationBaseAddress = new Uri(deploymentResult.ApplicationBaseUri);

                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler)
                    {
                        BaseAddress = applicationBaseAddress,
                        Timeout = TimeSpan.FromSeconds(5),
                    };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Running", responseText);
                    }
                    catch (XunitException)
                    {
                        logger.LogWarning(response.ToString());
                        logger.LogWarning(responseText);
                        throw;
                    }

                    // Invoke given test scenario function
                    await scenario(httpClient, logger);

                    if (serverType == ServerType.IIS)
                    {
                        // Start ANCMFunctional test, which is launched only for IIS, not for IISExpress
                        await ANCMFunctionalTestOnIIS(appPoolSetting, publishedApplicationRootPathBackup, deploymentResult);
                    }
                }
            }
        }

        private static async Task ANCMFunctionalTestOnIIS(IISConfigUtility.AppPoolSettings appPoolSetting, string publishedApplicationRootPathBackup, DeploymentResult deploymentResult)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                TestUtility.LogTrace("Scenario 1: Verify the ANCM event");

                var testsiteContext = new WebSiteContext("localhost", "StandardTestSite", 1234);
                string solutionPath = UseLatestAncm.GetSolutionDirectory();
                string webRootPath = Path.Combine(solutionPath, "test", "WebRoot", "WebSite1");

                var rootAppContext = new WebAppContext("/", webRootPath, testsiteContext);
                iisConfig.CreateSite(testsiteContext.SiteName, rootAppContext.PhysicalPath, 555, testsiteContext.TcpPort, rootAppContext.AppPoolName);

                if (appPoolSetting == IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64)
                {
                    iisConfig.SetAppPoolSetting(rootAppContext.AppPoolName, IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64, true);
                    Thread.Sleep(500);
                    iisConfig.RecycleAppPool(rootAppContext.AppPoolName);
                    Thread.Sleep(500);
                }

                var fooApp = new WebAppContext("/foo", publishedApplicationRootPathBackup, testsiteContext);
                iisConfig.CreateApp(testsiteContext.SiteName, fooApp.Name, fooApp.PhysicalPath);

                var baseAddress = fooApp.GetHttpUri();

                var httpClientHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpClientHandler)
                {
                    BaseAddress = baseAddress,
                    Timeout = TimeSpan.FromSeconds(5),
                };

                var response = await RetryHelper.RetryRequest(() =>
                {
                    return httpClient.GetAsync(string.Empty);
                }, TestUtility.Logger, deploymentResult.HostShutdownToken);

                var responseText = await response.Content.ReadAsStringAsync();
                try
                {
                    Assert.Equal("Running", responseText);
                }
                catch (XunitException)
                {
                    TestUtility.LogWarning(response.ToString());
                    TestUtility.LogWarning(responseText);
                    throw;
                }

                var result = TestUtility.GetApplicationEvent(1001);
                Assert.True(result.Count > 0, "Verfiy Event log");
            }
        }

        private static async Task CheckChunkedAsync(HttpClient client, ILogger logger)
        {
            var response = await client.GetAsync("chunked");
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/chunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static string GetContentLength(HttpResponseMessage response)
        {
            // Don't use response.Content.Headers.ContentLength, it will dynamically calculate the value if it can.
            IEnumerable<string> values;
            return response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out values) ? values.FirstOrDefault() : null;
        }
    }
}
