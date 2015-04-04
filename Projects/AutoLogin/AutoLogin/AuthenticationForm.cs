namespace TestCommonLibrary.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    /// <summary>
    /// Form for authentication
    /// </summary>
    [ComVisible(true)]
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public partial class AuthenticationForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationForm" /> class.
        /// </summary>
        public AuthenticationForm()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Delegate for invoke
        /// </summary>
        public delegate void MyDelegate();

        /// <summary>
        /// Get cookies
        /// </summary>
        /// <param name="userName">
        /// The user name.
        /// </param>
        /// <param name="password">
        /// The password.
        /// </param>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="timeoutInSeconds">
        /// The timeout in seconds.
        /// </param>
        /// <returns>
        /// The cookies in string<see cref="string"/>.
        /// </returns>
        public static CookieContainer GetCookie(string userName, string password, Uri uri, int timeoutInSeconds = 30)
        {
            CookieContainer cookie = null;
            var newThread = new Thread(
                new ThreadStart(
                    () =>
                        {
                            using (var form = new AuthenticationForm())
                            {
                                cookie = form.GetAuthenticationCookie(userName, password, uri);
                                form.Close();
                                form.Dispose();
                            }
                        }));
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
            newThread.Join(timeoutInSeconds * 1000);

            if (cookie == null)
            {
                throw new InvalidOperationException("Get cookie failed!");
            }

            return cookie;
        }

        /// <summary>
        /// Get cookies
        /// </summary>
        /// <param name="userName">user name</param>
        /// <param name="password">password for the user</param>
        /// <param name="uri">target uri</param>
        /// <returns>the cookies in string</returns>
        private CookieContainer GetAuthenticationCookie(string userName, string password, Uri uri)
        {
            CookieHelper.ClearCookie();

            this.webBrowser.ScriptErrorsSuppressed = true;
            this.webBrowser.ObjectForScripting = this;
            CookieContainer container = null;

            this.webBrowser.DocumentCompleted += delegate(object sender, WebBrowserDocumentCompletedEventArgs e)
                {
                    if (!string.IsNullOrEmpty(this.webBrowser.Document.Body.OuterText))
                    {
                        // If navigation failed for some reason close the window and fail the suite
                        if (this.webBrowser.Document.Body.OuterText.Contains("Navigation to the webpage was canceled"))
                        {
                            this.Close();
                            throw new Exception("Navigation to the webpage for getting the cookie failed!");
                        }

                        // Sign in.
                        if (webBrowser.Document.Body.OuterHtml.Contains("cred_userid_inputtext"))
                        {
                            var userName1 =
                                webBrowser.Document.All.OfType<HtmlElement>()
                                    .FirstOrDefault(ele => ele.GetAttribute("id") == "cred_userid_inputtext");
                            var passWord =
                                webBrowser.Document.All.OfType<HtmlElement>()
                                    .FirstOrDefault(ele => ele.GetAttribute("id") == "cred_password_inputtext");
                            var submitBtn =
                                webBrowser.Document.All.OfType<HtmlElement>()
                                    .FirstOrDefault(ele => ele.GetAttribute("id") == "cred_sign_in_button");
                            userName1.SetAttribute("value", userName);
                            passWord.SetAttribute("value", password);

                            this.BeginInvoke(new MyDelegate(this.Submit));
                        }

                        // Get cookie container if sign in successfully.
                        if (webBrowser.Document.Body.OuterHtml.Contains("SkypeCast Scheduler"))
                        {
                            container = CookieHelper.GetUriCookieContainer(uri);
                            this.Close();
                        }
                    }
                };

            this.webBrowser.Navigate(uri);

            this.ShowDialog();

            return container;
        }

        private void Submit()
        {
            var element = this.webBrowser.Document.GetElementById("cred_sign_in_button");

            if (element != null)
            {
                element.InvokeMember("click");
            }
        }
    }
}
