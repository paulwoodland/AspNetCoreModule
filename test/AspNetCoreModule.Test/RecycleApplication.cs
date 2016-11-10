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
    public class RecycleApplication : FunctionalTetClass
    {
        //private const int _repeatCount = 10;
        private const int _repeatCount = 3;

        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApplicationTest(appPoolBitness);
        }

        private static async Task DoRecycleApplicationTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.Setup();
            TestEnv.SetAppPoolBitness(appPoolBitness);

            TestEnv.StandardTestApp.RestoreFile("web.config");
            // recycle application by killing the backend process
            string backendProcessId_old = null;
            for (int i = 0; i < _repeatCount; i++)
            {
                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);
                string backendProcessId = await GetResponseBody(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                backendProcessId_old = backendProcessId;
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                VerifyANCMEventLog(Convert.ToInt32(backendProcessId), startTime);
                backendProcess.Kill();
                Thread.Sleep(500);
            }

            // recycle application by updating web.config
            for (int i = 0; i < _repeatCount; i++)
            {
                DateTime startTime = DateTime.Now;
                Thread.Sleep(500);
                string backendProcessId = await GetResponseBody(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
                var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
                Assert.NotEqual(backendProcessId_old, backendProcessId);
                backendProcessId_old = backendProcessId;
                Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
                VerifyANCMEventLog(Convert.ToInt32(backendProcessId), startTime);
                TestEnv.StandardTestApp.MoveFile("web.config", "_web.config");                
                Thread.Sleep(500);
                TestEnv.StandardTestApp.MoveFile("_web.config", "web.config");
            }
            TestEnv.StandardTestApp.RestoreFile("web.config");

            TestEnv.Cleanup();
        }
    }
}
