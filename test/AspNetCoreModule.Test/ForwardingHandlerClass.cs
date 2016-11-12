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
            TestEnv.StartTestcase();

            TestEnv.SetAppPoolBitness(TestEnv.StandardTestApp.AppPoolName, appPoolBitness);
            TestEnv.ResetAspnetCoreModule(appPoolBitness);
            Thread.Sleep(500);

            var postFormData = new[]
            {
                new KeyValuePair<string, string>("FirstName", "Mickey"),
                new KeyValuePair<string, string>("LastName", "Mouse"),
                new KeyValuePair<string, string>("TestData", testData),
            };
            var expectedResponseBody = "FirstName=Mickey&LastName=Mouse&TestData=" + testData;
            await VerifyPostResponseBody(TestEnv.StandardTestApp.GetHttpUri("EchoPostData"), postFormData, expectedResponseBody, HttpStatusCode.OK);
            
            TestEnv.EndTestcase();
        }        
    }
}
