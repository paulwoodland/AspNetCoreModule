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
    public class ServerProcessClass : Testclass
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task StartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoStartupTimeLimitTest(appPoolBitness);
        }

        private static async Task DoStartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:01:00"); // 1 minute
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep3000"), "Running", HttpStatusCode.OK);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "startupTimeLimit", 1);  // 1 second
                    await VerifyResponseStatus(TestEnv.StandardTestApp.GetHttpUri("DoSleep3000"), HttpStatusCode.BadGateway);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "startupTimeLimit", 10); // 10 seconds
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep3000"), "Running", HttpStatusCode.OK);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RequestTimeoutTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRequestTimeoutTest(appPoolBitness);
        }

        private static async Task DoRequestTimeoutTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:02:00"); // 2 minute

                    Thread.Sleep(500);
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), "Running", HttpStatusCode.OK);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:01:00"); // 1 minute

                    Thread.Sleep(500);
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    await VerifyResponseStatus(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), HttpStatusCode.BadGateway);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:02:00"); // 2 minute

                    Thread.Sleep(500);
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), "Running", HttpStatusCode.OK);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 25, 19)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 25, 19)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 5, 4)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 5, 4)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, 0, 0)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, 0, 0)]
        public Task ShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime)
        {
            return DoShutdownTimeLimitTest(appPoolBitness, valueOfshutdownTimeLimit, expectedClosingTime);
        }

        private static async Task DoShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    // Set new value (10 second) to make the backend process get the Ctrl-C signal and measure when the recycle happens
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "shutdownTimeLimit", valueOfshutdownTimeLimit);
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoClosingTimeSleep20000"), "Running", HttpStatusCode.OK);  // set 20 seconds for closing time sleep
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);
                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));

                    // Set a new value such as 100 to make the backend process being recycled
                    DateTime startTime = DateTime.Now;
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "shutdownTimeLimit", 100);
                    backendProcess.WaitForExit(30000);
                    DateTime endTime = DateTime.Now;
                    var difference = endTime - startTime;
                    Assert.True(difference.Seconds >= expectedClosingTime);
                    Assert.True(difference.Seconds < expectedClosingTime + 3);
                    Assert.True(backendProcessId != await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK));
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task StdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoStdoutLogEnabledTest(appPoolBitness);
        }

        private static async Task DoStdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                TestEnv.StandardTestApp.DeleteDirectory("logs");

                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "stdoutLogEnabled", true);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "stdoutLogFile", @".\logs\stdout");

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    string logPath = TestEnv.StandardTestApp.GetDirectoryPathWith("logs");
                    Assert.False(Directory.Exists(logPath));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), 1004, startTime, @"logs\stdout"));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    TestEnv.StandardTestApp.CreateDirectory("logs");

                    // verify the log file is not created because backend process is not recycled
                    Assert.True(Directory.GetFiles(logPath).Length == 0);
                    Assert.True(backendProcessId == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK)));

                    // reset web.config to recycle backend process and give write permission to the Users local group to which IIS workerprocess identity belongs
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    TestUtility.GiveWritePermissionTo(logPath, sid);

                    startTime = DateTime.Now;
                    Thread.Sleep(500);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "stdoutLogEnabled", false);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "stdoutLogEnabled", true);

                    Assert.True(backendProcessId != (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK)));

                    // Verify log file is created now after backend process is recycled
                    Assert.True(TestUtility.RetryHelper(p => { return Directory.GetFiles(p).Length > 0 ? true : false; }, logPath));
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "dotnet.exe", "./")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "dotnet", @".\")]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "$env", "")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "$env", "")]
        public Task ProcessPathAndArgumentsTest(IISConfigUtility.AppPoolBitness appPoolBitness, string processPath, string argumentsPrefix)
        {
            return DoProcessPathAndArgumentsTest(appPoolBitness, processPath, argumentsPrefix);
        }

        private static async Task DoProcessPathAndArgumentsTest(IISConfigUtility.AppPoolBitness appPoolBitness, string processPath, string argumentsPrefix)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    string arguments = argumentsPrefix + TestEnv.StandardTestApp.GetArgumentFileName();
                    string tempProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var tempBackendProcess = Process.GetProcessById(Convert.ToInt32(tempProcessId));

                    // replace $env with the actual test value
                    if (processPath == "$env")
                    {
                        string tempString = Environment.ExpandEnvironmentVariables("%systemdrive%").ToLower();
                        processPath = Path.Combine(tempBackendProcess.MainModule.FileName).ToLower().Replace(tempString, "%systemdrive%");
                        arguments = TestEnv.StandardTestApp.GetDirectoryPathWith(arguments).ToLower().Replace(tempString, "%systemdrive%");
                    }

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processPath", processPath);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "arguments", arguments);
                    Thread.Sleep(500);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }
    }
}
