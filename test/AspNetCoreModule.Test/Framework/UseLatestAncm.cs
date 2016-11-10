// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using static AspNetCoreModule.Test.Framework.TestUtility;
using System.Threading;

namespace AspNetCoreModule.Test.Framework
{
    public class UseLatestAncm : IDisposable
    {
        private string _setupScriptPath = null;
        public static string Aspnetcore_X64_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express", "aspnetcore_private.dll");
        public static string Aspnetcore_X86_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express", "aspnetcore_private.dll");
        public static string IISExpressAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express", "config", "schema", "aspnetcore_schema.xml");
        public static string IISAspnetcoreSchema_path = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "schema", "aspnetcore_schema.xml");
        
        // Set this flag true if the nuget package contains out-dated aspnetcore.dll and you want to use the solution output path instead to apply the laetst ANCM files
        public static bool UseSolutionOutputFiles = true;
        public static bool ReplaceExistingFiles = false;

        public UseLatestAncm()
        {
            LogTrace("Start of UseLatestAncm");
            UpdateAspnetCoreBinaryFiles();
        }

        public void Dispose()
        {
            LogTrace("End of UseLatestAncm");
            RollbackAspnetCoreBinaryFileChanges();
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
                    throw new ApplicationException("aspnetcore.dll is not available; build aspnetcore.dll for both x86 and x64 and then try again!!!");
                }
            }

            if (ReplaceExistingFiles)
            {
                // invoke installancm.ps1 to replace the existing files
                RunCommand("powershell.exe", $"\"{_setupScriptPath}\\installancm.ps1\" \"" + outputPath + "\" -ForceToBackup");
            }

            // create an extra private copy of the private file on IISExpress directory
            bool updateSuccess = false;
            for (int i = 0; i < 3; i++)
            {
                updateSuccess = false;
                try
                {
                    RestartServices(RestartOption.KillWorkerProcess);
                    RestartServices(RestartOption.StopW3svcStartW3svc);
                    Thread.Sleep(1000);
                    FileCopy(Path.Combine(outputPath, "x64", "aspnetcore.dll"), Aspnetcore_X64_path);
                    if (IsOSAmd64)
                    {
                        FileCopy(Path.Combine(outputPath, "Win32", "aspnetcore.dll"), Aspnetcore_X86_path);
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
                RunCommand("powershell.exe", $"\"{_setupScriptPath}\\installancm.ps1\" -Rollback");
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