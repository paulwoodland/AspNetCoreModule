// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using System.Net;
using System.Diagnostics;

namespace AspNetCoreModule.Test
{
    public class ApplicationClass : Testclass
    {
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
            using (var TestEnv = new SetupTestEnv(appPoolBitness))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    backendProcess.Kill();
                    Thread.Sleep(500);
                }
            }
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
            using (var TestEnv = new SetupTestEnv(appPoolBitness))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                TestEnv.StandardTestApp.RestoreFile("web.config");

            }
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
            using (var TestEnv = new SetupTestEnv(appPoolBitness))
            {   
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                    string backendProcessId = await GetResponse(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                TestEnv.StandardTestApp.RestoreFile("web.config");

            }
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
            using (var TestEnv = new SetupTestEnv(appPoolBitness))
            {   
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                    string backendProcessId = await GetResponse(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    TestEnv.RootAppContext.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    TestEnv.RootAppContext.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                TestEnv.RootAppContext.RestoreFile("web.config");

            }
        }
    }
}
