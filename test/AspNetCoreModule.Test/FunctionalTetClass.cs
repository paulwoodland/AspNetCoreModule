﻿// Copyright (c) .NET Foundation. All rights reserved.
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
    public class FunctionalTetClass : IClassFixture<UseLatestAncm>, IClassFixture<TestEnvSetup>
    {
        public static TestEnvSetup TestEnv;
        
        public static void VerifyWebSocket(Uri uri)
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

        public static void VerifyANCMEventLog(int backendProcessId, DateTime startFrom)
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

        public static async Task VerifyResponseStatus(Uri uri, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await DoVerifyResponse(uri, null, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag);
        }

        public static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await DoVerifyResponse(uri, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag);
        }

        public static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await DoVerifyResponse(uri, null, expectedStrings, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag);
        }

        public static async Task<string> GetResponse(Uri uri, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType = ReturnValueType.ResponseBody, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            return await DoVerifyResponse(uri, null, null, expectedResponseStatus, returnValueType, numberOfRetryCount, verifyResponseFlag);
        }
        
        public enum ReturnValueType
        {
            ResponseBody,
            ResponseStatus,
            None
        }
        
        public static async Task<string> DoVerifyResponse(Uri uri, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType, int numberOfRetryCount, bool verifyResponseFlag)
        {
            string result = null;
            string responseText = "NotInitialized";
            string responseStatus = "NotInitialized";

            var httpClientHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromSeconds(5),
            };

            HttpResponseMessage response = null;

            try
            {
                // RetryRequest does not support for 503/500 server error
                if (expectedResponseStatus != HttpStatusCode.ServiceUnavailable && expectedResponseStatus != HttpStatusCode.InternalServerError)
                {
                    response = await RetryHelper.RetryRequest(() =>
                        {
                            return httpClient.GetAsync(string.Empty);
                        }, TestUtility.Logger, retryCount: numberOfRetryCount);
                }
                else
                {
                    response = await httpClient.GetAsync(string.Empty);
                }

                if (response != null)
                {
                    if (verifyResponseFlag)
                    {
                        if (expectedResponseBody != null)
                        {
                            responseText = await response.Content.ReadAsStringAsync();
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
                    
                    switch (returnValueType)
                    {
                        case ReturnValueType.ResponseBody:
                            if (responseText == "NotInitialized")
                            {
                                result = await response.Content.ReadAsStringAsync();
                            }
                            else
                            {
                                result = responseText;
                            }
                            break;
                        case ReturnValueType.ResponseStatus:
                            result = response.StatusCode.ToString();
                            break;
                    }
                }
            }
            catch (XunitException)
            {
                if (response != null)
                {
                    TestUtility.LogWarning(response.ToString());
                }
                TestUtility.LogWarning(responseText);
                TestUtility.LogWarning(responseStatus);

                throw;                
            }
            return result;
        }

        public static bool VerifySendingWebSocketData(WebSocketClientHelper websocketClient)
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

        public static bool DoVerifyDataSentAndReceived(WebSocketClientHelper websocketClient)
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
