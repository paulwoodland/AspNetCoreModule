// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;
using System.Diagnostics;
using System.Net;
using System.Threading;
using AspNetCoreModule.Test.WebSocketClient;
using System.Text;
using System.IO;
using System.Security.Principal;

namespace AspNetCoreModule.Test
{
    public class TestHelper
    {
        private const int _repeatCount = 3;

        public enum ReturnValueType
        {
            ResponseBody,
            ResponseStatus,
            None
        }

        public static async Task DoRecycleApplicationAfterBeingKilled(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    backendProcess.Kill();
                    Thread.Sleep(500);
                }
            }
        }

        public static async Task DoRecycleApplicationAfterWebConfigUpdated(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                TestEnv.StandardTestApp.RestoreFile("web.config");

            }
        }

        public static async Task DoRecycleApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                    string backendProcessId = await GetResponse(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                TestEnv.StandardTestApp.RestoreFile("web.config");

            }
        }

        public static async Task DoRecycleParentApplicationWithURLRewrite(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                const int repeatCount = 3;
                for (int i = 0; i < repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                    string backendProcessId = await GetResponse(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                    TestEnv.RootAppContext.MoveFile("web.config", "_web.config");
                    Thread.Sleep(500);
                    TestEnv.RootAppContext.MoveFile("_web.config", "web.config");
                }

                // restore web.config
                TestEnv.RootAppContext.RestoreFile("web.config");
            }
        }

        public static async Task DoEnvironmentVariablesTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
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
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    int expectedValue = Convert.ToInt32(totalNumber) + 1;
                    Assert.True(expectedValue.ToString() == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetEnvironmentVariables"), HttpStatusCode.OK)));
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "environmentVariable", new string[] { "ANCMTestBar", "bar" });
                    Thread.Sleep(500);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    expectedValue++;
                    Assert.True("foo" == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("ExpandEnvironmentVariablesANCMTestFoo"), HttpStatusCode.OK)));
                    Assert.True("bar" == (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("ExpandEnvironmentVariablesANCMTestBar"), HttpStatusCode.OK)));
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        public static async Task DoVerifyANCM(IISConfigUtility.AppPoolBitness appPoolBitness, ServerType serverType)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness, serverType))
            {
                string backendProcessId_old = null;

                DateTime startTime = DateTime.Now;

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

        public static async Task DoAppOfflineTestWithRenaming(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                string fileContent = "BackEndAppOffline";
                TestEnv.StandardTestApp.CreateFile(new string[] { fileContent }, "app_offline.htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    Thread.Sleep(500);
                    DateTime startTime = DateTime.Now;

                    // verify 503 
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

                    // rename app_offline.htm to _app_offline.htm and verify 200
                    TestEnv.StandardTestApp.MoveFile("app_offline.htm", "_app_offline.htm");
                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // rename back to app_offline.htm
                    TestEnv.StandardTestApp.MoveFile("_app_offline.htm", "app_offline.htm");
                }
            }
        }

        public static async Task DoAppOfflineTestWithUrlRewriteAndDeleting(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                string backendProcessId_old = null;
                string fileContent = "BackEndAppOffline2";
                TestEnv.StandardTestApp.CreateFile(new string[] { fileContent }, "app_offline.htm");

                for (int i = 0; i < _repeatCount; i++)
                {
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    // verify 503 
                    string urlForUrlRewrite = TestEnv.URLRewriteApp.URL + "/Rewrite2/" + TestEnv.StandardTestApp.URL + "/GetProcessId";
                    await VerifyResponseBody(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), fileContent + "\r\n", HttpStatusCode.ServiceUnavailable);

                    // delete app_offline.htm and verify 200 
                    TestEnv.StandardTestApp.DeleteFile("app_offline.htm");
                    string backendProcessId = await GetResponse(TestEnv.RootAppContext.GetHttpUri(urlForUrlRewrite), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                    Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                    Assert.NotEqual(backendProcessId_old, backendProcessId);
                    backendProcessId_old = backendProcessId;
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                    // create app_offline.htm again
                    TestEnv.StandardTestApp.CreateFile(new string[] { fileContent }, "app_offline.htm");
                }
            }
        }

        public static async Task DoPostMethodTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
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

        public static async Task DoDisableStartUpErrorPageTest(IISConfigUtility.AppPoolBitness appPoolBitness)
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

        public static async Task DoRapidFailsPerMinuteTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfRapidFailsPerMinute)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    bool rapidFailsTriggered = false;
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "rapidFailsPerMinute", valueOfRapidFailsPerMinute);

                    string backendProcessId_old = null;
                    const int repeatCount = 10;

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    for (int i = 0; i < repeatCount; i++)
                    {
                        // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                        TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                        DateTime startTimeInsideLooping = DateTime.Now;
                        Thread.Sleep(500);

                        var statusCode = await GetResponseStatusCode(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"));
                        if (statusCode != HttpStatusCode.OK.ToString())
                        {
                            Assert.True(i >= valueOfRapidFailsPerMinute);
                            Assert.True(i < valueOfRapidFailsPerMinute + 3);
                            rapidFailsTriggered = true;
                            break;
                        }

                        string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                        Assert.NotEqual(backendProcessId_old, backendProcessId);
                        backendProcessId_old = backendProcessId;
                        var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                        Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTimeInsideLooping, backendProcessId));
                        backendProcess.Kill();
                        Thread.Sleep(500);
                    }
                    if (valueOfRapidFailsPerMinute == 0)
                    {
                        Assert.False(rapidFailsTriggered);
                    }
                    else
                    {
                        Assert.True(rapidFailsTriggered);

                        // verify event error log
                        int errorEventId = 1003;
                        string errorMessageContainThis = "'" + valueOfRapidFailsPerMinute + "'"; // part of error message
                        Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), errorEventId, startTime, errorMessageContainThis));
                    }
                }
                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        public static async Task DoProcessesPerApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfProcessesPerApplication)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    DateTime startTime = DateTime.Now;

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processesPerApplication", valueOfProcessesPerApplication);
                    HashSet<int> processIDs = new HashSet<int>();

                    for (int i = 0; i < 20; i++)
                    {
                        string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                        int id = Convert.ToInt32(backendProcessId);
                        if (!processIDs.Contains(id))
                        {
                            processIDs.Add(id);
                        }

                        if (i == (valueOfProcessesPerApplication - 1))
                        {
                            Assert.Equal(valueOfProcessesPerApplication, processIDs.Count);
                        }
                    }

                    Assert.Equal(valueOfProcessesPerApplication, processIDs.Count);
                    foreach (var id in processIDs)
                    {
                        var backendProcess = Process.GetProcessById(id);
                        Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                        Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, id.ToString()));
                    }

                    // reset the value with 1 again
                    processIDs = new HashSet<int>();
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processesPerApplication", 1);
                    Thread.Sleep(3000);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);
                    Thread.Sleep(500);

                    for (int i = 0; i < 20; i++)
                    {
                        string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                        int id = Convert.ToInt32(backendProcessId);
                        if (!processIDs.Contains(id))
                        {
                            processIDs.Add(id);
                        }
                    }
                    Assert.Equal(1, processIDs.Count);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        public static async Task DoStartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
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
            }
        }

        public static async Task DoRequestTimeoutTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:02:00"); // 2 minute

                    Thread.Sleep(500);
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), "Running", HttpStatusCode.OK);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:01:00"); // 1 minute

                    Thread.Sleep(500);
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    await VerifyResponseStatus(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), HttpStatusCode.BadGateway);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "requestTimeout", "00:02:00"); // 2 minute

                    Thread.Sleep(500);
                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep65000"), "Running", HttpStatusCode.OK);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        public static async Task DoShutdownTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness, int valueOfshutdownTimeLimit, int expectedClosingTime)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    // Set new value (10 second) to make the backend process get the Ctrl-C signal and measure when the recycle happens
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "shutdownTimeLimit", valueOfshutdownTimeLimit);
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoClosingTimeSleep20000"), "Running", HttpStatusCode.OK);  // set 20 seconds for closing time sleep
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);
                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));

                    // Set a new value such as 100 to make the backend process being recycled
                    DateTime startTime = DateTime.Now;
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "shutdownTimeLimit", 100);
                    backendProcess.WaitForExit(30000);
                    DateTime endTime = DateTime.Now;
                    var difference = endTime - startTime;
                    Assert.True(difference.Seconds >= expectedClosingTime);
                    Assert.True(difference.Seconds < expectedClosingTime + 3);
                    Assert.True(backendProcessId != await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK));
                    await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }
        public static async Task DoStdoutLogEnabledTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
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
                    Assert.True(TestUtility.RetryHelper((arg1, arg2, arg3) => VerifyApplicationEventLog(arg1, arg2, arg3), 1004, startTime, @"logs\stdout"));
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

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "stdoutLogEnabled", true);

                    Assert.True(backendProcessId != (await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK)));

                    // Verify log file is created now after backend process is recycled
                    Assert.True(TestUtility.RetryHelper(p => { return Directory.GetFiles(p).Length > 0 ? true : false; }, logPath));
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        public static async Task DoProcessPathAndArgumentsTest(IISConfigUtility.AppPoolBitness appPoolBitness, string processPath, string argumentsPrefix)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                {
                    string arguments = argumentsPrefix + TestEnv.StandardTestApp.GetArgumentFileName();
                    string tempProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    var tempBackendProcess = Process.GetProcessById(Convert.ToInt32(tempProcessId));

                    // replace $env with the actual test value
                    if (processPath == "$env")
                    {
                        string tempString = Environment.ExpandEnvironmentVariables("%systemdrive%").ToLower();
                        processPath = Path.Combine(tempBackendProcess.MainModule.FileName).ToLower().Replace(tempString, "%systemdrive%");
                        arguments = TestEnv.StandardTestApp.GetDirectoryPathWith(arguments).ToLower().Replace(tempString, "%systemdrive%");
                    }

                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(500);

                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "processPath", processPath);
                    iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "arguments", arguments);
                    Thread.Sleep(500);

                    // BugBug: Private build of ANCM causes VSJitDebuger and that should be cleaned up here
                    TestUtility.RestartServices(RestartOption.KillVSJitDebugger);
                    Thread.Sleep(500);

                    string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                    Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));
                }

                TestEnv.StandardTestApp.RestoreFile("web.config");
            }
        }

        public static async Task DoWebSocketTest(IISConfigUtility.AppPoolBitness appPoolBitness, string testData)
        {
            using (var TestEnv = new TestEnvironment(appPoolBitness))
            {
                DateTime startTime = DateTime.Now;

                await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

                // Get Process ID
                string backendProcessId = await GetResponse(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);

                // Verify WebSocket without setting subprotocol
                await VerifyResponseBodyContain(TestEnv.WebSocketApp.GetHttpUri("echo.aspx"), new string[] { "Socket Open" }, HttpStatusCode.OK); // echo.aspx has hard coded path for the websocket server

                // Verify WebSocket subprotocol
                await VerifyResponseBodyContain(TestEnv.WebSocketApp.GetHttpUri("echoSubProtocol.aspx"), new string[] { "Socket Open", "mywebsocketsubprotocol" }, HttpStatusCode.OK); // echoSubProtocol.aspx has hard coded path for the websocket server

                // Verify process creation ANCM event log
                Assert.True(TestUtility.RetryHelper((arg1, arg2) => VerifyANCMStartEvent(arg1, arg2), startTime, backendProcessId));

                // Verify websocket 
                using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
                {
                    var frameReturned = websocketClient.Connect(TestEnv.StandardTestApp.GetHttpUri("websocket"), true, true);
                    Assert.True(frameReturned.Content.Contains("Connection: Upgrade"));
                    Assert.True(frameReturned.Content.Contains("Upgrade: Websocket"));
                    Assert.True(frameReturned.Content.Contains("HTTP/1.1 101 Switching Protocols"));

                    VerifySendingWebSocketData(websocketClient, testData);

                    frameReturned = websocketClient.Close();
                    Assert.True(frameReturned.FrameType == FrameType.Close, "Closing Handshake");
                }

                // send a simple request again and verify the response body
                await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri(), "Running", HttpStatusCode.OK);

            }
        }

        private static bool VerifySendingWebSocketData(WebSocketClientHelper websocketClient, string testData)
        {
            bool result = false;

            //
            // send complete or partial text data and ping multiple times
            //
            websocketClient.SendTextData(testData);
            websocketClient.SendPing();
            websocketClient.SendTextData(testData);
            websocketClient.SendPing();
            websocketClient.SendPing();
            websocketClient.SendTextData(testData, 0x01);  // 0x01: start of sending partial data
            websocketClient.SendPing();
            websocketClient.SendTextData(testData, 0x80);  // 0x80: end of sending partial data
            websocketClient.SendPing();
            websocketClient.SendPing();
            websocketClient.SendTextData(testData);
            websocketClient.SendTextData(testData);
            websocketClient.SendTextData(testData);
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
                    TestUtility.LogWarning("Same size of data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                }

                if (sentString.ToString() != recString.ToString())
                {
                    result = false;
                    TestUtility.LogWarning("Not matched string in sent and received");
                }
                if (pongString.Length != pingString.Length)
                {
                    result = false;
                    TestUtility.LogWarning("Ping received; Ping (" + pingString.Length + ") and Pong (" + pongString.Length + ")");
                }
                websocketClient.Connection.DataSent.Clear();
                websocketClient.Connection.DataReceived.Clear();
            }
            else
            {
                TestUtility.LogWarning("Retrying...  so far data sent(" + sentString.Length + ") and received(" + recString.Length + ")");
                result = false;
            }
            return result;
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

        private static bool VerifyANCMStartEvent(DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(1001, startFrom, includeThis);
        }

        private static bool VerifyApplicationEventLog(int eventID, DateTime startFrom, string includeThis)
        {
            return VerifyEventLog(eventID, startFrom, includeThis);
        }

        private static bool VerifyEventLog(int eventId, DateTime startFrom, string includeThis = null)
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

        private static async Task VerifyResponseStatus(Uri uri, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, null);
        }

        private static async Task VerifyResponseBody(Uri uri, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, null);

        }

        private static async Task VerifyPostResponseBody(Uri uri, KeyValuePair<string, string>[] postData, string expectedResponseBody, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, expectedResponseBody, null, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, postData);
        }

        private static async Task VerifyResponseBodyContain(Uri uri, string[] expectedStrings, HttpStatusCode expectedResponseStatus, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            await SendReceive(uri, null, expectedStrings, expectedResponseStatus, ReturnValueType.None, numberOfRetryCount, verifyResponseFlag, null);
        }

        private static async Task<string> GetResponse(Uri uri, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType = ReturnValueType.ResponseBody, int numberOfRetryCount = 2, bool verifyResponseFlag = true)
        {
            return await SendReceive(uri, null, null, expectedResponseStatus, returnValueType, numberOfRetryCount, verifyResponseFlag, null);
        }

        private static async Task<string> GetResponseStatusCode(Uri uri, int numberOfRetryCount = 2)
        {
            return await SendReceive(uri, null, null, HttpStatusCode.OK, ReturnValueType.ResponseStatus, numberOfRetryCount, false, null);
        }
        
        private static async Task<string> SendReceive(Uri uri, string expectedResponseBody, string[] expectedStringsInResponseBody, HttpStatusCode expectedResponseStatus, ReturnValueType returnValueType, int numberOfRetryCount, bool verifyResponseFlag, KeyValuePair<string, string>[] postData)
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
