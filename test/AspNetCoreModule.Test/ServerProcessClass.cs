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
    public class ServerProcessClass : Testclass
    {
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(IISConfigUtility.AppPoolBitness.noChange)]
        public Task StartupTimeLimitTest(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            return DoStartupTimeLimit(appPoolBitness);
        }

        private static async Task DoStartupTimeLimit(IISConfigUtility.AppPoolBitness appPoolBitness)
        {
            TestEnv.StartTestcase();
            TestEnv.SetAppPoolBitness(TestEnv.StandardTestApp.AppPoolName, appPoolBitness);
            TestEnv.ResetAspnetCoreModule(appPoolBitness);
            Thread.Sleep(500);

            await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep3000"), "Running", HttpStatusCode.OK);

            using (var iisConfig = new IISConfigUtility(ServerType.IIS))
            {
                iisConfig.SetANCMConfig(TestEnv.TestsiteContext.SiteName, TestEnv.StandardTestApp.Name, "startupTimeLimit", 1);
            }

            await VerifyResponseBody(TestEnv.StandardTestApp.GetHttpUri("DoSleep3000"), "Running", HttpStatusCode.InternalServerError);

            TestEnv.StandardTestApp.RestoreFile("web.config");
            TestEnv.EndTestcase();
        }        
    }
}
