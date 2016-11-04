// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace AspNetCoreModule.Test.Framework
{
    public class WebSiteContext : IDisposable
    {
        public WebSiteContext(string hostName, string siteName, int tcpPort)
        {
            _hostName = hostName;
            _siteName = siteName;
            _tcpPort = tcpPort;
        }

        public void Dispose()
        {

        }

        public string _hostName = null;
        public string HostName
        {
            get
            {
                if (_hostName == null)
                {
                    _hostName = "localhost";
                }
                return _hostName;
            }
            set
            {
                _hostName = value;
            }
        }

        public string _siteName = null;
        public string SiteName
        {
            get
            {
                return _siteName;
            }
            set
            {
                _siteName = value;
            }
        }

        public int _tcpPort = 8080;
        public int TcpPort
        {
            get
            {
                return _tcpPort;
            }
            set
            {
                _tcpPort = value;
            }
        }
    }
}