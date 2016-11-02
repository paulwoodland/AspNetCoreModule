using System;

/// <summary>
/// Helper Class for Dynamic Site Registration Test Cases
/// </summary> 
namespace AspNetCoreModule.Test.HttpClient
{
    /// <summary>
    /// Placeholder
    /// </summary>
    public class RequestInfo
    {
        public string ip;
        public int port;
        public string host;
        public int status;

        public RequestInfo(string ipIn, int portIn, string hostIn, int statusIn)
        {
            ip = ipIn;
            port = portIn;
            host = hostIn;
            status = statusIn;
        }

        public string ToUrlRegistration()
        {
            if ((ip == null || ip == "*") && (host == null || host == "*"))
                return String.Format("HTTP://*:{0}/", port).ToUpper();

            if (ip == null || ip == "*")
                return String.Format("HTTP://{0}:{1}/", host, port).ToUpper();

            if (host == null || host == "*")
                return String.Format("HTTP://{0}:{1}:{0}/", ip, port).ToUpper();

            return String.Format("HTTP://{0}:{1}:{2}/", host, port, ip).ToUpper();
        }
    }

    public class BindingInfo
    {
        public string ip;
        public int port;
        public string host;
        //public bool isConfigured;
        //public string appPoolName;
        //public int groupId;
        //public string siteName;
        //public int siteId;

        public BindingInfo(string ip, int port, string host)
        {
            this.ip = ip;
            this.port = port;
            this.host = host;
            //this.appPoolName = appPoolName;
            //this.groupId = groupId;
            //this.siteName = siteName;
            //this.siteId = siteId;
            //this.isConfigured = isConfigured;
        }

        public int GetBindingType()
        {
            if (ip == null)
            {
                if (host == null)
                    return 5;
                else
                    return 3;
            }
            else
            {
                if (host == null)
                    return 4;
                else
                    return 2;
            }
        }

        public bool IsSupportedForDynamic()
        {
            return GetBindingType() == 2 || GetBindingType() == 5;
        }

        public bool Match(RequestInfo req)
        {
            if (ip != null && ip != req.ip)
                return false;
            if (port != req.port)
                return false;
            if (host != null && host != req.host)
                return false;

            return true;
        }

        public string ToBindingString()
        {
            string bindingInfoString = "";
            bindingInfoString += (ip == null) ? "*" : ip;
            bindingInfoString += ":";
            bindingInfoString += port;
            bindingInfoString += ":";
            if (host != null)
                bindingInfoString += host;

            return bindingInfoString;
        }

        public string ToUrlRegistration()
        {
            if ((ip == null || ip == "*") && (host == null || host == "*"))
                return String.Format("HTTP://*:{0}/", port).ToUpper();

            if (ip == null || ip == "*")
                return String.Format("HTTP://{0}:{1}/", host, port).ToUpper();

            if (host == null || host == "*")
                return String.Format("HTTP://{0}:{1}:{0}/", ip, port).ToUpper();

            return String.Format("HTTP://{0}:{1}:{2}/", host, port, ip).ToUpper();
        }
    }
}