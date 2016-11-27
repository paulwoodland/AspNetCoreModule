// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace AspNetCoreModule.Test
{
    public class BuildVerificationTest : Testclass
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange, ServerType.IISExpress)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit, ServerType.IISExpress)]
        public Task VerifyANCMOnIISExpress(IISConfigUtility.AppPoolBitness appPoolBitness, ServerType serverType)
        {
            return DoVerifyANCM(appPoolBitness, serverType);
        }

        public async Task DoVerifyANCM(IISConfigUtility.AppPoolBitness appPoolBitness, ServerType serverType)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness, serverType))
            {
                string backendProcessId_old = null;
                
                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);

                string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                var httpClientHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpClientHandler)
                {
                    BaseAddress = TestEnv.StandardTestApp.GetHttpUri(),
                    Timeout = TimeSpan.FromSeconds(5),
                };

                // Invoke given test scenario function
                await CheckChunkedAsync(httpClient, TestEnv.StandardTestApp);
            }
        }

        private static async Task CheckChunkedAsync(HttpClient client, WebAppContext webApp)
        {
            var response = await client.GetAsync(webApp.GetHttpUri("chunked"));
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/chunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                TestUtility.LogWarning(response.ToString());
                TestUtility.LogWarning(responseText);
                throw;
            }
        }

        private static string GetContentLength(HttpResponseMessage response)
        {
            // Don't use response.Content.Headers.ContentLength, it will dynamically calculate the value if it can.
            IEnumerable<string> values;
            return response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out values) ? values.FirstOrDefault() : null;
        }
    }
}
