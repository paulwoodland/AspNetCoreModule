// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Sdk;
using AspNetCoreModule.Test.WebSocketClient;
using System.Net;
using System.Text;
using System.Diagnostics;

namespace AspNetCoreModule.Test
{
    public class RecycleApplication : FunctionalTetClass
    {
        private const int _repeatCount = 3;

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterBeingKilled(appPoolBitness);
        }

        private static async Task DoRecycleApplicationAfterBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(appPoolBitness);
            string backendProcessId_old = null;
            for (int i = 0; i < _repeatCount; i++)
            {
                // BugBug: VSJitDebugger
                TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);
                string backendProcessId = await GetResponseBody(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                backendProcessId_old = backendProcessId;
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                VerifyANCMEventLog(Convert.ToInt32(backendProcessId), startTime);
                backendProcess.Kill();
                Thread.Sleep(500);
            }
            TestEnv.EndTestcase();
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationAfterWebConfigUpdated(appPoolBitness);
        }

        private static async Task DoRecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(appPoolBitness);
            string backendProcessId_old = null;
            for (int i = 0; i < _repeatCount; i++)
            {
                // BugBug: VSJitDebugger
                TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);
                string backendProcessId = await GetResponseBody(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                backendProcessId_old = backendProcessId;
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                VerifyANCMEventLog(Convert.ToInt32(backendProcessId), startTime);
                TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");
                Thread.Sleep(500);
                TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
            }

            // restore web.config
            TestEnv.StandardTestApp.RestoreFile("web.config");

            TestEnv.EndTestcase();
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationWithURLRewrite(appPoolBitness);
        }

        private static async Task DoRecycleApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(appPoolBitness);
            string backendProcessId_old = null;
            for (int i = 0; i < _repeatCount; i++)
            {
                // BugBug: VSJitDebugger
                TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);
                string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                string backendProcessId = await GetResponseBody(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                backendProcessId_old = backendProcessId;
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                VerifyANCMEventLog(Convert.ToInt32(backendProcessId), startTime);
                TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");
                Thread.Sleep(500);
                TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
            }

            // restore web.config
            TestEnv.StandardTestApp.RestoreFile("web.config");

            TestEnv.EndTestcase();
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleParentApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleParentApplicationWithURLRewrite(appPoolBitness);
        }

        private static async Task DoRecycleParentApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(appPoolBitness);
            string backendProcessId_old = null;
            for (int i = 0; i < _repeatCount; i++)
            {
                // BugBug: VSJitDebugger
                TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);
                string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                string backendProcessId = await GetResponseBody(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                backendProcessId_old = backendProcessId;
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                VerifyANCMEventLog(Convert.ToInt32(backendProcessId), startTime);
                TestEnv.RootAppContext.MoveFile("web.config", "_web.config");
                Thread.Sleep(500);
                TestEnv.RootAppContext.MoveFile("_web.config", "web.config");
            }

            // restore web.config
            TestEnv.RootAppContext.RestoreFile("web.config");

            TestEnv.EndTestcase();
        }
    }
}
