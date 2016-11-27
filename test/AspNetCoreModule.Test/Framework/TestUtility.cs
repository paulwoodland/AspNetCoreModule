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
using System.Security.Principal;
using System.Security.AccessControl;

namespace AspNetCoreModule.Test.Framework
{
    public enum RestartOption
    {
        CallIISReset,
        StopHttpStartW3svc,
        StopWasStartW3svc,
        StopW3svcStartW3svc,
        KillWorkerProcess,
        KillVSJitDebugger,
        KillIISExpress
    }

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

        public TestUtility(ILogger logger)
        {
            _logger = logger;
        }
        
        public static bool RetryHelper<T> (
                   Func<T, bool> verifier,
                   T arg,
                   Action<Exception> exceptionBlock = null,
                   int retryCount = 3,
                   int retryDelayMilliseconds = 1000
                   )
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    if (verifier(arg))
                        return true;
                }
                catch (Exception exception)
                {
                    exceptionBlock?.Invoke(exception);
                }
                Thread.Sleep(retryDelayMilliseconds);
            }
            return false;
        }

        public static bool RetryHelper<T1, T2>(
                   Func<T1, T2, bool> verifier,
                   T1 arg1,
                   T2 arg2,
                   Action<Exception> exceptionBlock = null,
                   int retryCount = 3,
                   int retryDelayMilliseconds = 1000
                   )
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    if (verifier(arg1, arg2))
                        return true;
                }
                catch (Exception exception)
                {
                    exceptionBlock?.Invoke(exception);
                }
                Thread.Sleep(retryDelayMilliseconds);
            }
            return false;
        }

        public static bool RetryHelper<T1, T2, T3>(
                   Func<T1, T2, T3, bool> verifier,
                   T1 arg1,
                   T2 arg2,
                   T3 arg3,
                   Action<Exception> exceptionBlock = null,
                   int retryCount = 3,
                   int retryDelayMilliseconds = 1000
                   )
        {
            for (var retry = 0; retry < retryCount; ++retry)
            {
                try
                {
                    if (verifier(arg1, arg2, arg3))
                        return true;
                }
                catch (Exception exception)
                {
                    exceptionBlock?.Invoke(exception);
                }
                Thread.Sleep(retryDelayMilliseconds);
            }
            return false;
        }

        public static void GiveWritePermissionTo(string folder, SecurityIdentifier sid)
        {
            DirectorySecurity fsecurity = Directory.GetAccessControl(folder);
            FileSystemAccessRule writerule = new FileSystemAccessRule(sid, FileSystemRights.Write, AccessControlType.Allow);            
            fsecurity.AddAccessRule(writerule);
            Directory.SetAccessControl(folder, fsecurity);
            Thread.Sleep(500);
        }
        
        public static bool IsOSAmd64
        {
            get
            {
                if (Environment.ExpandEnvironmentVariables("%PROCESSOR_ARCHITECTURE%") == "AMD64")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void LogTrace(string format, params object[] parameters)
        {
            if (format != null)
            {
                Logger.LogTrace(format);
            }
        }
        public static void LogError(string format, params object[] parameters)
        {
            if (format != null)
            {
                Logger.LogError(format);
            }
        }
        public static void LogWarning(string format, params object[] parameters)
        {
            if (format != null)
            {
                Logger.LogWarning(format);
            }
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

        public static void FileMove(string from, string to, bool overWrite = true)
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
                File.Move(from, to);
                if (!File.Exists(to))
                {
                    throw new ApplicationException("Failed to rename from : " + from + ", to : " + to);
                }
                if (File.Exists(from))
                {
                    throw new ApplicationException("Failed to rename from : " + from + ", to : " + to);
                }
            }
            else
            {
                throw new ApplicationException("File not found " + from);
            }
        }

        public static void FileCopy(string from, string to, bool overWrite = true, bool ignoreExceptionWhileDeletingExistingFile = false)
        {
            if (overWrite)
            {
                try
                {
                    DeleteFile(to);
                }
                catch
                {
                    if (!ignoreExceptionWhileDeletingExistingFile)
                    {
                        throw;
                    }
                }
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
                LogError("File not found " + from);
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
                LogTrace("Failed to kill process " + processFileName);
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
       

        public static void RestartServices(RestartOption option)
        {
            switch (option)
            {
                case RestartOption.CallIISReset:
                    CallIISReset();
                    break;
                case RestartOption.StopHttpStartW3svc:
                    StopHttp();
                    StartW3svc();
                    break;
                case RestartOption.StopWasStartW3svc:
                    StopWas();
                    StartW3svc();
                    break;
                case RestartOption.StopW3svcStartW3svc:
                    StopW3svc();
                    StartW3svc();
                    break;
                case RestartOption.KillWorkerProcess:
                    KillWorkerProcess();
                    KillIISWorkerProcess();
                    break;
                case RestartOption.KillVSJitDebugger:
                    KillVSJitDebugger();
                    break;
                case RestartOption.KillIISExpress:
                    KillIISExpress();
                    break;
            };
        }

        public static void KillIISExpress()
        {
            string query = "Select * from Win32_Process Where Name = \"iisexpress.exe\"";
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
                if (owner != null)
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        bool foundProcess = true;

                        if (String.Compare(argList[0], owner, true) != 0)
                        {
                            foundProcess = false;
                        }
                        if (foundProcess)
                        {
                            obj.InvokeMethod("Terminate", null);
                        }
                    }
                }
                else
                {
                    obj.InvokeMethod("Terminate", null); 
                }
            }
        }

        public static void KillIISWorkerProcess(string owner = null)
        {
            string query = "Select * from Win32_Process Where Name = \"iisexpress.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                if (owner != null)
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        bool foundProcess = true;

                        if (String.Compare(argList[0], owner, true) != 0)
                        {
                            foundProcess = false;
                        }
                        if (foundProcess)
                        {
                            obj.InvokeMethod("Terminate", null);
                        }
                    }
                }
                else
                {
                    obj.InvokeMethod("Terminate", null);
                }
            }
        }

        public static int RunCommand(string fileName, string arguments = null, bool checkStandardError = true, bool waitForExit=true)
        {
            int pid = -1;
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            if (arguments != null)
            {
                p.StartInfo.Arguments = arguments;
            }

            if (waitForExit)
            {
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
            }
            
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;            
            p.Start();            
            pid = p.Id;
            string standardOutput = string.Empty;
            string standardError = string.Empty;
            if (waitForExit)
            {
                standardOutput = p.StandardOutput.ReadToEnd();
                standardError = p.StandardError.ReadToEnd();
                p.WaitForExit();
            }
            if (checkStandardError && standardError != string.Empty)
            {
                throw new Exception("Failed to run " + fileName + " " + arguments + ", Error: " + standardError + ", StandardOutput: " + standardOutput);
            }
            return pid;  
        }

        public static void CallIISReset()
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
            string solutionPath = GlobalTestEnvironment.GetSolutionDirectory();
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

        public static List<String> GetApplicationEvent(int id, DateTime startFrom)
        {
            var result = new List<String>();
            for (int i = 0; i < 5; i++)
            {
                LogTrace("Waiting 1 seconds for eventlog to update...");
                Thread.Sleep(1000);
                EventLog systemLog = new EventLog("Application");
                foreach (EventLogEntry entry in systemLog.Entries)
                {
                    if (entry.TimeWritten > startFrom &&  entry.InstanceId == id)
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