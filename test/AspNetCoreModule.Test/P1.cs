// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;
using AspNetCoreModule.Test.WebSocketClient;
using System.Net;
using System.Text;

namespace AspNetCoreModule.Test
{
    public class P1 : IClassFixture<UseLatestAncm>
    {
        

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64)]
        [InlineData(IISConfigUtility.AppPoolSettings.none)]
        public Task E2E(IISConfigUtility.AppPoolSettings appPoolSetting)
        {
            return DoE2ETest(appPoolSetting);
        }
        
        private static async Task DoE2ETest(IISConfigUtility.AppPoolSettings appPoolSetting)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                if (!TestUtility.CleanupTestEnv(ServerType.IIS))
                {
                    return;
                }

                string solutionPath = UseLatestAncm.GetSolutionDirectory();
                string siteName = "StandardTestSite";

                TestUtility.StandardAppRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTestPublishTemp");
                TestUtility.InitializeStandardAppRootPath(false);

                WebSiteContext testsiteContext = new WebSiteContext("localhost", siteName, 1234);
                WebAppContext rootAppContext = new WebAppContext("/", Path.Combine(solutionPath, "test", "WebRoot", "WebSite1"), testsiteContext);
                iisConfig.CreateSite(testsiteContext.SiteName, rootAppContext.PhysicalPath, 555, testsiteContext.TcpPort, rootAppContext.AppPoolName);

                WebAppContext standardTestApp = new WebAppContext("/StandardTestApp", TestUtility.StandardAppRootPath, testsiteContext);
                iisConfig.CreateApp(testsiteContext.SiteName, standardTestApp.Name, standardTestApp.PhysicalPath);

                WebAppContext webSocketApp = new WebAppContext("/webSocket", Path.Combine(solutionPath, "test", "WebRoot", "WebSocket"), testsiteContext);
                iisConfig.CreateApp(testsiteContext.SiteName, webSocketApp.Name, webSocketApp.PhysicalPath);

                if (appPoolSetting == IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64)
                {
                    iisConfig.SetAppPoolSetting(rootAppContext.AppPoolName, IISConfigUtility.AppPoolSettings.enable32BitAppOnWin64, true);
                    Thread.Sleep(500);
                    iisConfig.RecycleAppPool(rootAppContext.AppPoolName);
                    Thread.Sleep(500);
                }

                // Send a request
                await VerifyResponseBody(standardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

                // Verify WebSocket without setting subprotocol
                await VerifyResponseBodyContain(webSocketApp.GetHttpUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

                // Verify WebSocket subprotocol
                await VerifyResponseBodyContain(webSocketApp.GetHttpUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

                // Verify websocket
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    var openingFrame = websocketClient.Connect(standardTestApp.GetHttpUri("websocket"), true, true);
                    Assert.True(((openingFrame.Content.IndexOf("Connection: Upgrade", System.StringComparison.OrdinalIgnoreCase) >= 0)
                               && (openingFrame.Content.IndexOf("Upgrade: Websocket", System.StringComparison.OrdinalIgnoreCase) >= 0)
                               && openingFrame.Content.Contains("HTTP/1.1 101 Switching Protocols")), "Opening handshake");

                    for (int i = 0; i <= 10; i++)
                    {
                        string dataSent = "abcdefghijklmnopqrstuvwxyz0123456789";
                        websocketClient.SendTextData(dataSent);
                        websocketClient.SendPing();
                        websocketClient.SendTextData(dataSent);
                        websocketClient.SendPing();
                        websocketClient.SendPing();
                        websocketClient.SendTextData(dataSent, 0x01);  // 0x01: start of sending partial data
                        websocketClient.SendPing();
                        websocketClient.SendTextData(dataSent, 0x80);  // 0x80: end of sending partial data
                        websocketClient.SendPing();
                        websocketClient.SendPing();
                        websocketClient.SendTextData(dataSent);
                        websocketClient.SendTextData(dataSent);
                        websocketClient.SendTextData(dataSent);
                        websocketClient.SendPing();
                        Thread.Sleep(3000);
                        VerifyDataSentAndReceived(websocketClient);
                    }

                    var closeFrame = websocketClient.Close();
                    Assert.True(closeFrame.FrameType == FrameType.Close, "Closing Handshake");
                }

                // Verify the ANCM event generated
                var result = TestUtility.GetApplicationEvent(1001);
                Assert.True(result.Count > 0, "Verfiy Event log");
            }
        }

        private static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus)
        {
            await DoVerifyResponseBody(uri, expectedResponseBody, null, expectedResponseStatus);
        }

        private static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus)
        {
            await DoVerifyResponseBody(uri, null, expectedStrings, expectedResponseStatus);
        }

        private static void VerifyDataSentAndReceived(WebSocketClientHelper websocketClient)
        {
            bool testResult = false;
            for (int i = 0; i < 3; i++)
            {
                if (DoVerifyDataSentAndReceived(websocketClient) == false)
                {
                    // retrying after 1 second sleeping
                    Thread.Sleep(1000);
                }
                else
                {
                    testResult = true;
                    break;
                }
            }
            Assert.True(testResult, "DoVerifyDataSentAndReceived");
        }

        private static bool DoVerifyDataSentAndReceived(WebSocketClientHelper websocketClient)
        {
            var result = true;
            var sentString = new StringBuilder();
            var recString = new StringBuilder();
            var pingString = new StringBuilder();
            var pongString = new StringBuilder();

            foreach (Frame frame in websocketClient.Connection.DataSent.ToArray())
            {
                if (frame.FrameType == FrameType.Continuation
                    || frame.FrameType == FrameType.SegmentedText
                        || frame.FrameType == FrameType.Text
                            || frame.FrameType == FrameType.ContinuationFrameEnd)
                {
                    sentString.Append(frame.Content);
                }

                if (frame.FrameType == FrameType.Ping)
                {
                    pingString.Append(frame.Content);
                }
            }

            foreach (Frame frame in websocketClient.Connection.DataReceived.ToArray())
            {
                if (frame.FrameType == FrameType.Continuation
                    || frame.FrameType == FrameType.SegmentedText
                        || frame.FrameType == FrameType.Text
                            || frame.FrameType == FrameType.ContinuationFrameEnd)
                {
                    recString.Append(frame.Content);
                }

                if (frame.FrameType == FrameType.Pong)
                {
                    pongString.Append(frame.Content);
                }
            }

            if (sentString.Length == recString.Length && pongString.Length == pingString.Length)
            {
                Assert.True(sentString.Length == recString.Length, "Same size of data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                Assert.True(sentString.ToString() == recString.ToString(), "Same string in sent and received");
                Assert.True(pongString.Length == pingString.Length, "Ping received; Ping (" + pingString.Length + ") and Pong (" + pongString.Length + ")");
                websocketClient.Connection.DataSent.Clear();
                websocketClient.Connection.DataReceived.Clear();
            }
            else
            {
                TestUtility.LogTrace("Retrying...  so far data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                result = false;
            }
            return result;
        }
        
        private static async Task DoVerifyResponseBody(Uri uri, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus)
        {

            // verify Foo
            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromSeconds(5),
            };

            var response = await RetryHelper.RetryRequest(() =>
            {
                return httpClient.GetAsync(string.Empty);
            }, TestUtility.Logger, retryCount: 2);

            string responseText = "NotInitialized";
            string responseStatus = "NotInitialized";
            try
            {
                if (response != null)
                {
                    responseText = await response.Content.ReadAsStringAsync();
                    if (expectedResponseBody != null)
                    {
                        Assert.Equal(expectedResponseBody, responseText);
                    }

                    if (expectedStringsInResponseBody != null)
                    {
                        foreach (string item in expectedStringsInResponseBody)
                        {
                            Assert.True(responseText.Contains(item));
                        }
                    }
                    Assert.Equal(response.StatusCode, expectedResponseStatus);
                    responseStatus = response.StatusCode.ToString();
                }
            }
            catch (XunitException)
            {
                TestUtility.LogWarning(response.ToString());
                TestUtility.LogWarning(responseText);
                TestUtility.LogWarning(responseStatus);
                throw;
            }
        }
    }
}
