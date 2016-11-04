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
        
        private AppContext _websocketecho = null;
        public AppContext Websocketecho
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

        private AppContext _websocketechoChild = null;
        public AppContext WebsocketechoChild
        {
            get
            {
                if (_websocketechoChild == null)
                {
                    _websocketechoChild = new AppContext("websocketechoChild", @"%AspNetCoreModuleTest%\AspnetCoreApp_WebSocketEcho", "/parent/websocketechoChild");
                }
                return _websocketechoChild;
            }
        }

        private AppContext _parent = null;
        public AppContext Parent
        {
            get
            {
                if (_parent == null)
                {
                    _parent = new AppContext("parent", @"%AspNetCoreModuleTest%\parent", "/parent");
                }
                return _parent;
            }
        }
    }
}