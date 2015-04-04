using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoLogin
{
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;

    using TestCommonLibrary.Authentication;

    class Program
    {
        private static void Main(string[] args)
        {

            var url = new Uri("https://tst01wusscscsvc.cloudapp.net/");
            var cookie = AuthenticationForm.GetCookie("leitan@exchlabs.ccsctp.net", "Pa$$word3", url, 180);

            var request =
                (HttpWebRequest) HttpWebRequest.Create("https://tst01wusscscsvc.cloudapp.net/account/createmeeting");
           
            request.CookieContainer = cookie;

            using (var response = request.GetResponse())
            {
                using (var readStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    Console.WriteLine(readStream.ReadToEnd());
                }
            }

        }


    }
}
