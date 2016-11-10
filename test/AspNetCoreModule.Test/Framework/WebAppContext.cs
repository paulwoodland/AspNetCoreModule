// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace AspNetCoreModule.Test.Framework
{
    public class WebAppContext : IDisposable
    {
        private WebSiteContext _siteContext;
        public WebSiteContext SiteContext
        {
            get
            {
                return _siteContext;
            }
            set
            {
                _siteContext = value;
            }
        }

        public WebAppContext(string name, string physicalPath, string url = null)
            : this(name, physicalPath, null, url)
        {
        }
                
        public WebAppContext(string name, string physicalPath, WebSiteContext siteContext, string url = null)
        {
            _siteContext = siteContext;
            _name = name;
            string temp = physicalPath;
            if (physicalPath.Contains("%"))
            {
                temp = System.Environment.ExpandEnvironmentVariables(physicalPath);
            }
            _physicalPath = temp;

            if (url != null)
            {
                _url = url;
            }
            else
            {
                _url = "/" + name;
                _url.Replace("//", "/");
            }

            BackupFile("web.config");
        }

        public void Dispose()
        {
            DeleteFile("app_offline.htm");
            RestoreFile("web.config");
        }

        private string _name = null;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private string _physicalPath = null;
        public string PhysicalPath
        {
            get
            {
                return _physicalPath;
            }
            set
            {
                _physicalPath = value;
            }
        }

        private string _url = null;
        public string URL
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
            }
        }

        public Uri GetHttpUri()
        {
            return new Uri("http://" + SiteContext.HostName + ":" + _siteContext.TcpPort.ToString() + URL);
        }

        public Uri GetHttpUri(string subPath)
        {
            return new Uri("http://" + SiteContext.HostName + ":" +  _siteContext.TcpPort.ToString()  + URL + "/" + subPath);
        }

        public string _appPoolName = null;
        public string AppPoolName
        {
            get
            {
                if (_appPoolName == null)
                {
                    _appPoolName = "DefaultAppPool";
                }
                return _appPoolName;
            }
            set
            {
                _appPoolName = value;
            }
        }

        public string GetProcessFileName()
        {
            string filePath = Path.Combine(_physicalPath, "web.config");
            string result = null;

            // read web.config
            string fileContent = TestUtility.FileReadAllText(filePath);

            // get the value of processPath attribute of aspNetCore element
            if (fileContent != null)
            {
                result = TestUtility.XmlParser(fileContent, "aspNetCore", "processPath", null);
            }

            // split fileName from full path
            result = Path.GetFileName(result);

            // append .exe if it wasn't used
            if (!result.Contains(".exe"))
            {
                result = result + ".exe";
            }
            return result;
        }

        public void BackupFile(string from)
        {
            string fromfile = Path.Combine(_physicalPath, from);
            string tofile = Path.Combine(_physicalPath, fromfile + ".bak");
            TestUtility.FileCopy(fromfile, tofile, overWrite: false);
        }

        public void RestoreFile(string from)
        {
            string fromfile = Path.Combine(_physicalPath, from + ".bak");
            string tofile = Path.Combine(_physicalPath, from);
            if (!File.Exists(tofile))
            {
                BackupFile(from);
            }
            TestUtility.FileCopy(fromfile, tofile);
        }

        public void DeleteFile(string file = "app_offline.htm")
        {
            string filePath = Path.Combine(_physicalPath, file);
            TestUtility.DeleteFile(filePath);
        }

        public void CreateFile(string[] content, string file = "app_offline.htm")
        {
            string filePath = Path.Combine(_physicalPath, file);
            TestUtility.CreateFile(filePath, content);
        }

        public void MoveFile(string from, string to)
        {
            string fromfile = Path.Combine(_physicalPath, from);
            string tofile = Path.Combine(_physicalPath, to);
            TestUtility.FileMove(fromfile, tofile);
        }
    }
}