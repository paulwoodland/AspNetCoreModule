using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Management;
using System.Threading;
using Xunit.Abstractions;
using System.Diagnostics;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.PlatformAbstractions;

namespace AspNetCoreModule.Test.Utility
{
    public class TestUtility
    {
        public static bool DisplayLog = true;
        public static ITestOutputHelper TestOutputHelper;
        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("cmd.exe", "/c del \"" + filePath + "\"");
                Process myProc = Process.Start(myProcessStartInfo);
                myProc.WaitForExit();
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
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("cmd.exe", "/c copy /y \"" + from + "\" \"" + to + "\"");
                Process myProc = Process.Start(myProcessStartInfo);
                myProc.WaitForExit();

                if (!File.Exists(to))
                {
                    throw new ApplicationException("Failed to move from : " + from + ", to : " + to);
                }
            }
            else
            {
                TestUtility.LogMessage("File not found " + from);
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("cmd.exe", "/c rm  \"" + directoryPath + "\" /s");
                Process myProc = Process.Start(myProcessStartInfo);
                myProc.WaitForExit();
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
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("cmd.exe", "/c md \"" + directoryPath + "\"");
                Process myProc = Process.Start(myProcessStartInfo);
                myProc.WaitForExit();
            }
            if (!Directory.Exists(directoryPath))
            {
                throw new ApplicationException("Failed to create directory: " + directoryPath);
            }
        }

        public static void DirectoryCopy(string from, string to)
        {
            if (!Directory.Exists(to))
            {
                DeleteDirectory(to);
            }

            if (!Directory.Exists(to))
            {
                CreateDirectory(to);
            }

            if (Directory.Exists(from))
            {
                ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("cmd.exe", "/c xcopy \"" + from + "\" \"" + to + "\" /s");
                Process myProc = Process.Start(myProcessStartInfo);
                myProc.WaitForExit();
            }
            else
            {
                TestUtility.LogMessage("Directory not found " + from);
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
                TestUtility.LogMessage("Failed to kill process " + processFileName);
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

        public static string GetHttpUri(string Url, SiteContext siteContext)
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

        public static void LogVerbose(string format, params object[] parameters)
        {
            if (DisplayLog)
            {
                TestOutputHelper.WriteLine(format, parameters);
            }
        }

        public static void LogMessage(string message)
        {
            //TestOutputHelper.WriteLine(message);
        }

        public static void LogFail(string message)
        {
            throw new ApplicationException("Not implemented");            
        }
        public static void LogPass(string message)
        {
            throw new ApplicationException("Not implemented");
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

        public static void RestartIis()
        {
            Process myProc = Process.Start("iisreset");
            myProc.WaitForExit();
        }

        public static void StopHttp()
        {
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("net", "stop http /y");
            Process myProc = Process.Start(myProcessStartInfo);
            myProc.WaitForExit();
        }

        public static void StopWas()
        {
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("net", "stop was /y");
            Process myProc = Process.Start(myProcessStartInfo);
            myProc.WaitForExit();
        }

        public static void StartWas()
        {
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("net", "start was");
            Process myProc = Process.Start(myProcessStartInfo);
            myProc.WaitForExit();
        }

        public static void StopW3svc()
        {
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("net", "stop w3svc /y");
            Process myProc = Process.Start(myProcessStartInfo);
            myProc.WaitForExit();
        }

        public static void StartW3svc()
        {
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo("net", "start w3svc");
            Process myProc = Process.Start(myProcessStartInfo);
            myProc.WaitForExit();
        }

        public static string GetApplicationPath(ApplicationType applicationType)
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;
            string solutionPath = UseLatestAncm.GetSolutionDirectory();
            string applicationPath = string.Empty;
            applicationPath = Path.Combine(solutionPath, "test", "AspNetCoreModule.TestSites");
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
        
        public static void ClearANCMEventLog()
        {
            //using (EventLog eventLog = new EventLog("Application"))
            //{
            //    eventLog.Clear();
            //}
            EventLog.DeleteEventSource("IIS AspNetCore Module");
            EventLog.DeleteEventSource("IIS Express AspNetCore Module");            
        }

        public static void VerifyApplicationEvent(int id, string runningMode = null, string configReader = null)
        {
            try
            {
                TestUtility.LogMessage("Waiting 5 seconds for logfile to update...");
                Thread.Sleep(5000);
                EventLog systemLog = new EventLog("Application");
                foreach (EventLogEntry entry in systemLog.Entries)
                {
                    if (entry.InstanceId == id)
                    {
                        if (id != 5211)
                        {
                            TestUtility.LogPass(String.Format("Found EVENT {0}", id));
                            return;
                        }
                        else
                        {
                            if (entry.ReplacementStrings[0] != runningMode || entry.ReplacementStrings[1] != configReader)
                                TestUtility.LogFail(String.Format("EVENT {0} had incorrect properties. RunningMode: {1}, ConfigReader: {2}", id, entry.ReplacementStrings[0], entry.ReplacementStrings[1]));
                            else
                                TestUtility.LogPass(String.Format("Found EVENT {0} with RunningMode {1} and ConfigReader {2}", id, entry.ReplacementStrings[0], entry.ReplacementStrings[1]));
                            return;
                        }
                    }
                }
                TestUtility.LogFail(String.Format("Event {0} not found", id));
            }
            catch (Exception ex)
            {
                TestUtility.LogFail("Verifying events in event log failed:" + ex.ToString());
            }
        }
        
        public static string ConvertToPunycode(string domain)
        {
            Uri uri = new Uri("http://" + domain);
            return uri.DnsSafeHost;
        }
    }
}