// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AspNetCoreModule.Test.Framework
{
    public class UseLatestAncm : IDisposable
    {
        private string _setupScriptPath = null;
        
        // Set this flag true if the nuget package contains out-dated aspnetcore.dll and you want to use the solution output path instead to apply the laetst ANCM files
        public static bool UseSolutionOutputFiles = true; 
        
        public UseLatestAncm()
        {
            InvokeInstallScript();
        }

        private void InvokeInstallScript()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            _setupScriptPath = Path.Combine(solutionRoot, "tools"); 

            if (!UseSolutionOutputFiles)
            {
                string aspnetCoreModulePackagePath = GetLatestAncmPackage();
                _setupScriptPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                ZipFile.ExtractToDirectory(aspnetCoreModulePackagePath, _setupScriptPath);

                outputPath = Path.Combine(_setupScriptPath, "ancm", "Debug");
            }
            else
            {
                outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Debug");
            }

            Process p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = $"\"{_setupScriptPath}\\installancm.ps1\" \"" + outputPath + "\" -ForceToBackup";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            string standardOutput = p.StandardOutput.ReadToEnd();
            string standardError = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (standardError != string.Empty)
            {
                throw new Exception("Failed to update ANCM files, StandardError: " + standardError + ", StandardOutput: " + standardOutput);
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

        public void Dispose()
        {
            InvokeRollbackScript();
        }

        private void InvokeRollbackScript()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            string setupScriptPath = string.Empty;

            Process p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = $"\"{_setupScriptPath}\\installancm.ps1\" -Rollback";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            string standardOutput = p.StandardOutput.ReadToEnd();
            string standardError = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (standardError != string.Empty)
            {
                throw new Exception("Failed to restore ANCM files, StandardError: " + standardError + ", StandardOutput: " + standardOutput);
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
    }
}