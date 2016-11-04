// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace AspNetCoreModule.Test.Framework
{
    public class IISServer : IDisposable
    {
        public IISServer()
        {
        }
        public void Dispose()
        {
        }
        
        private WebAppContext _websocketecho = null;
        public WebAppContext Websocketecho
        {
            get
            {
                if (_websocketecho == null)
                {
                    //_websocketecho = new AppContext("websocketecho", @"%AspNetCoreModuleTest%\AspnetCoreApp_WebSocketEcho", "/websocketecho");
                    //IIS.Applications.Add(_websocketecho);
                }
                return _websocketecho;
            }
        }

        private WebAppContext _websocketechoChild = null;
        public WebAppContext WebsocketechoChild
        {
            get
            {
                if (_websocketechoChild == null)
                {
                    _websocketechoChild = new WebAppContext("websocketechoChild", @"%AspNetCoreModuleTest%\AspnetCoreApp_WebSocketEcho", "/parent/websocketechoChild");
                }
                return _websocketechoChild;
            }
        }

        private WebAppContext _parent = null;
        public WebAppContext Parent
        {
            get
            {
                if (_parent == null)
                {
                    _parent = new WebAppContext("parent", @"%AspNetCoreModuleTest%\parent", "/parent");
                }
                return _parent;
            }
        }
    }
}