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
        
        private TestWebApplication _websocketecho = null;
        public TestWebApplication Websocketecho
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

        private TestWebApplication _websocketechoChild = null;
        public TestWebApplication WebsocketechoChild
        {
            get
            {
                if (_websocketechoChild == null)
                {
                    _websocketechoChild = new TestWebApplication("websocketechoChild", @"%AspNetCoreModuleTest%\AspnetCoreApp_WebSocketEcho", "/parent/websocketechoChild");
                }
                return _websocketechoChild;
            }
        }

        private TestWebApplication _parent = null;
        public TestWebApplication Parent
        {
            get
            {
                if (_parent == null)
                {
                    _parent = new TestWebApplication("parent", @"%AspNetCoreModuleTest%\parent", "/parent");
                }
                return _parent;
            }
        }
    }
}