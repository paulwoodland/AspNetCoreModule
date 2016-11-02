using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Web.Administration;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace AspNetCoreModule.Test.Utility
{
    public class IISConfigUtility : IDisposable
    {
        public class Strings
        {
            public static string AppHostConfigPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "applicationHost.config");
            public static string IIS64BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv");
            public static string IIS32BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv");
            public static string IISExpress64BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "IIS Express");
            public static string IISExpress32BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "IIS Express");
            public static string DefaultAppPool = "DefaultAppPool";
        }

        public enum AppPoolSettings
        {
            enable32BitAppOnWin64,
            none
        }

        public void Dispose()
        {
        }

        public IISConfigUtility(ServerType type)
        {
            this.ServerType = type;
        }
        public ServerType ServerType = ServerType.IIS;
         
        public ServerManager GetServerManager()
        {
            if (ServerType == ServerType.IISExpress)
            {
                return new ServerManager();
            }
            else
            {
                return new ServerManager(
                    false,                         // readOnly 
                    Strings.AppHostConfigPath      // applicationhost.config path
                );
            }
        }
        
        public void SetAppPoolSetting(string appPoolName, AppPoolSettings attribute, object value)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection applicationPoolsSection = config.GetSection("system.applicationHost/applicationPools");
                ConfigurationElementCollection applicationPoolsCollection = applicationPoolsSection.GetCollection();
                ConfigurationElement addElement = FindElement(applicationPoolsCollection, "add", "name", appPoolName);
                if (addElement == null) throw new InvalidOperationException("Element not found!");
                var attributeName = attribute.ToString();
                addElement[attributeName] = value;
                serverManager.CommitChanges();
            }
        }

        public void RecycleAppPool(string appPoolName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                serverManager.ApplicationPools[appPoolName].Recycle();
            }
        }

        public void StopAppPool(string appPoolName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                serverManager.ApplicationPools[appPoolName].Stop();
            }
        }

        public void StartAppPool(string appPoolName)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                serverManager.ApplicationPools[appPoolName].Start();
            }
        }

        public void CreateSite(string siteName, string physicalPath, int siteId, int tcpPort, string appPoolName = "DefaultAppPool")
        {
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection sitesSection = config.GetSection("system.applicationHost/sites");
                ConfigurationElementCollection sitesCollection = sitesSection.GetCollection();
                ConfigurationElement siteElement = sitesCollection.CreateElement("site");
                siteElement["id"] = siteId;
                siteElement["name"] = siteName;
                ConfigurationElementCollection bindingsCollection = siteElement.GetCollection("bindings");

                ConfigurationElement bindingElement = bindingsCollection.CreateElement("binding");
                bindingElement["protocol"] = @"http";
                bindingElement["bindingInformation"] = "*:" + tcpPort + ":";
                bindingsCollection.Add(bindingElement);

                ConfigurationElementCollection siteCollection = siteElement.GetCollection();
                ConfigurationElement applicationElement = siteCollection.CreateElement("application");
                applicationElement["path"] = @"/";
                applicationElement["applicationPool"] = appPoolName;

                ConfigurationElementCollection applicationCollection = applicationElement.GetCollection();
                ConfigurationElement virtualDirectoryElement = applicationCollection.CreateElement("virtualDirectory");
                virtualDirectoryElement["path"] = @"/";
                virtualDirectoryElement["physicalPath"] = physicalPath;
                applicationCollection.Add(virtualDirectoryElement);
                siteCollection.Add(applicationElement);
                sitesCollection.Add(siteElement);

                serverManager.CommitChanges();
            }
        }

        public void CreateApp(string siteName, string appName, string physicalPath)
        {
            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();

                ConfigurationSection sitesSection = config.GetSection("system.applicationHost/sites");

                ConfigurationElementCollection sitesCollection = sitesSection.GetCollection();

                ConfigurationElement siteElement = FindElement(sitesCollection, "site", "name", siteName);
                if (siteElement == null) throw new InvalidOperationException("Element not found!");

                ConfigurationElementCollection siteCollection = siteElement.GetCollection();

                ConfigurationElement applicationElement = siteCollection.CreateElement("application");
                string appPath = @"/" + appName;
                appPath = appPath.Replace("//", "/");
                applicationElement["path"] = appPath;

                ConfigurationElementCollection applicationCollection = applicationElement.GetCollection();

                ConfigurationElement virtualDirectoryElement = applicationCollection.CreateElement("virtualDirectory");
                virtualDirectoryElement["path"] = @"/";
                virtualDirectoryElement["physicalPath"] = physicalPath;
                applicationCollection.Add(virtualDirectoryElement);
                siteCollection.Add(applicationElement);

                serverManager.CommitChanges();
            }
        }

        public bool IsIISInstalled()
        {
            bool result = true;
            if (!File.Exists(Path.Combine(Strings.IIS64BitPath, "iiscore.dll")))
            {
                result = false;
            }
            if (!File.Exists(Path.Combine(Strings.IIS64BitPath, "config", "applicationhost.config")))
            {
                result = false;
            }
            return result;
        }

        public bool IsUrlRewriteInstalledForIIS()
        {
            bool result = true;
            var toRewrite64 = Path.Combine(Strings.IIS64BitPath, "rewrite.dll");
            var toRewrite32 = Path.Combine(Strings.IIS32BitPath, "rewrite.dll");
            if (!File.Exists(toRewrite64))
            {
                result = false;
            }

            if (!File.Exists(toRewrite32))
            {
                result = false;
            }

            using (ServerManager serverManager = GetServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection globalModulesSection = config.GetSection("system.webServer/globalModules");
                ConfigurationElementCollection globalModulesCollection = globalModulesSection.GetCollection();

                if (FindElement(globalModulesCollection, "add", "name", "RewriteModule") == null)
                {
                    result = false;
                }

                ConfigurationSection modulesSection = config.GetSection("system.webServer/modules");
                ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();
                if (FindElement(modulesCollection, "add", "name", "RewriteModule") == null)
                {
                    result = false;
                }                
            }
            return result;
        }

        private static ConfigurationElement FindElement(ConfigurationElementCollection collection, string elementTagName, params string[] keyValues)
        {
            foreach (ConfigurationElement element in collection)
            {
                if (String.Equals(element.ElementTagName, elementTagName, StringComparison.OrdinalIgnoreCase))
                {
                    bool matches = true;

                    for (int i = 0; i < keyValues.Length; i += 2)
                    {
                        object o = element.GetAttributeValue(keyValues[i]);
                        string value = null;
                        if (o != null)
                        {
                            value = o.ToString();
                        }

                        if (!String.Equals(value, keyValues[i + 1], StringComparison.OrdinalIgnoreCase))
                        {
                            matches = false;
                            break;
                        }
                    }
                    if (matches)
                    {
                        return element;
                    }
                }
            }
            return null;
        }

        public static void BackupAppHostConfig()
        {
            string fromfile = Strings.AppHostConfigPath;
            string tofile = Strings.AppHostConfigPath + ".ancmtest.bak";
            if (File.Exists(fromfile))
            {
                TestUtility.FileCopy(fromfile, tofile, overWrite: false);
            }
        }

        public static void RestoreAppHostConfig(bool restartIISServices = false)
        {
            string fromfile = Strings.AppHostConfigPath + ".ancmtest.bak";
            string tofile = Strings.AppHostConfigPath;

            // backup first if the backup file is not available
            if (!File.Exists(fromfile))
            {
                BackupAppHostConfig();
            }

            // try again after the ininial clean up 
            if (File.Exists(fromfile))
            {
                TestUtility.FileCopy(fromfile, tofile);
                if (restartIISServices)
                {
                    RestartServices(4);
                }
                TestUtility.FileCopy(fromfile, tofile);
            }
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
    }
}