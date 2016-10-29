using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Administration;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics.Eventing.Reader;
using AspNetCoreModule.Test.Utility;

/// <summary>
/// Helper Class for Dynamic Site Registration Test Cases
/// </summary> 
namespace AspNetCoreModule.Test.HttpClient
{
    public class HttpClientUtility
    {
        private ServerManager _mgr = new ServerManager();

        private IPHostEntry _host = Dns.GetHostEntry(Dns.GetHostName());

        private string _rootDir = @"%systemdrive%\inetpub\wwwroot";

        public string RootDir
        {
            get { return _rootDir; }
        }

        private string _ipv4Loopback = "127.0.0.1";
        private string _ipv4One = null;
        private string _ipv4Two = null;
        private string _ipv6Loopback = "[::1]";
        private string _ipv6One = null;
        private string _ipv6Two = null;

        public string IPv4Loopback
        {
            get { return _ipv4Loopback; }
        }
        public string IPv4One
        {
            get { return _ipv4One; }
        }
        public string IPv4Two
        {
            get { return _ipv4Two; }
        }
        public string IPv6Loopback
        {
            get { return _ipv6Loopback; }
        }
        public string IPv6One
        {
            get { return _ipv6One; }
        }
        public string IPv6Two
        {
            get { return _ipv6Two; }
        }

        private string[] _Ips;

        private string[] _Hosts = { "foo", "bar", "foobar", "barfoo" };

        private string _unusedIp;

        // ToDo: Clean up
        //private string _unusedHost1 = "unused1";
        //private string _unusedHost2 = "unused2";


        private Thread _backgroundRequestThread = null;

        public HttpClientUtility()
        {
            ReadMachineIpAddressInfo();

            _Ips = new string[] { _ipv4Loopback, _ipv4One, _ipv6Loopback, _ipv6One, _ipv6Two };

            _Hosts = new string[] { "foo", "bar", "foobar", "barfoo" };

            _unusedIp = _ipv6Two;
            //_unusedHost1 = "unused1";
            //_unusedHost2 = "unused2";
        }


        #region HELPERS
        
        public void Setup(int dynamicRegistrationThreshold)
        {
            DeleteAllAppPools();
            DeleteAllSites();
            SetDynamicSiteRegistrationThreshold(dynamicRegistrationThreshold);
        }

        public void ReadMachineIpAddressInfo()
        {
            foreach (IPAddress ip in _host.AddressList)
            {
                if (IPAddress.IsLoopback(ip))
                    continue;

                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (_ipv4One == null)
                        _ipv4One = ip.ToString();
                    else if (_ipv4Two == null)
                        _ipv4Two = ip.ToString();
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (!ip.ToString().Contains("%"))
                    {
                        if (_ipv6One == null)
                            _ipv6One = "[" + ip.ToString() + "]";
                        else if (_ipv6Two == null)
                            _ipv6Two = "[" + ip.ToString() + "]";
                    }
                }
            }
        }


        public int SendReceiveStatus(string path = "/", string protocol = "http", string ip = "127.0.0.1", int port = 8080, string host = "localhost", int expectedStatus = 200, int retryCount = 0)
        {
            string uri = protocol + "://" + ip + ":" + port + path;
            int status = HttpClient.sendRequest(uri, host, "CN=NULL", false, false);
            for (int i = 0; i < retryCount; i++)
            {
                if (status == expectedStatus)
                {
                    break;
                }
                DoSleep(1000);
                status = HttpClient.sendRequest(uri, host, "CN=NULL", false, false);
            }            
            return status;
        }

        public void DoRequest(string uri, string host = null, string expectedCN = "CN=NULL", bool useLegacy = false, bool displayContent = false)
        {
            HttpClient.sendRequest(uri, host, expectedCN, useLegacy, displayContent);
        }

        private void BackgroundRequestLoop(object req)
        {
            String[] uriHost = (String[])req;

            while (true)
            {
                HttpClient.sendRequest(uriHost[0], uriHost[1], "CN=NULL", false, false, false);
                Thread.Sleep(5000);
            }
        }

        public void StartBackgroundRequests(string uri, string host = null)
        {
            if (_backgroundRequestThread != null && _backgroundRequestThread.ThreadState == System.Threading.ThreadState.Running)
                _backgroundRequestThread.Abort();

            if (host == null)
                TestUtility.LogMessage(String.Format("########## Starting background requests to {0} with no hostname ##########", uri));
            else
                TestUtility.LogMessage(String.Format("########## Starting background requests to {0} with hostname {1} ##########", uri, host));


            ParameterizedThreadStart threadStart = new ParameterizedThreadStart(BackgroundRequestLoop);
            _backgroundRequestThread = new Thread(threadStart);
            _backgroundRequestThread.IsBackground = true;
            _backgroundRequestThread.Start(new string[] { uri, host });
        }

        public void StopBackgroundRequests()
        {
            TestUtility.LogMessage(String.Format("####################### Stopping background requests #######################"));

            if (_backgroundRequestThread != null && _backgroundRequestThread.ThreadState == System.Threading.ThreadState.Running)
                _backgroundRequestThread.Abort();

            _backgroundRequestThread = null;
        }

        public void DoSleep(int sleepMs)
        {
            TestUtility.LogMessage(String.Format("################## Sleeping for {0} ms ##################", sleepMs));
            Thread.Sleep(sleepMs);
        }

        public void KillWorkerProcess(string owner)
        {
            TestUtility.LogMessage(String.Format("######## Killing worker process with owner {0} ########", owner));
            string query = "Select * from Win32_Process Where Name = \"w3wp.exe\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user
                    //string owner = argList[1] + "\\" + argList[0];
                    //return owner;
                    if (String.Compare(argList[0], owner, true) == 0)
                        obj.InvokeMethod("Terminate", null);
                }
            }
        }


        public string VerifyRunningWpOwners(string[] owners)
        {
            string query = "Select * From Win32_Process Where Name = 'w3wp.exe'";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();


            bool[] ownersFound = new bool[owners.Length];
            for (int i = 0; i < ownersFound.Length; i++)
                ownersFound[i] = false;

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    bool found = false;
                    for (int i = 0; i < owners.Length; i++)
                    {
                        if (argList[0].ToUpper() == owners[i].ToUpper())
                        {
                            found = ownersFound[i] = true;
                            owners[i] = argList[0] + "\\" + argList[1];
                            break;
                        }
                    }
                    if (!found)
                    {
                        TestUtility.LogFail(String.Format("Unexpeced w3wp.exe with owner {0}\\{1} found", argList[0], argList[1]));
                    }
                }
            }

            for (int i = 0; i < owners.Length; i++)
            {
                if (ownersFound[i])
                    TestUtility.LogPass(String.Format("w3wp.exe with owner {0} found", owners[i]));
                else
                    TestUtility.LogFail(String.Format("w3wp.exe with owner {0} not found", owners[i]));
            }

            return null;
        }

       public void CreateAppPool(string poolName, bool alwaysRunning = false)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Adding App Pool {0} with startMode = {1} ####################", poolName, alwaysRunning ? "AlwaysRunning" : "OnDemand"));

                _mgr.ApplicationPools.Add(poolName);
                ApplicationPool apppool = _mgr.ApplicationPools[poolName];
                apppool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                if (alwaysRunning)
                    apppool.SetAttributeValue("startMode", "AlwaysRunning");
                _mgr.CommitChanges();
            }

            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Create app pool {0} failed. Reason: {1} ####################", poolName, ex.Message));
            }
        }

        public void SetIdleTimeoutForAppPool(string appPoolName, int idleTimeoutMinutes)
        {
            TestUtility.LogMessage(String.Format("#################### Setting idleTimeout to {0} minutes for AppPool {1} ####################", idleTimeoutMinutes, appPoolName));

            try
            {
                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                appPools[appPoolName].ProcessModel.IdleTimeout = TimeSpan.FromMinutes(idleTimeoutMinutes);
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Setting idleTimeout to {0} minutes for AppPool {1} failed. Reason: {2} ####################", idleTimeoutMinutes, appPoolName, ex.Message));
            }
        }

        public void SetMaxProcessesForAppPool(string appPoolName, int maxProcesses)
        {
            TestUtility.LogMessage(String.Format("#################### Setting maxProcesses to {0} for AppPool {1} ####################", maxProcesses, appPoolName));

            try
            {
                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                appPools[appPoolName].ProcessModel.MaxProcesses = maxProcesses;
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Setting maxProcesses to {0} for AppPool {1} failed. Reason: {2} ####################", maxProcesses, appPoolName, ex.Message));
            }
        }

        public void SetIdentityForAppPool(string appPoolName, string userName, string password)
        {
            TestUtility.LogMessage(String.Format("#################### Setting userName {0} and password {1} for AppPool {2} ####################", userName, password, appPoolName));

            try
            {
                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                appPools[appPoolName].ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
                appPools[appPoolName].ProcessModel.UserName = userName;
                appPools[appPoolName].ProcessModel.Password = password;
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Setting userName {0} and password {1} for AppPool {2} failed. Reason: {2} ####################", userName, password, appPoolName, ex.Message));
            }
        }

        public void SetStartModeAlwaysRunningForAppPool(string appPoolName, bool alwaysRunning)
        {
            string startMode = alwaysRunning ? "AlwaysRunning" : "OnDemand";

            TestUtility.LogMessage(String.Format("#################### Setting startMode to {0} for AppPool {1} ####################", startMode, appPoolName));

            try
            {
                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                appPools[appPoolName]["startMode"] = startMode;
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Setting startMode to {0} for AppPool {1} failed. Reason: {2} ####################", startMode, appPoolName, ex.Message));
            }
        }

        public void StartAppPool(string appPoolName)
        {
            StartOrStopAppPool(appPoolName, true);
        }

        public void StopAppPool(string appPoolName)
        {
            StartOrStopAppPool(appPoolName, false);
        }

        private void StartOrStopAppPool(string appPoolName, bool start)
        {
            string action = start ? "Starting" : "Stopping";
            TestUtility.LogMessage(String.Format("#################### {0} app pool {1} ####################", action, appPoolName));

            try
            {
                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                if (start)
                    appPools[appPoolName].Start();
                else
                    appPools[appPoolName].Stop();
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                TestUtility.LogMessage(String.Format("#################### {0} app pool {1} failed. Reason: {2} ####################", action, appPoolName, ex.Message));
            }
        }

        public void VerifyAppPoolState(string appPoolName, Microsoft.Web.Administration.ObjectState state)
        {
            try
            {
                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                if (appPools[appPoolName].State == state)
                    TestUtility.LogPass(String.Format("Verified state for app pool {0} is {1}.", appPoolName, state.ToString()));
                else
                    TestUtility.LogPass(String.Format("Unexpected state {0} for app pool  {1}.", state, appPoolName.ToString()));
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Failed to verify state for app pool {0}. Reason: {1} ####################", appPoolName, ex.Message));
            }
        }

        public void DeleteAppPool(string poolName)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Deleting App Pool {0} ####################", poolName));

                ApplicationPoolCollection appPools = _mgr.ApplicationPools;
                appPools.Remove(appPools[poolName]);
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Delete app pool {0} failed. Reason: {1} ####################", poolName, ex.Message));
            }
        }

        public void DeleteAllAppPools()
        {
            TestUtility.LogMessage(String.Format("#################### Deleting all app pools ####################"));

            ApplicationPoolCollection appPools = _mgr.ApplicationPools;
            while (appPools.Count > 0)
                appPools.RemoveAt(0);
            _mgr.CommitChanges();
        }

        public void CreateSite(int siteId, string siteName, string poolName, string dirRoot, string Ip, int Port, string host)
        {
            try
            {
                string bindingInfo = "";
                if (Ip == null)
                    Ip = "*";
                bindingInfo += Ip;
                bindingInfo += ":";
                bindingInfo += Port;
                bindingInfo += ":";
                if (host != null)
                    bindingInfo += host;

                TestUtility.LogMessage(String.Format("#################### Adding Site {0} with App Pool {1} with BindingInfo {2} ####################", siteName, poolName, bindingInfo));

                SiteCollection sites = _mgr.Sites;
                Site site = sites.CreateElement();
                site.Id = siteId;
                site.SetAttributeValue("name", siteName);
                sites.Add(site);

                Application app = site.Applications.CreateElement();
                app.SetAttributeValue("path", "/");
                app.SetAttributeValue("applicationPool", poolName);
                site.Applications.Add(app);

                VirtualDirectory vdir = app.VirtualDirectories.CreateElement();
                vdir.SetAttributeValue("path", "/");
                vdir.SetAttributeValue("physicalPath", dirRoot);

                app.VirtualDirectories.Add(vdir);

                Binding b = site.Bindings.CreateElement();
                b.SetAttributeValue("protocol", "http");
                b.SetAttributeValue("bindingInformation", bindingInfo);

                site.Bindings.Add(b);

                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Create site {0} failed. Reason: {1} ####################", siteName, ex.Message));
            }
        }

        public void StartSite(string siteName)
        {
            StartOrStopSite(siteName, true);
        }

        public void StopSite(string siteName)
        {
            StartOrStopSite(siteName, false);
        }

        private void StartOrStopSite(string siteName, bool start)
        {
            string action = start ? "Starting" : "Stopping";
            TestUtility.LogMessage(String.Format("#################### {0} site {1} ####################", action, siteName));

            try
            {
                SiteCollection sites = _mgr.Sites;
                if (start)
                {
                    sites[siteName].Start();
                    sites[siteName].SetAttributeValue("serverAutoStart", true);
                }
                else
                {
                    sites[siteName].Stop();
                    sites[siteName].SetAttributeValue("serverAutoStart", false);
                }
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### {0} site {1} failed. Reason: {2} ####################", action, siteName, ex.Message));
            }
        }

        public void VerifySiteState(string siteName, Microsoft.Web.Administration.ObjectState state)
        {
            try
            {
                SiteCollection sites = _mgr.Sites;
                if (sites[siteName].State == state)
                    TestUtility.LogPass(String.Format("Verified state for site {0} is {1}.", siteName, state.ToString()));
                else
                    TestUtility.LogPass(String.Format("Unexpected state {0} for site  {1}.", state, siteName.ToString()));
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Failed to verify state for site {0}. Reason: {1} ####################", siteName, ex.Message));
            }
        }

        public void AddApplicationToSite(string siteName, string appPath, string physicalPath, string poolName)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Adding Application {0} with App Pool {1} to Site {2} ####################", appPath, poolName, siteName));

                SiteCollection sites = _mgr.Sites;
                Application app = sites[siteName].Applications.CreateElement();
                app.SetAttributeValue("path", appPath);
                app.SetAttributeValue("applicationPool", poolName);
                sites[siteName].Applications.Add(app);

                VirtualDirectory vdir = app.VirtualDirectories.CreateElement();
                vdir.SetAttributeValue("path", "/");
                vdir.SetAttributeValue("physicalPath", physicalPath);

                app.VirtualDirectories.Add(vdir);

                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Add Application {0} with App Pool {1} to Site {2} failed. Reason: {3} ####################", appPath, poolName, siteName, ex.Message));
            }
        }

        public void ChangeApplicationPool(string siteName, int appIndex, string poolName)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Changing Application Pool for App {0} of Site {1} to {2} ####################", appIndex, siteName, poolName));

                _mgr.Sites[siteName].Applications[appIndex].SetAttributeValue("applicationPool", poolName);

                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Changing Application Pool for App {0} of Site {1} to {2} failed. Reason: {3} ####################", appIndex, siteName, poolName, ex.Message));
            }
        }

        public void ChangeApplicationPath(string siteName, int appIndex, string path)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Changing Path for App {0} of Site {1} to {2} ####################", appIndex, siteName, path));

                _mgr.Sites[siteName].Applications[appIndex].SetAttributeValue("path", path);

                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Changing Path for App {0} of Site {1} to {2} failed. Reason: {3} ####################", appIndex, siteName, path, ex.Message));
            }
        }

        public void RemoveApplication(string siteName, int appIndex)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Deleting App {0} from Site {1} ####################", appIndex, siteName));

                _mgr.Sites[siteName].Applications.RemoveAt(appIndex);

                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Deleting App {0} from Site {1} failed. Reason: {2} ####################", appIndex, siteName, ex.Message));
            }
        }

        public void AddBindingToSite(string siteName, string Ip, int Port, string host)
        {
            string bindingInfo = "";
            if (Ip == null)
                Ip = "*";
            bindingInfo += Ip;
            bindingInfo += ":";
            bindingInfo += Port;
            bindingInfo += ":";
            if (host != null)
                bindingInfo += host;

            TestUtility.LogMessage(String.Format("#################### Adding Binding {0} to Site {1} ####################", bindingInfo, siteName));

            try
            {
                SiteCollection sites = _mgr.Sites;
                Binding b = sites[siteName].Bindings.CreateElement();
                b.SetAttributeValue("protocol", "http");
                b.SetAttributeValue("bindingInformation", bindingInfo);

                sites[siteName].Bindings.Add(b);

                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Adding Binding {0} to Site {1} failed. Reason: {2} ####################", bindingInfo, siteName, ex.Message));
            }
        }

        public void RemoveBindingFromSite(string siteName, BindingInfo bindingInfo)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Removing Binding {0} from Site {1} ####################", bindingInfo.ToBindingString(), siteName));

                for (int i = 0; i < _mgr.Sites[siteName].Bindings.Count; i++)
                {
                    if (_mgr.Sites[siteName].Bindings[i].BindingInformation.ToString() == bindingInfo.ToBindingString())
                    {
                        _mgr.Sites[siteName].Bindings.RemoveAt(i);
                        _mgr.CommitChanges();
                        return;
                    }
                }

                TestUtility.LogMessage(String.Format("#################### Remove binding failed because binding was not found ####################"));
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Remove binding failed. Reason: {0} ####################", ex.Message));
            }
        }

        public void ModifyBindingForSite(string siteName, BindingInfo bindingInfoOld, BindingInfo bindingInfoNew)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Changing Binding {0} for Site {1} to {2} ####################", bindingInfoOld.ToBindingString(), siteName, bindingInfoNew.ToBindingString()));

                for (int i = 0; i < _mgr.Sites[siteName].Bindings.Count; i++)
                {
                    if (_mgr.Sites[siteName].Bindings[i].BindingInformation.ToString() == bindingInfoOld.ToBindingString())
                    {
                        _mgr.Sites[siteName].Bindings[i].SetAttributeValue("bindingInformation", bindingInfoNew.ToBindingString());
                        _mgr.CommitChanges();
                        return;
                    }
                }

                TestUtility.LogMessage(String.Format("#################### Modify binding failed because binding was not found ####################"));
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Changing binding failed. Reason: {0} ####################", ex.Message));
            }
        }

        public void DeleteSite(string siteName)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Deleting Site {0} ####################", siteName));

                SiteCollection sites = _mgr.Sites;
                sites.Remove(sites[siteName]);
                _mgr.CommitChanges();
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Delete site {0} failed. Reason: {1} ####################", siteName, ex.Message));
            }
        }

        public void DeleteAllSites()
        {
            TestUtility.LogMessage(String.Format("#################### Deleting all sites ####################"));

            SiteCollection sites = _mgr.Sites;
            while (sites.Count > 0)
                sites.RemoveAt(0);
            _mgr.CommitChanges();
        }

        public void SetDynamicSiteRegistrationThreshold(int threshold)
        {
            try
            {
                TestUtility.LogMessage(String.Format("#################### Changing dynamicRegistrationThreshold to {0} ####################", threshold));

                using (ServerManager serverManager = new ServerManager())
                {
                    Configuration config = serverManager.GetApplicationHostConfiguration();

                    ConfigurationSection webLimitsSection = config.GetSection("system.applicationHost/webLimits");
                    webLimitsSection["dynamicRegistrationThreshold"] = threshold;

                    serverManager.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                TestUtility.LogMessage(String.Format("#################### Changing dynamicRegistrationThreshold failed. Reason: {0} ####################", ex.Message));
            }
        }

        /// <summary>
        /// Clears the System Events Log
        /// </summary>
        /// <param name="EventVerifications">List of events to count</param>
        public void ClearSystemLog()
        {
            using (EventLog eventLog = new EventLog("System"))
            {
                eventLog.Clear();
            }
        }


        /// <summary>
        /// Verifies an event with a given ID was logged
        /// </summary>
        public void VerifySystemEvent(int id, string runningMode = null, string configReader = null)
        {
            try
            {
                TestUtility.LogMessage("Waiting 5 seconds for logfile to update...");
                Thread.Sleep(5000);
                EventLog systemLog = new EventLog("System");
                foreach (EventLogEntry entry in systemLog.Entries)
                {
                    // ToDo: Clean up 
                    // if (entry.EventID == id)
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

        #endregion
    }
}