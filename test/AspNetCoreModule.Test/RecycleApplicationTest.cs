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
    public class RecycleApplicationTest : FunctionalTest
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.enable32Bit)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task RecycleApp(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoRecycleApp(appPoolBitness);
        }

        private static async Task DoRecycleApp(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.Setup();
            TestEnv.SetAppPoolBitness(appPoolBitness);

            string backendProcessId = await GetResponseBody(TestEnv.StandardTestApp.GetHttpUri("GetProcessId"), HttpStatusCode.OK);
            var backendProcess = Process.GetProcessById(Convert.ToInt32(backendProcessId));
            Assert.Equal(backendProcess.ProcessName.ToLower().Replace(".exe", ""), TestEnv.StandardTestApp.GetProcessFileName().ToLower().Replace(".exe", ""));
            TestEnv.Cleanup();
        }
    }
}
