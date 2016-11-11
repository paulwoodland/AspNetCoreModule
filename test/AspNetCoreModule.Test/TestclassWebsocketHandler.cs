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
    public class TestclassWebsocketHandler : BaseTestclass
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task WebSocketTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoWebSocketTest(appPoolBitness);
        }

        private static async Task DoWebSocketTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            
            TestEnv.SetAppPoolBitness(appPoolBitness);

            await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

            // Get Process ID
            string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);

            // Verify WebSocket without setting subprotocol
            await VerifyResponseBodyContain(TestEnv.WebSocketApp.GetHttpUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

            // Verify WebSocket subprotocol
            await VerifyResponseBodyContain(TestEnv.WebSocketApp.GetHttpUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

            // Verify process creation ANCM event log
            VerifyANCMEventLog(Convert.ToInt32(backendProcessId), TestEnv.testHelper.StartTime);

            // Verify websocket 
            VerifyWebSocket(TestEnv.StandardTestApp.GetHttpUri("websocket"));
            
            // send a simple request again and verify the response body
            await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

            TestEnv.EndTestcase();
        }
    }
}
