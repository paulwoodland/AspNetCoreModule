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

namespace AspNetCoreModule.Test
{
    public class AspnetCoreConfigClass : Testclass
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task EnvironmentVariablesTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoEnvironmentVariablesTest(appPoolBitness);
        }

        private static async Task DoEnvironmentVariablesTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new SetupTestEnv(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string totalNumber = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetEnvironmentVariables"), HttpStatusCode.OK);
                    Assert.True(totalNumber == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetEnvironmentVariables"), HttpStatusCode.OK)));

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "environmentVariable", new string[] { "ANCMTestFoo", "foo" });
                    Thread.Sleep(500);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    int expectedValue = Convert.ToInt32(totalNumber) + 1;
                    Assert.True(expectedValue.ToString() == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetEnvironmentVariables"), HttpStatusCode.OK)));
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "environmentVariable", new string[] { "ANCMTestBar", "bar" });
                    Thread.Sleep(500);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);

                    expectedValue++;
                    Assert.True("foo" == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("ExpandEnvironmentVariablesANCMTestFoo"), HttpStatusCode.OK)));
                    Assert.True("bar" == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("ExpandEnvironmentVariablesANCMTestBar"), HttpStatusCode.OK)));
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }
    }
}
