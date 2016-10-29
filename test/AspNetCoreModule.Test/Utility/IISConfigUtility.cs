using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Web.Administration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using static AspNetCoreModule.Test.Utility.IISServer;

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
            public static string IISExpress32BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles (x86)%"), "IIS Express");
        }

        public enum AppPoolSettings
        {
            enable32BitAppOnWin64,
            none
        }

        public void Dispose()
        {
            RestoreAppHostConfig(false);
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
                applicationElement["path"] = @"/" + appName;

                ConfigurationElementCollection applicationCollection = applicationElement.GetCollection();

                ConfigurationElement virtualDirectoryElement = applicationCollection.CreateElement("virtualDirectory");
                virtualDirectoryElement["path"] = @"/";
                virtualDirectoryElement["physicalPath"] = physicalPath;
                applicationCollection.Add(virtualDirectoryElement);
                siteCollection.Add(applicationElement);

                serverManager.CommitChanges();
            }
        }

        public void EnableUrlRewriteToIIS()
        {
            var fromRewrite64 = Path.Combine(Strings.IISExpress64BitPath, "rewrite.dll");
            var fromRewrite32 = Path.Combine(Strings.IISExpress64BitPath, "rewrite.dll");
            var toRewrite64 = Path.Combine(Strings.IIS64BitPath, "rewrite.dll");
            var toRewrite32 = Path.Combine(Strings.IIS32BitPath, "rewrite.dll");
            if (!File.Exists(toRewrite64))
            {
                if (File.Exists(fromRewrite64))
                {
                    File.Copy(fromRewrite64, toRewrite64);
                }
            }
            if (!File.Exists(toRewrite32))
            {
                if (File.Exists(fromRewrite32))
                {
                    File.Copy(fromRewrite32, toRewrite32);
                }
            }

            using (ServerManager serverManager = GetServerManager())
            {
                bool commitChange = false;
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection globalModulesSection = config.GetSection("system.webServer/globalModules");
                ConfigurationElementCollection globalModulesCollection = globalModulesSection.GetCollection();

                if (FindElement(globalModulesCollection, "add", "name", "RewriteModule") == null)
                {
                    ConfigurationElement addElement = globalModulesCollection.CreateElement("add");
                    addElement["name"] = @"RewriteModule";
                    addElement["image"] = @"%windir%\system32\inetsrv\rewrite.dll";
                    globalModulesCollection.Add(addElement);
                    commitChange = true;
                }

                ConfigurationSection modulesSection = config.GetSection("system.webServer/modules");
                ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();

                if (FindElement(modulesCollection, "add", "name", "RewriteModule") == null)
                {
                    ConfigurationElement addElement = modulesCollection.CreateElement("add");
                    addElement["name"] = @"RewriteModule";
                    modulesCollection.Add(addElement);
                    commitChange = true;
                }
                if (commitChange)
                {
                    serverManager.CommitChanges();
                }
            }
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
            if (File.Exists(tofile))
            {
                if (File.Exists(fromfile))
                {
                    TestUtility.FileCopy(fromfile, tofile, overWrite: false);
                }
            }
        }

        public static void RestoreAppHostConfig(bool restartIISServices = true)
        {
            string fromfile = Strings.AppHostConfigPath + ".ancmtest.bak";
            string tofile = Strings.AppHostConfigPath;
            if (File.Exists(tofile))
            {
                if (!File.Exists(fromfile))
                {
                    BackupAppHostConfig();
                }

                if (File.GetCreationTime(fromfile) != File.GetCreationTime(tofile))
                {
                    TestUtility.FileCopy(fromfile, tofile);
                    if (restartIISServices)
                    {
                        RestartServices(2);
                    }
                    TestUtility.FileCopy(fromfile, tofile);

                    if (File.GetCreationTime(fromfile) != File.GetCreationTime(tofile))
                    {
                        RestartServices(1);
                        TestUtility.FileCopy(fromfile, tofile);
                    }
                }
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
            };
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