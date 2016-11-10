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
using System.Diagnostics;

namespace AspNetCoreModule.Test
{
    public class E2ETestEnv : IDisposable
    {
        public WebSiteContext TestsiteContext;
        public WebAppContext RootAppContext;
        public WebAppContext StandardTestApp;
        public WebAppContext WebSocketApp;
        public TestUtility TestHelper;
        private ILogger _logger;
        private bool _globalSetupFinished = false;        

        public E2ETestEnv()
        {
            P1.TestEnv = this;
        }

        public void GlobalSetup()
        {
            if (_globalSetupFinished)
            {
                return;
            }

            _globalSetupFinished = true;
            TestUtility.LogTrace("Start of E2ETestEnv");
            string solutionPath = UseLatestAncm.GetSolutionDirectory();
            string siteName = "StandardTestSite";
            
            TestsiteContext = new WebSiteContext("localhost", siteName, 1234);
            RootAppContext = new WebAppContext("/", Path.Combine(solutionPath, "test", "WebRoot", "WebSite1"), TestsiteContext);
            string standardAppRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTestPublishTemp");
            TestUtility.InitializeStandardAppRootPath(standardAppRootPath);
            StandardTestApp = new WebAppContext("/StandardTestApp", standardAppRootPath, TestsiteContext);
            WebSocketApp = new WebAppContext("/webSocket", Path.Combine(solutionPath, "test", "WebRoot", "WebSocket"), TestsiteContext);

            _logger = new LoggerFactory()
                    .AddConsole()
                    .CreateLogger(string.Format("P1"));

            TestHelper = new TestUtility(_logger);
            if (!TestHelper.StartTestMachine(ServerType.IIS))
            {
                return;
            }

            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                iisConfig.CreateSite(TestsiteContext.SiteName, RootAppContext.PhysicalPath, 555, TestsiteContext.TcpPort, RootAppContext.AppPoolName);
                iisConfig.CreateApp(TestsiteContext.SiteName, StandardTestApp.Name, StandardTestApp.PhysicalPath);
                iisConfig.CreateApp(TestsiteContext.SiteName, WebSocketApp.Name, WebSocketApp.PhysicalPath);
            }
        }

        public void Setup()
        {
            GlobalSetup();
            TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
        }

        public void SetAppPoolBitness(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                if (appPoolBitness == IISConfigUtility.AppPoolBitness.enable32Bit)
                {
                    iisConfig.AddModule("AspNetCoreModule", UseLatestAncm.Aspnetcore_X86_path, "bitness32");
                    iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", true);
                }
                else
                {
                    iisConfig.AddModule("AspNetCoreModule", UseLatestAncm.Aspnetcore_X64_path, "bitness64");
                    iisConfig.SetAppPoolSetting(RootAppContext.AppPoolName, "enable32BitAppOnWin64", false);
                }
                iisConfig.RecycleAppPool(RootAppContext.AppPoolName);
                Thread.Sleep(500);
            }
        }

        public void Cleanup()
        {
            TestUtility.RestartServices(TestUtility.RestartOption.KillVSJitDebugger);
        }

        public void Dispose()
        {
            TestUtility.LogTrace("End of E2ETestEnv");
            TestHelper.EndTestMachine();
        }
    }

    public class P1 : IClassFixture<UseLatestAncm>, IClassFixture<E2ETestEnv>
    {
        public static E2ETestEnv TestEnv;

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task E2E(IISConfigUtility.AppPoolBitness appPoolSetting)
        {
            return DoE2ETest(appPoolSetting);
        }
        
        private static async Task DoE2ETest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.Setup();

            TestEnv.SetAppPoolBitness(appPoolBitness);

            await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

            // Get Process ID
            string backendProcessId = await GetResponseBody(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);

            // Verify WebSocket without setting subprotocol
            await VerifyResponseBodyContain(TestEnv.WebSocketApp.GetHttpUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

            // Verify WebSocket subprotocol
            await VerifyResponseBodyContain(TestEnv.WebSocketApp.GetHttpUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

            // Verify process creation ANCM event log
            VerifyANCMEventLog(Convert.ToInt32(backendProcessId), TestEnv.TestHelper.StartTime);

            // Verify websocket 
            VerifyWebSocket(TestEnv.StandardTestApp.GetHttpUri("websocket"));

            // Verify AppOffline
            VerifyRecyclingApp(TestEnv.StandardTestApp);

            // send a simple request again and verify the response body
            await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

            TestEnv.Cleanup();
        }

        private static void VerifyWebSocket(Uri uri)
        {
            using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
            {
                var openingFrame = websocketClient.Connect(uri, true, true);
                Assert.True(((openingFrame.Content.IndexOf("Connection: Upgrade", System.StringComparison.OrdinalIgnoreCase) >= 0)
                           && (openingFrame.Content.IndexOf("Upgrade: Websocket", System.StringComparison.OrdinalIgnoreCase) >= 0)
                           && openingFrame.Content.Contains("HTTP/1.1 101 Switching Protocols")), "Opening handshake");

                VerifySendingWebSocketData(websocketClient);

                var closeFrame = websocketClient.Close();
                Assert.True(closeFrame.FrameType == FrameType.Close, "Closing Handshake");
            }
        }

        private static void VerifyANCMEventLog(int backendProcessId, DateTime startFrom)
        {
            var events = TestUtility.GetApplicationEvent(1001, startFrom);
            Assert.True(events.Count > 0, "Verfiy expected event logs");
            bool findEvent = false;
            foreach (string item in events)
            {
                if (item.Contains(backendProcessId.ToString()))
                {
                    findEvent = true;
                    break;
                }
            }
            Assert.True(findEvent, "Verfiy the event log of the target backend process");
        }

        private static void VerifyRecyclingApp(WebAppContext app)
        {
            //var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
            //Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), app.GetProcessFileName().ToLower().Replace(".exe", ""));
        }

        private static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus)
        {
            await DoVerifyResponseBody(uri, expectedResponseBody, null, expectedResponseStatus);
        }

        private static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus)
        {
            await DoVerifyResponseBody(uri, null, expectedStrings, expectedResponseStatus);
        }

        private static async Task<string> GetResponseBody(Uri uri, HttpStatusCode expectedResponseStatus)
        {
            return await DoVerifyResponseBody(uri, null, null, expectedResponseStatus);
        }

        private static async Task<string> DoVerifyResponseBody(Uri uri, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus)
        {
            string responseText = "NotInitialized";
            string responseStatus = "NotInitialized";

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
            return responseText;
        }

        private static bool VerifySendingWebSocketData(WebSocketClientHelper websocketClient)
        {
            bool result = false;

            //
            // send complete or partial text data and ping multiple times
            //
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

            // Verify test result
            for (int i = 0; i < 3; i++)
            {
                if (DoVerifyDataSentAndReceived(websocketClient) == false)
                {
                    // retrying after 1 second sleeping
                    Thread.Sleep(1000);
                }
                else
                {
                    result = true;
                    break;
                }
            }
            return result;
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
                if (sentString.Length != recString.Length)
                {
                    result = false;
                    TestUtility.LogTrace("Same size of data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                }

                if (sentString.ToString() != recString.ToString())
                {
                    result = false;
                    TestUtility.LogTrace("Not matched string in sent and received");
                }
                if (pongString.Length != pingString.Length)
                {
                    result = false;
                    TestUtility.LogTrace("Ping received; Ping (" + pingString.Length + ") and Pong (" + pongString.Length + ")");
                }
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
    }
}
