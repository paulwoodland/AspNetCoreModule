using System;

namespace AspNetCoreModule.Test.Utility
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