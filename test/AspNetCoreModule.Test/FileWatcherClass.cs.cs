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
    public class FileWatcherClass : Testclass
    {
        private const int _repeatCount = 3;

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task AppOfflineTestWithRenaming(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoAppOfflineTestWithRenaming(appPoolBitness);
        }

        private static async Task DoAppOfflineTestWithRenaming(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                string fileContent = "BackEndAppOffline";
                TestEnv.StandardTestApp.CreateFile(new string[] { fileContent }, "app_offline.htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    Thread.Sleep(500);
                    DateTime startTime = DateTime.Now;

                    // verify 503 
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

                    // rename app_offline.htm to _app_offline.htm and verify 200
                    TestEnv.StandardTestApp.MoveFile("app_offline.htm", "_app_offline.htm");
                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // rename back to app_offline.htm
                    TestEnv.StandardTestApp.MoveFile("_app_offline.htm", "app_offline.htm");
                }
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task AppOfflineTestWithUrlRewriteAndDeleting(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoAppOfflineTestWithUrlRewriteAndDeleting(appPoolBitness);
        }

        private static async Task DoAppOfflineTestWithUrlRewriteAndDeleting(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                string fileContent = "BackEndAppOffline2";
                TestEnv.StandardTestApp.CreateFile(new string[] { fileContent }, "app_offline.htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    // verify 503 
                    string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                    await VerifyResponseBody(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

                    // delete app_offline.htm and verify 200 
                    TestEnv.StandardTestApp.DeleteFile("app_offline.htm");
                    string backendProcessId = await GetResponse(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // create app_offline.htm again
                    TestEnv.StandardTestApp.CreateFile(new string[] { fileContent }, "app_offline.htm");
                }
            }
        }
    }
}
