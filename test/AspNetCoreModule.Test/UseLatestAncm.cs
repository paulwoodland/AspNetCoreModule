using AspNetCoreModule.Test.Utility;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AspNetCoreModule.FunctionalTests
{
    public class UseLatestAncm : IDisposable
    {
        private readonly string _extractDirectory = null;
        
        // Set this flag true if the nuget package contains out-dated aspnetcore.dll and you want to use the solution output path instead to apply the laetst ANCM files
        public static bool UseSolutionOutputFiles = true; 
        
        public UseLatestAncm()
        {
            // restore IIS AppHostConfig
            IISConfigUtility.RestoreAppHostConfig(true);

            string aspnetCoreModulePackagePath = GetLatestAncmPackage();
            _extractDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            ZipFile.ExtractToDirectory(aspnetCoreModulePackagePath, _extractDirectory);
            InvokeInstallScript();
        }

        private void InvokeInstallScript()
        {
            var solutionRoot = GetSolutionDirectory();
            string outputPath = string.Empty;
            string setupScriptPath = string.Empty;

            if (UseSolutionOutputFiles)
            {
                setupScriptPath = _extractDirectory;
                outputPath = Path.Combine(solutionRoot, "artifacts", "build", "AspNetCore", "bin", "Debug");
            }
            else
            {
                setupScriptPath = Path.Combine(solutionRoot, "tools");
                outputPath = Path.Combine(_extractDirectory, "ancm", "Debug");
            }

            Process p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = $"\"{setupScriptPath}\\installancm.ps1\" \"" + outputPath + "\"";
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
            Process p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = $"\"{_extractDirectory}\\installancm.ps1\" -Rollback";
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
                Directory.Delete(_extractDirectory);
            }
            catch
            {
                // ignore exception which happens while deleting the temporary directory which won'be used anymore
            }
        }
    }
}