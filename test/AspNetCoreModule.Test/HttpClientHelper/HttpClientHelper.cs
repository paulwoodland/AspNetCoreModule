using System;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using AspNetCoreModule.Test.Utility;

namespace AspNetCoreModule.Test.HttpClientHelper
{
    public class HttpClientHelper
    {
        // callback used to validate the certificate in an SSL conversation
        public static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        public static int sendRequest(string uri, string hostName, string expectedCN, bool useLegacy, bool displayContent, bool doLogging = true)
        {
            int status = -1;

            if (doLogging)
            {
                if (hostName == null)
                    TestUtility.LogMessage(String.Format("HttpClient::sendRequest() {0} with no hostname", uri));
                else
                    TestUtility.LogMessage(String.Format("HttpClient::sendRequest() {0} with hostname {1}", uri, hostName));
            }

            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            if (useLegacy)
            {
                TestUtility.LogMessage(String.Format("Using SSL3"));
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            }

            HttpWebRequest myRequest;
            myRequest = (HttpWebRequest)WebRequest.Create(uri);
            myRequest.Proxy = null;
            myRequest.KeepAlive = false;
            if (hostName != null)
                myRequest.Host = hostName;

            ServicePoint point = myRequest.ServicePoint;
            point.ConnectionLeaseTimeout = 0;

            try
            {
                using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
                {
                    using (Stream myStream = myResponse.GetResponseStream())
                    {
                        if (displayContent)
                        {
                            using (StreamReader myReader = new StreamReader(myStream))
                            {
                                string text = myReader.ReadToEnd();
                                TestUtility.LogMessage("\n\n");
                                TestUtility.LogMessage(text);
                                TestUtility.LogMessage("\n\n");
                            }
                        }
                    }
                    status = (int)myResponse.StatusCode;
                }
            }
            catch (WebException ex)
            {
                if ((HttpWebResponse)ex.Response == null)
                    status = 0;
                else
                    status = (int)((HttpWebResponse)ex.Response).StatusCode;
            }

            return status;
        }
    }
}

