// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using System.Net;
using System.Threading;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;

namespace AspNetCoreModule.Test
{
    public class ProcessManagerClass : Testclass
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 5)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 2)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 0)]
        public Task RapidFailsPerMinuteTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfRapidFailsPerMinute)
        {
            return DoRapidFailsPerMinuteTest(appPoolBitness, valueOfRapidFailsPerMinute);
        }

        private static async Task DoRapidFailsPerMinuteTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfRapidFailsPerMinute)
        {
            TestEnv.StartTestcase();
            
            TestEnv.SetAppPoolBitness(TestEnv.StandardTestApp.AppPoolName, appPoolBitness);
            TestEnv.ResetAspnetCoreModule(appPoolBitness);
            Thread.Sleep(500);

            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                bool rapidFailsTriggered = false;
                iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "rapidFailsPerMinute", valueOfRapidFailsPerMinute);

                string backendProcessId_old = null;
                const int repeatCount = 10;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    var statusCode = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK, ReturnValueType.ResponseStatus);
                    if (statusCode != HttpStatusCode.OK.ToString())
                    {
                        Assert.True(i >= valueOfRapidFailsPerMinute);
                        Assert.True(i < valueOfRapidFailsPerMinute + 3);
                        rapidFailsTriggered = true;
                        break;
                    }
                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    backendProcess.Kill();
                    Thread.Sleep(500);
                }
                if (valueOfRapidFailsPerMinute == 0)
                {
                    Assert.False(rapidFailsTriggered);
                }
                else
                {
                    Assert.True(rapidFailsTriggered);
                }
            }
            TestEnv.StandardTestApp.RestoreFile("web.config");
            TestEnv.EndTestcase();
        }
    }
}
