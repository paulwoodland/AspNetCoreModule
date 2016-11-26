// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Extensions.PlatformAbstractions;
using System.Linq;
using static AspNetCoreModule.Test.Framework.TestUtility;
using System.IO.Compression;

namespace AspNetCoreModule.Test.Framework
{
    public class SetupTestEnv : IDisposable
    {
        public WebSiteContext TestsiteContext;
        public WebAppContext RootAppContext;
        public WebAppContext StandardTestApp;
        public WebAppContext WebSocketApp;
        public WebAppContext URLRewriteApp;
        public TestUtility testHelper;
        private ILogger _logger;

        private static int _tcpPort = 81;

        public void Dispose()
        {
            TestUtility.LogTrace("End of test!!!");
            testHelper.EndTestMachine();
        }


        public SetupTestEnv(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            // 
            // System.Diagnostics.Debugger.Launch();
            //

            TestUtility.LogTrace("Start of E2ETestEnv");

            //
            // Initialize test machine
            //
            _logger = new LoggerFactory()
                    .AddConsole()
                    .CreateLogger(string.Format("P1"));
            testHelper = new TestUtility(_logger);
            if (!testHelper.StartTestMachine(ServerType.IIS))
            {
                throw new System.ApplicationException("Failed to clean up initial test enviornment");
            }

            //
            // Initialize context variables
            //

            string solutionPath = GlobalSetup.GetSolutionDirectory();
            int tcpPort = _tcpPort++; 
            
            string siteName = "StandardTestSite" + tcpPort.ToString();
            TestsiteContext = new WebSiteContext("localhost", siteName, tcpPort);
            RootAppContext = new WebAppContext("/", Path.Combine(solutionPath, "test", "WebRoot", "WebSite1"), TestsiteContext);
            string standardAppRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTestPublishTemp");
            TestUtility.InitializeStandardAppRootPath(standardAppRootPath);
            StandardTestApp = new WebAppContext("/StandardTestApp", standardAppRootPath, TestsiteContext);
            WebSocketApp = new WebAppContext("/WebSocket", Path.Combine(solutionPath, "test", "WebRoot", "WebSocket"), TestsiteContext);
            URLRewriteApp = new WebAppContext("/URLRewriteApp", Path.Combine(solutionPath, "test", "WebRoot", "URLRewrite"), TestsiteContext);

            //
            // Create sites and apps to applicationhost.config
            //
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                iisConfig.CreateAppPool(TestsiteContext.SiteName);
                if (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                {
                    if (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                    {
                        iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", true);
                    }
                    else
                    {
                        iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", false);
                    }
                }
                iisConfig.CreateSite(TestsiteContext.SiteName, RootAppContext.PhysicalPath, 555, TestsiteContext.TcpPort, TestsiteContext.SiteName);
                RootAppContext.RestoreFile("web.config");
                RootAppContext.DeleteFile("app_offline.htm");

                iisConfig.CreateApp(TestsiteContext.SiteName, StandardTestApp.Name, StandardTestApp.PhysicalPath);
                StandardTestApp.RestoreFile("web.config");
                StandardTestApp.DeleteFile("app_offline.htm");

                iisConfig.CreateApp(TestsiteContext.SiteName, WebSocketApp.Name, WebSocketApp.PhysicalPath);
                WebSocketApp.RestoreFile("web.config");
                WebSocketApp.DeleteFile("app_offline.htm");

                iisConfig.CreateApp(TestsiteContext.SiteName, URLRewriteApp.Name, URLRewriteApp.PhysicalPath);
                URLRewriteApp.RestoreFile("web.config");
                URLRewriteApp.DeleteFile("app_offline.htm");
            }
        }
    }
}
