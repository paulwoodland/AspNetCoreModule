using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Xml;
using System.Management;
using System.Threading;
using Xunit.Abstractions;
using System.Diagnostics;

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
    }
}