// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Management;
using System.Threading;
using System.Diagnostics;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace AspNetCoreModule.Test.Framework
{
    public class TestUtility
    {
        public static ILogger _logger = null;
        public static ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger("TestUtility");
                }
                return _logger;
            }
        }

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }
        
        public static void InitializeStandardAppRootPath(string outputPath)
        {
            if (Directory.Exists(outputPath))
            {
                DeleteDirectory(outputPath);                
            }

            string appPath = TestUtility.GetApplicationPath(ApplicationType.Portable);
            string publishPath = Path.Combine(appPath, "bin", "Debug", "netcoreapp1.0", "publish");                
            RunCommand("dotnet", "publish " + appPath);
            DirectoryCopy(publishPath, outputPath);
        }
        
        public static void LogTrace(string format, params object[] parameters)
        {
            Logger.LogTrace(format);
        }
        public static void LogError(string format, params object[] parameters)
        {
            Logger.LogError(format);
        }
        public static void LogWarning(string format, params object[] parameters)
        {
            Logger.LogWarning(format);
        }

        public static bool CleanupTestEnv(ServerType serverType)
        {
            // clear Event logs
            TestUtility.ClearApplicationEventLog();

            using (var iisConfig = new IISConfigUtility(serverType))
            {
                try
                {
                    IISConfigUtility.RestoreAppHostConfig(false);
                }
                catch
                {
                    // ignore this initial exception
                }

                if (!iisConfig.IsIISInstalled())
                {
                    LogTrace("IIS is not installed on this machine. Skipping!!!");
                    return false;
                }

                if (!iisConfig.IsUrlRewriteInstalledForIIS())
                {
                    LogTrace("IIS UrlRewrite module is not installed on this machine. Skipping!!!");
                    return false;
                }

                // kill vsjitdebugger processes if it happened in the previous test
                TestUtility.RestartServices(5);

                // kill IIS worker processes to unlock file handle for applicationhost.config file
                TestUtility.RestartServices(4);

                // restore the applicationhost.config file with the backup file; if the backup file does not exist, it will be created here as well.
                IISConfigUtility.RestoreAppHostConfig(true);

                // start DefaultAppPool in case it is stopped
                iisConfig.StartAppPool(IISConfigUtility.Strings.DefaultAppPool);

                // start w3svc service in case it is not started
                TestUtility.StartW3svc();

                if (iisConfig.GetServiceStatus("w3svc") != "Running")
                {
                    LogTrace("w3svc service is not runing. Skipping!!!");
                    return false;
                }
            }
            return true;
        }
        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                RunCommand("cmd.exe", "/c del \"" + filePath + "\"");                
            }
            if (File.Exists(filePath))
            {
                throw new ApplicationException("Failed to delete file: " + filePath);
            }
        }
        
        public static void FileCopy(string from, string to, bool overWrite = true)
        {
            if (overWrite)
            {
                DeleteFile(to);
            }
            if (File.Exists(from))
            {
                if (File.Exists(to) && overWrite == false)
                {
                    return;                
                }
                RunCommand("cmd.exe", "/c copy /y \"" + from + "\" \"" + to + "\"");

                if (!File.Exists(to))
                {
                    throw new ApplicationException("Failed to move from : " + from + ", to : " + to);
                }
            }
            else
            {
                TestUtility.LogError("File not found " + from);
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                RunCommand("cmd.exe", "/c rd \"" + directoryPath + "\" /s /q");                
            }
            if (Directory.Exists(directoryPath))
            {
                throw new ApplicationException("Failed to delete directory: " + directoryPath);
            }
        }

        public static void CreateDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                RunCommand("cmd.exe", "/c md \"" + directoryPath + "\"");                
            }
            if (!Directory.Exists(directoryPath))
            {
                throw new ApplicationException("Failed to create directory: " + directoryPath);
            }
        }

        public static void DirectoryCopy(string from, string to)
        {
            if (Directory.Exists(to))
            {
                DeleteDirectory(to);
            }

            if (!Directory.Exists(to))
            {
                CreateDirectory(to);
            }

            if (Directory.Exists(from))
            {
                RunCommand("cmd.exe", "/c xcopy \"" + from + "\" \"" + to + "\" /s");                
            }
            else
            {
                LogTrace("Directory not found " + from);
            }
        }

        public static string FileReadAllText(string file)
        {
            string result = null;
            if (File.Exists(file))
            {
                result = File.ReadAllText(file);
            }
            return result;
        }

        public static void CreateFile(string file, string[] stringArray)
        {
            DeleteFile(file);
            using (StreamWriter sw = new StreamWriter(file))
            {
                foreach (string line in stringArray)
                {
                    sw.WriteLine(line);
                }
            }

            if (!File.Exists(file))
            {
                throw new ApplicationException("Failed to create " + file);
            }
        }

        public static void KillProcess(string processFileName)
        {
            string query = "Select * from Win32_Process Where Name = \"" + processFileName + "\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            foreach (ManagementObject obj in processList)
            {
                obj.InvokeMethod("Terminate", null);
            }
            Thread.Sleep(1000);

            processList = searcher.Get();
            if (processList.Count > 0)
            {
                TestUtility.LogTrace("Failed to kill process " + processFileName);
            }            
        }

        public static int GetNumberOfProcess(string processFileName, int expectedNumber = 1, int retry = 0)
        {
            int result = 0;
            string query = "Select * from Win32_Process Where Name = \"" + processFileName + "\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            result = processList.Count;
            for (int i = 0; i < retry; i++)
            {
                if (result == expectedNumber)
                {
                    break;
                }
                Thread.Sleep(1000);
                processList = searcher.Get();
                result = processList.Count;
            }
            return result;
        }

        public static string GetHttpUri(string Url, WebSiteContext siteContext)
        {
            string tempUrl = Url.TrimStart(new char[] { '/' });
            return "http://" + siteContext.HostName + ":" + siteContext.TcpPort + "/" + tempUrl;
        }

        public static string XmlParser(string xmlFileContent, string elementName, string attributeName, string childkeyValue)
        {
            string result = string.Empty;

            XmlDocument serviceStateXml = new XmlDocument();
            serviceStateXml.LoadXml(xmlFileContent);

            XmlNodeList elements = serviceStateXml.GetElementsByTagName(elementName);
            foreach (XmlNode item in elements)
            {
                if (childkeyValue == null)
                {
                    if (item.Attributes[attributeName].Value != null)
                    {
                        string newValueFound = item.Attributes[attributeName].Value;
                        if (result != string.Empty)
                        {
                            newValueFound += "," + newValueFound;   // make the result value in comma seperated format if there are multiple nodes
                        }
                        result += newValueFound;
                    }
                }
                else
                {
                    //int groupIndex = 0;
                    foreach (XmlNode groupNode in item.ChildNodes)
                    {
                        /*UrlGroup urlGroup = new UrlGroup();
                        urlGroup._requestQueue = requestQueue._requestQueueName;
                        urlGroup._urlGroupId = groupIndex.ToString();

                        foreach (XmlNode urlNode in groupNode)
                            urlGroup._urls.Add(urlNode.InnerText.ToUpper());

                        requestQueue._urlGroupIds.Add(groupIndex);
                        requestQueue._urlGroups.Add(urlGroup);
                        groupIndex++; */
                    }
                }
            }
            return result;
        }

        public static string RandomString(long size)
        {
            var random = new Random((int)DateTime.Now.Ticks);

            var builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }
        
        public static void RestartServices(int option)
        {
            switch (option)
            {
                case 0:
                    RestartIis();
                    break;
                case 1:
                    StopHttp();
                    StartW3svc();
                    break;
                case 2:
                    StopWas();
                    StartW3svc();
                    break;
                case 3:
                    StopW3svc();
                    StartW3svc();
                    break;
                case 4:
                    KillWorkerProcess();
                    break;
                case 5:
                    KillVSJitDebugger();
                    break;
            };
        }

        public static void KillVSJitDebugger()
        {
            string query = "Select * from Win32_Process Where Name = \"vsjitdebugger.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                bool foundProcess = true;
                if (foundProcess)
                {
                    obj.InvokeMethod("Terminate", null);
                }
            }
        }

        public static void KillWorkerProcess(string owner = null)
        {
            string query = "Select * from Win32_Process Where Name = \"w3wp.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    bool foundProcess = true;
                    if (owner != null)
                    {
                        if (String.Compare(argList[0], owner, true) != 0)
                        {
                            foundProcess = false;
                        }
                    }
                    if (foundProcess)
                    {
                        obj.InvokeMethod("Terminate", null);
                    }
                }
            }
        }

        public static void RunCommand(string fileName, string arguments = null, bool checkStandardError = true)
        {
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            if (arguments != null)
            {
                p.StartInfo.Arguments = arguments;
            }
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string standardOutput = p.StandardOutput.ReadToEnd();
            string standardError = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (checkStandardError && standardError != string.Empty)
            {
                throw new Exception("Failed to run " + fileName + " " + arguments + ", Error: " + standardError + ", StandardOutput: " + standardOutput);
            }
        }

        public static void RestartIis()
        {
            RunCommand("iisreset", null, false);            
        }

        public static void StopHttp()
        {
            RunCommand("net", "stop http /y", false);
        }

        public static void StopWas()
        {
            RunCommand("net", "stop was /y", false);
        }

        public static void StartWas()
        {
            RunCommand("net", "start was", false);   
        }

        public static void StopW3svc()
        {
            RunCommand("net", "stop w3svc /y", false);
        }

        public static void StartW3svc()
        {
            RunCommand("net", "start w3svc", false);            
        }

        public static string GetApplicationPath(ApplicationType applicationType)
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;
            string solutionPath = UseLatestAncm.GetSolutionDirectory();
            string applicationPath = string.Empty;
            applicationPath = Path.Combine(solutionPath, "test", "AspNetCoreModule.TestSites.Standard");
            if (applicationType == ApplicationType.Standalone)
            {
                // NA
            }
            return applicationPath;
        }

        public static string GetConfigContent(ServerType serverType, string iisConfig)
        {
            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText(iisConfig);
            }
            return content;
        }
        
        public static void ClearApplicationEventLog()
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Clear();
            }
            for (int i = 0; i < 5; i++)
            {
                LogTrace("Waiting 1 seconds for eventlog to clear...");
                Thread.Sleep(1000);
                EventLog systemLog = new EventLog("Application");
                if (systemLog.Entries.Count == 0)
                {
                    break;
                }
            }
        }

        public static List<String> GetApplicationEvent(int id)
        {
            var result = new List<String>();
            for (int i = 0; i < 5; i++)
            {
                LogTrace("Waiting 1 seconds for eventlog to update...");
                Thread.Sleep(1000);
                EventLog systemLog = new EventLog("Application");
                foreach (EventLogEntry entry in systemLog.Entries)
                {
                    if (entry.InstanceId == id)
                    {
                        result.Add(entry.ReplacementStrings[0]);
                    }
                }
                if (result.Count > 0)
                {
                    break;
                }
            }
            return result;
        }

        public static string ConvertToPunycode(string domain)
        {
            Uri uri = new Uri("http://" + domain);
            return uri.DnsSafeHost;
        }
    }
}