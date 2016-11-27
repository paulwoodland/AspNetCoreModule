// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using System.Threading;
using Microsoft.Extensions.PlatformAbstractions;
using System.Linq;
using static AspNetCoreModule.Test.Framework.TestUtility;
using System.IO.Compression;
using System.Collections.Generic;

namespace AspNetCoreModule.Test.Framework
{
    public class GlobalTestEnvironment : IDisposable
    {
        private string _setupScriptPath = null;
        public static string Aspnetcore_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore_private.dll");
        public static string Aspnetcore_path_original = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "aspnetcore.dll");
        public static string Aspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv", "aspnetcore_private.dll");
        public static string IISExpressAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string IISAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "schema", "aspnetcore_schema.xml");
        public static bool UseSolutionOutputFiles = true;
        public static bool ReplaceExistingFiles = false;
        public static int _referenceCount = 0;
        public static List<string> CleanupQueue = null;
        
        public GlobalTestEnvironment()
        {
            _referenceCount++;

            if (CleanupQueue == null)
            {
                CleanupQueue = new List<string>();
            }

            if (_referenceCount == 1)
            {
                if (Environment.ExpandEnvironmentVariables("%ANCMDebug%").ToLower() == "true")
                {
                    System.Diagnostics.Debugger.Launch();                    
                }

                TestUtility.RestartServices(TestUtility.RestartOption.KillIISExpress);

                TestUtility.LogTrace("Initializing global test environment");
                try
                {
                    IISConfigUtility.RestoreAppHostConfig();
                }
                catch
                {
                    // ignore
                }

                UpdateAspnetCoreBinaryFiles();
                if (!ReplaceExistingFiles)
                {
                    using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                    {
                        iisConfig.AddModule("AspNetCoreModule", Aspnetcore_path, null);
                    }
                }
            }
        }

        public void Dispose()
        {
            _referenceCount--;

            if (_referenceCount == 0)
            {
                TestUtility.RestartServices(TestUtility.RestartOption.KillIISExpress);

                RollbackAspnetCoreBinaryFileChanges();

                foreach (var postfix in CleanupQueue)
                {
                    string siteName = "StandardTestSite" + postfix;
                    string siteRootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "inetpub", postfix);
                    try
                    {
                        using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                        {
                            iisConfig.DeleteSite(siteName);
                            iisConfig.DeleteAppPool(siteName);
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    try
                    {
                        TestUtility.DeleteDirectory(siteRootPath);
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (!ReplaceExistingFiles)
                {
                    using (var iisConfig = new IISConfigUtility(ServerType.IIS))
                    {
                        iisConfig.AddModule("AspNetCoreModule", Aspnetcore_path_original, null);
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

            if (!UseSolutionOutputFiles)
            {
                string aspnetCoreModulePackagePath = GetLatestAncmPackage();
                _setupScriptPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                ZipFile.ExtractToDirectory(aspnetCoreModulePackagePath, _setupScriptPath);

                outputPath = Path.Combine(_setupScriptPath);
            }
            else
            {
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
            }

            if (ReplaceExistingFiles)
            {
                // invoke installancm.ps1 to replace the existing files
                TestUtility.RunCommand("powershell.exe", $"\"{_setupScriptPath}\\installancm.ps1\" \"" + outputPath + "\" -ForceToBackup");
            }

            // create an extra private copy of the private file on IISExpress directory
            bool updateSuccess = false;
            for (int i = 0; i < 3; i++)
            {
                updateSuccess = false;
                try
                {
                    TestUtility.RestartServices(RestartOption.KillWorkerProcess);
                    TestUtility.RestartServices(RestartOption.StopW3svcStartW3svc);
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

        private void RollbackAspnetCoreBinaryFileChanges()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            string setupScriptPath = string.Empty;

            if (ReplaceExistingFiles)
            {
                TestUtility.RunCommand("powershell.exe", $"\"{_setupScriptPath}\\installancm.ps1\" -Rollback");
            }
            try
            {
                Directory.Delete(_setupScriptPath);
            }
            catch
            {
                // ignore exception which happens while deleting the temporary directory which won'be used anymore
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

        private static string GetLatestAncmPackage()
        {
            var solutionRoot = GetSolutionDirectory();
            var buildDir = Path.Combine(solutionRoot, "artifacts", "build");
            var nupkg = Directory.EnumerateFiles(buildDir, "*.nupkg").OrderByDescending(p => p).FirstOrDefault();

            if (nupkg == null)
            {
                throw new Exception("Cannot find the ANCM nuget package, which is expected to be under artifacts\build");
            }

            return nupkg;
        }        
    }
}
