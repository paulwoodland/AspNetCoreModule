// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit;
using Xunit.Sdk;
using System.Net;
using AspNetCoreModule.Test.Framework;
using System.Collections.Generic;

namespace AspNetCoreModule.Test
{
    public abstract class Testclass : IClassFixture<GlobalSetup>
    {
        public static bool VerifyANCMStartEvent(DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(1001, startFrom, includeThis);
        }

        public static bool VerifyApplicationEventLog(int eventID, DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(eventID, startFrom, includeThis);
        }

        public static bool VerifyEventLog(int eventId, DateTime startFrom, string includeThis = null)
        {
            var events = TestUtility.GetApplicationEvent(eventId, startFrom);
            Assert.True(events.Count > 0, "Verfiy expected event logs");
            bool findEvent = false;
            foreach (string item in events)
            {
                if (item.Contains(includeThis))
                {
                    findEvent = true;
                    break;
                }
            }
            return findEvent;
        }

        public static async Task VerifyResponseStatus(Uri uri, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, null);
        }                
        public static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, null);

        }
        public static async Task VerifyPostResponseBody(Uri uri, KeyValuePair<string, string>[] postData, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {           
            await SendReceive(uri, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, postData);
        }
        public static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, expectedStrings, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, null);
        }

        public static async Task<string> GetResponse(Uri uri, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType = ReturnValueType.ResponseBody, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            return await SendReceive(uri, null, null, expectedResponseStatus, returnValueType, numberOfRetryCount, verifyResponseFlag, null);
        }

        public static async Task<string> GetResponseStatusCode(Uri uri, int numberOfRetryCount = 2)
        {
            return await SendReceive(uri, null, null, HttpStatusCode.OK, ReturnValueType.ResponseStatus, numberOfRetryCount, false, null);
        }
        
        public enum ReturnValueType
        {
            ResponseBody,
            ResponseStatus,
            None
        }
        
        public static async Task<string> SendReceive(Uri uri, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType, int numberOfRetryCount, bool verifyResponseFlag, KeyValuePair<string, string>[] postData)
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
                FormUrlEncodedContent postHttpContent = null;
                if (postData != null)
                {
                    postHttpContent = new FormUrlEncodedContent(postData);
                }

                // RetryRequest does not support for 503/500 server error
                if (returnValueType != ReturnValueType.ResponseStatus && expectedResponseStatus == HttpStatusCode.OK)
                {
                    if (postData == null)
                    {
                        response = await RetryHelper.RetryRequest(() =>
                            {
                                return httpClient.GetAsync(string.Empty);
                            }, TestUtility.Logger, retryCount: numberOfRetryCount);
                    }
                    else
                    {
                        response = await RetryHelper.RetryRequest(() =>
                        {
                            return httpClient.PostAsync(string.Empty, postHttpContent);
                        }, TestUtility.Logger, retryCount: numberOfRetryCount);
                    }
                }
                else
                {
                    if (postData == null)
                    {
                        response = await httpClient.GetAsync(string.Empty);
                    }
                    else
                    {
                        response = await httpClient.PostAsync(string.Empty, postHttpContent);
                    }
                }

                if (response != null)
                {
                    responseStatus = response.StatusCode.ToString();                    
                    if (verifyResponseFlag)
                    {
                        if (expectedResponseBody != null)
                        {
                            if (responseText == "NotInitialized")
                            {
                                responseText = await response.Content.ReadAsStringAsync();
                            }
                            Assert.Equal(expectedResponseBody, responseText);
                        }

                        if (expectedStringsInResponseBody != null)
                        {
                            if (responseText == "NotInitialized")
                            {
                                responseText = await response.Content.ReadAsStringAsync();
                            }
                            foreach (string item in expectedStringsInResponseBody)
                            {
                                Assert.True(responseText.Contains(item));
                            }
                        }
                        Assert.Equal(response.StatusCode, expectedResponseStatus);
                    }

                    switch (returnValueType)
                    {
                        case ReturnValueType.ResponseBody:
                            if (responseText == "NotInitialized")
                            {
                                responseText = await response.Content.ReadAsStringAsync();
                            }
                            result = responseText;                            
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
    }
}
