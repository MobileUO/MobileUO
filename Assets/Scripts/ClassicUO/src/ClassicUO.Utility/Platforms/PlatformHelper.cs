// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility.Platforms
{
    public static class PlatformHelper
    {
        public static readonly bool IsMonoRuntime = Type.GetType("Mono.Runtime") != null;

        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        // MobileUO: added variable for Mobile
        public static readonly bool IsMobile = UnityEngine.Application.isMobilePlatform;

        public static void LaunchBrowser(string url, bool localFile = false, bool retry = false)
        {
            try
            {
                if(!localFile)
                    if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                    {
                        Log.Error($"Invalid URL format: {url}, trying with https://..");

                        if(!retry)
                            LaunchBrowser("https://" + url, true);

                        return;
                    }

                // MobileUO: handle opening URLs 
                if (IsMobile)
                {
                    UnityEngine.Application.OpenURL(url);
                }
                else if (IsWindows)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start \"\" \"{url}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                }
                else if (IsOSX)
                {
                    Process.Start("open", url);
                }
                else
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}