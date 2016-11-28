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
using AspNetCoreModule.Test.WebSocketClient;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;

namespace AspNetCoreModule.Test
{
    public class ForwardingHandlerClass : Testclass
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, "abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz0123456789")]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, "a")]
        public Task PostMethodTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            return DoPostMethodTest(appPoolBitness, testData);
        }

        private static async Task DoPostMethodTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                var postFormData = new[]
                {
                    new KeyValuePair<string, string>("FirstName", "Mickey"),
                    new KeyValuePair<string, string>("LastName", "Mouse"),
                    new KeyValuePair<string, string>("TestData", testData),
                };
                var expectedResponseBody = "FirstName=Mickey&LastName=Mouse&TestData=" + testData;
                await VerifyPostResponseBody(TestEnv.StandardTestApp.GetHttpUri("EchoPostData"), postFormData, expectedResponseBody, HttpStatusCode.OK);
            }
        }

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]        
        public Task DisableStartUpErrorPageTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoDisableStartUpErrorPageTest(appPoolBitness);
        }

        private static async Task DoDisableStartUpErrorPageTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            int errorEventId = 1000;
            string errorMessageContainThis = "bogus"; // bogus path value to cause 502.3 error

            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                TestEnv.StandardTestApp.DeleteFile("custom502-3.htm");
                string curstomErrorMessage = "ANCMTest502-3";
                TestEnv.StandardTestApp.CreateFile(new string[] { curstomErrorMessage }, "custom502-3.htm");

                Thread.Sleep(500);

                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.ConfigureCustomLogging(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, 502, 3, "custom502-3.htm");
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "disableStartUpErrorPage", true);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processPath", errorMessageContainThis);

                    var responseBody = await GetResponse(TestEnv.StandardTestApp.GetHttpUri(), HttpStatusCode.BadGateway);
                    responseBody = responseBody.Replace("\r", "").Replace("\n", "").Trim();
                    Assert.True(responseBody == curstomErrorMessage);

                    // verify event error log
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));

                    // try again after setting "false" value
                    startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "disableStartUpErrorPage", false);
                    Thread.Sleep(500);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    responseBody = await GetResponse(TestEnv.StandardTestApp.GetHttpUri(), HttpStatusCode.BadGateway);
                    Assert.True(responseBody.Contains("808681"));

                    // verify event error log
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));
                }
                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }
    }
}
