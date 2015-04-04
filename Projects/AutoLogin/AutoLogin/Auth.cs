
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AutoLogin
{
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    /// <summary>
    /// This is used to directly fetch EK data from page html string. 
    /// </summary>
    [ComVisible(true)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public class EKUIDataFetcher
    {
        static bool ValidateServerCertificate(
            object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // Ignore certificate error.
            return true;
        }

        static EKUIDataFetcher()
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);
            ServicePointManager.DefaultConnectionLimit = 20;
            ServicePointManager.UseNagleAlgorithm = true;
            ServicePointManager.Expect100Continue = false;
        }

        private CookieContainer cookieContainer;
        private string lastNavigatedUrl = string.Empty;

        private string baseUrl = "https://tst01wusscscsvc.cloudapp.net/";

        private Exception sTAThreadException;

        #region Private Methodes
        bool loggedin = false;
        [Obsolete]
        private void sTAGetCookieThreadFunc()
        {
           // abortSession();

            using (var webBrowser = new WebBrowser())
            {
             
                webBrowser.AllowNavigation = true;
                webBrowser.ScriptErrorsSuppressed = true;
                webBrowser.ObjectForScripting = this;
                webBrowser.Navigated += (sender, e) =>
                {
                    lastNavigatedUrl = webBrowser.Url.ToString();
                };
                webBrowser.DocumentCompleted += (sender, e) =>
                    {
                        ((WebBrowser)sender).Document.Window.Error += (a, b) => { b.Handled = true; };

                    if ((e.Url != webBrowser.Url) || (webBrowser.ReadyState != WebBrowserReadyState.Complete))
                        return;

                    if (lastNavigatedUrl.StartsWith("https://login.microsoftonline.com"))
                    {
                        var userName = webBrowser.Document.All.OfType<HtmlElement>().FirstOrDefault(ele => ele.GetAttribute("id") == "cred_userid_inputtext");
                        var passWord = webBrowser.Document.All.OfType<HtmlElement>().FirstOrDefault(ele => ele.GetAttribute("id") == "cred_password_inputtext");
                        var submitBtn = webBrowser.Document.All.OfType<HtmlElement>().FirstOrDefault(ele => ele.GetAttribute("id") == "cred_sign_in_button");
                        userName.SetAttribute("value", "leitan@exchlabs.ccsctp.net");
                        passWord.SetAttribute("value", "Pa$$word3");
                        submitBtn.InvokeMember("click");
                        return;
                    }

                    if (lastNavigatedUrl.ToLower().Equals(this.baseUrl.ToLower()))
                    {
                        var title = webBrowser.Document.All.OfType<HtmlElement>().FirstOrDefault(ele => ele.TagName == "a" && ele.InnerText == "SkypeCast Scheduler");
                        if (title != null)
                        {
                            loggedin = true;
                        }
                        return;
                    }

                };


                getEKSiteAuthCookie(webBrowser);


                webBrowser.Stop();
                webBrowser.Dispose();
            }
        }

        public void init()
        {
            Thread initWebBrowserThread = new Thread(new ThreadStart(sTAGetCookieThreadFunc));
            initWebBrowserThread.IsBackground = true;
            initWebBrowserThread.SetApartmentState(ApartmentState.STA);
            initWebBrowserThread.Start();
            initWebBrowserThread.Join();
            if (this.sTAThreadException != null)
                throw this.sTAThreadException;
        }

        public string GetHttpResponse(string relativeUrl, string httpMethod = "Get", string contentType = "text/html", string body = null, int expectedStatusCode = 200)
        {
            HttpWebResponse response = GetHttpWebResponse(relativeUrl, httpMethod, contentType, body, expectedStatusCode);
            try
            {
                while (response.StatusCode == HttpStatusCode.Found)
                {
                    var redirectUrl = response.Headers["Location"];
                    response.Close();
                    response.Dispose();

                    response = GetHttpWebResponse(redirectUrl, httpMethod, contentType, body, expectedStatusCode);
                }

                if ((int)response.StatusCode != expectedStatusCode)
                {
                    throw new Exception("Get response failed. StatusCode = " + response.StatusCode + "; url = " + relativeUrl);
                }

                using (StreamReader readStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var responsContent = readStream.ReadToEnd();
                    return responsContent;
                }
            }
            finally
            {
                if (response != null)
                    response.Dispose();
            }
        }

        public HttpWebResponse GetHttpWebResponse(string relativeUrl, string httpMethod = "Get", string contentType = "text/html", string body = null, int expectedStatusCode = 200)
        {
            int retry = 0;
            WebException exception = null;
            while (retry++ < 5)
            {
                try
                {
                    var url = this.baseUrl + relativeUrl;
                    var req = (System.Net.HttpWebRequest)HttpWebRequest.Create(url);
                    //req.Proxy = null;
                    req.CookieContainer = this.cookieContainer;
                    req.ContentType = contentType;
                    req.Method = httpMethod;
                    req.Timeout = 1000 * 60 * 3;
                    if (body != null)
                    {
                        byte[] buf = Encoding.UTF8.GetBytes(body);
                        using (var reqStream = req.GetRequestStream())
                        {
                            reqStream.Write(buf, 0, buf.Length);
                        }
                    }
                    return (HttpWebResponse)req.GetResponse();
                }
                catch (WebException ex)
                {
                    exception = ex;
                }
            }
            throw exception;
        }

        private void getEKSiteAuthCookie(WebBrowser webBrowser)
        {
            navigateTopage(webBrowser, this.baseUrl);

     


            var liveLink = webBrowser.Document.GetElementById("page_header_logotag");
            if (liveLink == null || !liveLink.InnerHtml.ToLowerInvariant().Contains("<a href=\"/signout.aspx\">"))
            {
              
                return;
            }

            var cookie = getGlobalCookies(webBrowser.Document.Url.AbsoluteUri);
            if (cookie == null)
            {
                return;
            }
            this.cookieContainer = new CookieContainer();
            this.cookieContainer.SetCookies(new Uri(this.baseUrl), cookie);
        }


        [Obsolete]
        private void navigateTopage(WebBrowser webBrowser, string url)
        {
            webBrowser.Navigate(url);
            while ((webBrowser.ReadyState != WebBrowserReadyState.Complete || webBrowser.IsBusy == true)
                   || loggedin == false)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }
        }

        [Obsolete]
        private string getGlobalCookies(string uri)
        {
            uint datasize = 8192;
            int INTERNET_COOKIE_HTTPONLY = 0x00002000;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (InternetGetCookieEx(uri, null, cookieData, ref datasize, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero)
                && cookieData.Length > 0)
            {
                return cookieData.ToString().Replace(';', ',');
            }
            else
            {
                return null;
            }
        }

        private void abortSession()
        {
            // Clear Current Session and Cache
            InternetSetOption(IntPtr.Zero, 42, IntPtr.Zero, 0);
            /*
            Process process = Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 255");
            while (process.HasExited == false)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }*/
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        #endregion
    }
}
