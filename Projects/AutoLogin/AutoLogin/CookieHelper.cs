//--------------------------------------------------------------------------
// <copyright file="CookieHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//--------------------------------------------------------------------------
namespace TestCommonLibrary.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Helper class for Cookie to get HttpOnly Cookie. The document.cookie only returns non httponly cookie.
    /// </summary>
    public static class CookieHelper
    {
        private const int InternetCookieHttponly = 0x2000;
        private const int INTERNETOPTIONENDBROWSERSESSION = 42;

        /// <summary>
        /// Gets the URI cookie container.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The Cookie container</returns>
        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = null;

            // Determine the size of the cookie
            int datasize = 8192 * 16;
            StringBuilder cookieData = new StringBuilder(datasize);

            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                {
                    return null;
                }

                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
                {
                    return null;
                }
            }

            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }

            return cookies;
        }

        /// <summary>
        /// Clear the Cookie
        /// </summary>
        public static void ClearCookie()
        {
            InternetSetOption(IntPtr.Zero, INTERNETOPTIONENDBROWSERSESSION, IntPtr.Zero, 0);
        }

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, int flags, IntPtr reservedIntPtr);

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr internet, int option, IntPtr buffer, int bufferLength);
    }
}
