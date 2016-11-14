﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;

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
        private bool _globalSetupAlreadyCalled = false;        

        public SetupTestEnv()
        {
            Testclass.TestEnv = this;
        }

        public void Dispose()
        {
            TestUtility.LogTrace("End of E2ETestEnv");
            testHelper.EndTestMachine(); 
        }

        public void GlobalSetup()
        {
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
            _globalSetupAlreadyCalled = true;
        }

        public void StartTestcase()
        {
            if (!_globalSetupAlreadyCalled)
            {
                GlobalSetup();
            }
            
            // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
            TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
        }

        public void EndTestcase()
        {
            // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
            TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
        }

        public void SetAppPoolBitness(string appPoolName, IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                if (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                {
                    iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", true);
                }
                else
                {
                    iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", false);
                }
                iisConfig.RecycleAppPool(RootAppContext.AppPoolName);
            }
        }

        public void ResetAspnetCoreModule(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                if (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                {
                    iisConfig.AddModule("AspNetCoreModule", UseLatestAncm.Aspnetcore_X86_path, "bitness32");
                }
                else
                {
                    iisConfig.AddModule("AspNetCoreModule", UseLatestAncm.Aspnetcore_X64_path, "bitness64");
                }
            }
        }
    }
}