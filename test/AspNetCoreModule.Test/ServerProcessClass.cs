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
            return DoStartupTimeLimit(appPoolBitness);
        }

        private static async Task DoStartupTimeLimit(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(TestEnv.StandardTestApp.AppPoolName, appPoolBitness);
            TestEnv.ResetAspnetCoreModule(appPoolBitness);
            Thread.Sleep(500);

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
            TestEnv.EndTestcase();
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
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(TestEnv.StandardTestApp.AppPoolName, appPoolBitness);
            TestEnv.ResetAspnetCoreModule(appPoolBitness);
            Thread.Sleep(500);

            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:02:00"); // 2 minute
                await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), "Running", HttpStatusCode.OK);
                iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:01:00"); // 2 minute
                await VerifyResponseStatus(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), HttpStatusCode.BadGateway);
                iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:02:00"); // 2 minute
                await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), "Running", HttpStatusCode.OK);
            }

            TestEnv.StandardTestApp.RestoreFile("web.config");
            TestEnv.EndTestcase();
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
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(TestEnv.StandardTestApp.AppPoolName, appPoolBitness);
            TestEnv.ResetAspnetCoreModule(appPoolBitness);
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
                Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMFailedToCreateLogEvent(arg1, arg2), startTime, @"logs\stdout"));
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
                iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "stdoutLogEnabled", true);
                Assert.True(backendProcessId != (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK)));

                // Verify log file is created now after backend process is recycled
                Assert.True(TestUtility.RetryHelper(p => { return Directory.GetFiles(p).Length > 0 ? true : false; }, logPath));
            }

            TestEnv.StandardTestApp.RestoreFile("web.config");
            TestEnv.EndTestcase();
        }
    }
}
