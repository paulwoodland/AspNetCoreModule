// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

namespace AspNetCoreModule.Test
{
    public class TestEnvSetup : IDisposable
    {
        public WebSiteContext TestsiteContext;
        public WebAppContext RootAppContext;
        public WebAppContext StandardTestApp;
        public WebAppContext WebSocketApp;
        public WebAppContext URLRewriteApp;
        public TestUtility TestHelper;
        private ILogger _logger;
        private bool _globalSetupFinished = false;        

        public TestEnvSetup()
        {
            FunctionalTetClass.TestEnv = this;
        }

        public void GlobalSetup()
        {
            if (_globalSetupFinished)
            {
                return;
            }

            TestUtility.LogTrace("Start of E2ETestEnv");
            
            //
            // Initialize test machine
            //
            _logger = new LoggerFactory()
                    .AddConsole()
                    .CreateLogger(string.Format("P1"));
            TestHelper = new TestUtility(_logger);
            if (!TestHelper.StartTestMachine(ServerType.IIS))
            {
                return;
            }

            //
            // Initialize context variables
            //
            string solutionPath = UseLatestAncm.GetSolutionDirectory();
            string siteName = "StandardTestSite";
            TestsiteContext = new WebSiteContext("localhost", siteName, 1234);
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
                iisConfig.CreateSite(TestsiteContext.SiteName, RootAppContext.PhysicalPath, 555, TestsiteContext.TcpPort, RootAppContext.AppPoolName);
                iisConfig.CreateApp(TestsiteContext.SiteName, StandardTestApp.Name, StandardTestApp.PhysicalPath);
                iisConfig.CreateApp(TestsiteContext.SiteName, WebSocketApp.Name, WebSocketApp.PhysicalPath);
                iisConfig.CreateApp(TestsiteContext.SiteName, URLRewriteApp.Name, URLRewriteApp.PhysicalPath);
            }
            _globalSetupFinished = true;
        }

        public void Setup()
        {
            GlobalSetup();
            TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
        }

        public void SetAppPoolBitness(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                if (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                {
                    iisConfig.AddModule("AspNetCoreModule", UseLatestAncm.Aspnetcore_X86_path, "bitness32");
                    iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", true);
                }
                else
                {
                    iisConfig.AddModule("AspNetCoreModule", UseLatestAncm.Aspnetcore_X64_path, "bitness64");
                    iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", false);
                }
                iisConfig.RecycleAppPool(RootAppContext.AppPoolName);
                Thread.Sleep(500);
            }
        }

        public void Cleanup()
        {
            TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
        }

        public void Dispose()
        {
            TestUtility.LogTrace("End of E2ETestEnv");
            TestHelper.EndTestMachine();
        }
    }
}
