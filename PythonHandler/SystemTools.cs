using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace PythonHandler
{
    internal static class SystemTools
    {
        /// <summary>
        /// 获取操作系统名称
        /// </summary>
        /// <returns></returns>
        internal static string GetSystemName()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                string sCPUnumber = "";
                foreach (var item in searcher.Get())
                {
                    sCPUnumber = item["Version"] as string;
                }
                OperatingSystem os = Environment.OSVersion;
                switch (os.Platform)
                {
                    case PlatformID.Win32S:
                        return "Windows 3.1";
                    case PlatformID.Win32Windows:
                        return "Windows 95/98/Me";
                    case PlatformID.Win32NT:
                        {
                            int Major = int.Parse(sCPUnumber.Split('.')[0]);
                            int Minor = int.Parse(sCPUnumber.Split('.')[1]);
                            if (Major >= 6)
                                if (Major >= 6)
                                {
                                    if (Major == 6 && Minor == 0)
                                        return "Windows Vista";
                                    else if (Major == 6 && Minor == 1)
                                        return "Windows 7";
                                    else if (Major == 6 && Minor == 2)
                                        return "Windows 8";
                                    else if (Major == 6 && Minor == 3)
                                        return "Windows 8.1";
                                    else if (Major == 10)
                                        return "Windows 10 or Windows 11";
                                    else
                                        return "Windows NT";
                                }
                                else if (Major == 5 && Minor == 0)
                                {
                                    return "Windows 2000";
                                }
                                else if (Major == 5 && Minor == 1)
                                {
                                    return "Windows XP";
                                }
                                else if (Major == 5 && Minor == 2)
                                {
                                    return "Windows Server 2003";
                                }
                            break;
                        }
                    case PlatformID.WinCE:
                        break;
                    case PlatformID.Unix:
                        return "Unix";
                    case PlatformID.Xbox:
                        break;
                    case PlatformID.MacOSX:
                        return "Mac OS X";
                    default:
                        return "Unknown OS";
                }
            }
            catch (Exception e) { }
            return "";
        }

        internal static string GetNewestPythonVersion(string systemtype)
        {
            if (systemtype == "Windows 7")
            {
                return "3.8.0";
            }
            if (systemtype == "Windows Vista")
            {
                return "3.7.0";
            }
            if (systemtype == "Windows XP")
            {
                return "3.5.0";
            }
            if (systemtype == "Windows 2000")
            {
                return "3.4.0";
            }
            if (systemtype == "Windows 10 or Windows 11" || systemtype == "Windows 8.1" || systemtype == "Windows 8")
            {
                return "3.9.0";
            }
            if (systemtype == "Windows Server 2003 or XP x64 Edition")
            {
                return "3.4.0";
            }
            return "";
        }
    }
}
