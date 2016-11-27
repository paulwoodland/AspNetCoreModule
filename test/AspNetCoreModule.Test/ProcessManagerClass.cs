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
using System.Diagnostics;
using System.Collections.Generic;

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
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    bool rapidFailsTriggered = false;
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "rapidFailsPerMinute", valueOfRapidFailsPerMinute);

                    string backendProcessId_old = null;
                    const int repeatCount = 10;

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    for (int i = 0; i < repeatCount; i++)
                    {
                        // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                        TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                        DateTime startTimeInsideLooping = DateTime.Now;
                        Thread.Sleep(500);

                        var statusCode = await GetResponseStatusCode(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"));
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
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTimeInsideLooping, backendProcessId));
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

                        // verify event error log
                        int errorEventId = 1003;
                        string errorMessageContainThis = "'" + valueOfRapidFailsPerMinute + "'"; // part of error message
                        Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));
                    }
                }
                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 10)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 2)]
        public Task ProcessesPerApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfProcessesPerApplication)
        {
            return DoProcessesPerApplicationTest(appPoolBitness, valueOfProcessesPerApplication);
        }

        private static async Task DoProcessesPerApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfProcessesPerApplication)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processesPerApplication", valueOfProcessesPerApplication);
                    HashSet<int> processIDs = new HashSet<int>();

                    for (int i = 0; i < 20; i++)
                    {
                        string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                        int id = Convert.ToInt32(backendProcessId);
                        if (!processIDs.Contains(id))
                        {
                            processIDs.Add(id);
                        }

                        if (i == (valueOfProcessesPerApplication - 1))
                        {
                            Assert.Equal(valueOfProcessesPerApplication, processIDs.Count);
                        }
                    }

                    Assert.Equal(valueOfProcessesPerApplication, processIDs.Count);
                    foreach (var id in processIDs)
                    {
                        var backendProcess = Process.GetProcessById(id);
                        Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, id.ToString()));
                    }

                    // reset the value with 1 again
                    processIDs = new HashSet<int>();
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processesPerApplication", 1);
                    Thread.Sleep(3000);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
                    Thread.Sleep(500);

                    for (int i = 0; i < 20; i++)
                    {
                        string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                        int id = Convert.ToInt32(backendProcessId);
                        if (!processIDs.Contains(id))
                        {
                            processIDs.Add(id);
                        }
                    }
                    Assert.Equal(1, processIDs.Count);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }
    }
}
