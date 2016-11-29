// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using System.Threading;
using Microsoft.Extensions.PlatformAbstractions;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;

namespace AspNetCoreModule.Test.Framework
{
    public class GlobalTestEnvironment : IDisposable
    {
        public static int SiteId = 81;
        public static string Aspnetcore_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore_private.dll");
        public static string Aspnetcore_path_original = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore.dll");
        public static string Aspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv", "aspnetcore_private.dll");
        public static string IISExpressAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string IISAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "schema", "aspnetcore_schema.xml");
        public static int _referenceCount = 0;

        private static bool _globalTestEnvironmentCompleted = false;
        private string _setupScriptPath = null;

        public GlobalTestEnvironment()
        {
            TestUtility.LogWarning("GlobalTestEnvironment::GlobalTestEnvironment()");

            _referenceCount++;

            if (_referenceCount == 1)
            {
                _globalTestEnvironmentCompleted = false;

                TestUtility.LogWarning("GlobalTestEnvironment::Start of global setup");
                if (Environment.ExpandEnvironmentVariables("%ANCMDebug%").ToLower() == "true")
                {
                    System.Diagnostics.Debugger.Launch();                    
                }

                TestUtility.ResetHelper(ResetHelperMode.KillIISExpress);

                TestUtility.LogWarning("Initializing global test environment");

                // cleanup before starting
                string siteRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", "ANCMTest");
                try
                {
                    if (IISConfigUtility.IsIISInstalled == true)
                    {
                        IISConfigUtility.RestoreAppHostConfig();
                    }
                }
                catch
                {
                    TestUtility.LogWarning("Failed to restore applicationhost.config");
                }
                foreach (string directory in Directory.GetDirectories(siteRootPath))
                {
                    bool successDeleteChildDirectory = true;
                    try
                    {
                        TestUtility.DeleteDirectory(directory);
                    }
                    catch
                    {
                        successDeleteChildDirectory = false;
                        TestUtility.LogWarning("Failed to delete " + directory);
                    }
                    if (successDeleteChildDirectory)
                    {
                        try
                        {
                            TestUtility.DeleteDirectory(siteRootPath);
                        }
                        catch
                        {
                            TestUtility.LogWarning("Failed to delete " + siteRootPath);
                        }
                    }
                }
                
                UpdateAspnetCoreBinaryFiles();

                // update applicationhost.config for IIS server
                if (IISConfigUtility.IsIISInstalled == true)
                {
                    using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                    {
                        iisConfig.AddModule("AspNetCoreModule", Aspnetcore_path, null);
                    }
                }
                
                TestUtility.LogWarning("GlobalTestEnvironment::End of global setup");
                _globalTestEnvironmentCompleted = true;
            }

            for (int i=0; i<120; i++)                    
            {
                if (_globalTestEnvironmentCompleted)
                {
                    break;
                }   
                else
                {
                    TestUtility.LogWarning("GlobalTestEnvironment:: Waiting global setup...");
                    Thread.Sleep(500);
                }                 
            }
            if (!_globalTestEnvironmentCompleted)
            {
                throw new System.ApplicationException("GlobalTestEnvironment is not completed...");
            }
        }

        public void Dispose()
        {
            TestUtility.LogWarning("GlobalTestEnvironment::Dispose()");
            _referenceCount--;

            if (_referenceCount == 0)
            {
                TestUtility.ResetHelper(ResetHelperMode.KillIISExpress);
                
                if (IISConfigUtility.IsIISInstalled == true)
                {
                    using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                    {
                        try
                        {
                            iisConfig.AddModule("AspNetCoreModule", Aspnetcore_path_original, null);
                        }
                        catch
                        {
                            TestUtility.LogWarning("Failed to restore aspnetcore.dll path!!!");
                        }
                    }
                }                
                TestUtility.LogTrace("End of test!!!");
            }
        }
        
        private void UpdateAspnetCoreBinaryFiles()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            _setupScriptPath = Path.Combine(solutionRoot, "tools");

            // First try with debug build
            outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Debug");

            // If debug build does is not available, try with release build
            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Release");
            }

            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                outputPath = Path.Combine(solutionRoot, "src", "AspNetCore", "bin", "Debug");
            }

            if (!File.Exists(Path.Combine(outputPath, "Win32", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore.dll"))
                || !File.Exists(Path.Combine(outputPath, "x64", "aspnetcore_schema.xml")))
            {
                throw new ApplicationException("aspnetcore.dll is not available; build aspnetcore.dll for both x86 and x64 and then try again!!!");
            }
            
            // create an extra private copy of the private file on IISExpress directory
            bool updateSuccess = false;
            for (int i = 0; i < 3; i++)
            {
                updateSuccess = false;
                try
                {
                    TestUtility.ResetHelper(ResetHelperMode.KillWorkerProcess);
                    TestUtility.ResetHelper(ResetHelperMode.StopW3svcStartW3svc);
                    Thread.Sleep(1000);
                    TestUtility.FileCopy(Path.Combine(outputPath, "x64", "aspnetcore.dll"), Aspnetcore_path);
                    if (TestUtility.IsOSAmd64)
                    {
                        TestUtility.FileCopy(Path.Combine(outputPath, "Win32", "aspnetcore.dll"), Aspnetcore_X86_path);
                    }
                    updateSuccess = true;
                }
                catch
                {
                    updateSuccess = false;
                }
                if (updateSuccess)
                {
                    break;
                }
            }
            if (!updateSuccess)
            {
                throw new System.ApplicationException("Failed to update aspnetcore.dll");
            }
        }

        public static string GetSolutionDirectory()
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFile = new FileInfo(Path.Combine(directoryInfo.FullName, "AspNetCoreModule.sln"));
                if (solutionFile.Exists)
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be located using application root {applicationBasePath}.");
        }
    }
}
