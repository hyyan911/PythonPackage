using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using Python.Deployment;
using System.Threading;
using System.Reflection;
using static Microsoft.Scripting.Hosting.Shell.SuperConsole;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PythonHandler;
using static System.Net.WebRequestMethods;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace PythonHandler
{
    /// <summary>
    /// Python解释器
    /// </summary>
    public class Python_NetInterpretor
    {
        internal static bool IsInitialized = false;

        internal static string website1 = "https://pypi.tuna.tsinghua.edu.cn/simple/";

        internal static string website2 = "https://mirrors.cloud.tencent.com/pypi/simple/ ";

        internal static string webname = "pypi.tuna.tsinghua.edu.cn";

        internal static int endcode = 0;

        internal static void StateGuesser(string str)
        {
            if (str.Contains("exit code"))
            {
                if (str.Contains("exit code 0"))
                {
                    endcode = 0;
                    return;
                }
                endcode = 1;
                return;
            }
            endcode = 0;
        }

        #region 外部方法
        internal static void InitializeConfig()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            GetSystemParams();
            PythonInstallPath = Path.Combine(Environment.CurrentDirectory, "Python" + PythonVersion);
        }

        /// <summary>
        /// 初始化Python环境(注意此方法是阻塞的，所以一般情况下不要放在UI线程中执行)
        /// </summary>
        /// <param name="installevent"></param>
        /// <param name="endingAction"></param>
        public static void Initialize(Action<string> installevent, Action endingAction)
        {
            InitializeConfig();
            AppDomain.CurrentDomain.ProcessExit += DosposePython;
            InstallMessageDelegate += installevent;
            if (IsPythonInstalled() && IsPipInstalled())
            {
                IsInitialized = true;
                return;
            }
            //安装python
            SetupPython();

            if (!IsPythonInstalled())
            {
                InstallMessageDelegate?.Invoke("安装Python" + PythonVersion + "失败");
                Thread.Sleep(1000);
                endingAction?.Invoke();
                return;
            }
            try
            {
                if (!IsPipInstalled())
                {
                    bool result = InstallPip();
                    if (result == false)
                    {
                        InstallMessageDelegate?.Invoke("安装Pip失败");
                        Thread.Sleep(1000);
                        endingAction?.Invoke();
                        return;
                    }
                }

                InstallMessageDelegate?.Invoke("Python环境配置成功");
                endingAction?.Invoke();
                PythonFunctionListener.Initialize();
                return;
            }
            catch (Exception ex)
            {
                InstallMessageDelegate?.Invoke("Python环境配置失败，原因:\n" + ex.Message);
                Thread.Sleep(1000);
                endingAction?.Invoke();
                return;
            }
        }

        /// <summary>
        /// 下载并安装包
        /// </summary>
        /// <param name="modulename"></param>
        /// <returns></returns>
        public static bool DownloadWheelAndInstall(string modulename)
        {
            if (IsModuleInstalled(modulename)) return true;
            InstallMessageDelegate?.Invoke("正在下载" + modulename);
            string pippath = Path.Combine(PythonInstallPath, "Scripts", "pip.exe");
            bool result = RunCommand(400000, "\"" + pippath + "\" " + "download " + modulename + " --only-binary=:all: -d \"" + Path.Combine(PythonInstallPath, "downloads") + "\" -i " + website1 + " --trusted-host " + webname, out string str);
            if (result == false)
            {
                result = RunCommand(400000, "\"" + pippath + "\" " + "download " + modulename + " -d \"" + Path.Combine(PythonInstallPath, "downloads") + "\"", out str);
                if (result == false)
                {
                    InstallMessageDelegate?.Invoke("安装" + modulename + "失败");
                    return false;
                }
            }
            InstallMessageDelegate?.Invoke("正在安装" + modulename);
            result = RunCommand(400000, "\"" + pippath + "\" " + "install --no-index --find-links \"" + Path.Combine(PythonInstallPath, "downloads") + "\" " + modulename, out str);
            if (result == false)
            {
                InstallMessageDelegate?.Invoke("安装" + modulename + "失败");
                return false;
            }
            InstallMessageDelegate?.Invoke("安装成功！");
            return true;
        }

        /// <summary>
        /// Python是否已经安装
        /// </summary>
        /// <returns></returns>
        public static bool IsPythonInstalled()
        {
            return System.IO.File.Exists(Path.Combine(PythonInstallPath, "python.exe"));
        }

        /// <summary>
        /// Pip是否已安装
        /// </summary>
        /// <returns></returns>
        public static bool IsPipInstalled()
        {
            return System.IO.File.Exists(Path.Combine(PythonInstallPath, "Scripts", "pip.exe"));
        }

        /// <summary>
        /// 安装pip
        /// </summary>
        /// <returns></returns>
        public static bool InstallPip()
        {
            string text = Path.Combine(PythonInstallPath);
            if (!Directory.Exists(text))
            {
                Directory.CreateDirectory(text);
            }

            string downloadUrl = "https://bootstrap.pypa.io/get-pip.py";
            string outputFilePath = Path.Combine(text, "get-pip.py");
            try
            {
                InstallMessageDelegate?.Invoke("正在下载pip...");
                Download(downloadUrl, outputFilePath, delegate (float progress)
                {
                    InstallMessageDelegate?.Invoke("下载进度：" + Math.Round(progress, 2).ToString() + "%");
                });
                InstallMessageDelegate?.Invoke("下载完成!");
            }
            catch (Exception ex)
            {
                InstallMessageDelegate?.Invoke("下载pip时出现问题" + ex.Message);
                return false;
            }
            RunCommand(40000, "cd \"" + PythonInstallPath + "\" && python.exe get-pip.py", out string str);
            return true;
        }

        /// <summary>
        /// 获取已安装包列表(包名，版本)
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> GetInstalledPackageList()
        {
            if (!IsPipInstalled() || !IsPythonInstalled()) return new Dictionary<string, string>();
            string pippath = Path.Combine(PythonInstallPath, "Scripts", "pip.exe");
            bool result = RunCommand(3000, "\"" + pippath + "\" " + "list", out string output);

            Dictionary<string, string> resultdic = new Dictionary<string, string>();

            Regex reg = new Regex("[a-zA-Z_0-9]+[ ]+[0-9.]+");
            MatchCollection coll = reg.Matches(output);
            foreach (Match match in coll)
            {
                string name = match.Value.Substring(0, match.Value.IndexOf(" "));
                string version = match.Value.Substring(match.Value.LastIndexOf(" ") + 1, match.Value.Length - match.Value.LastIndexOf(" ") - 1);
                resultdic.Add(name, version);
            }
            return resultdic;
        }

        /// <summary>
        /// 删除包
        /// </summary>
        public static bool DeletePackage(string name)
        {
            string pippath = Path.Combine(PythonInstallPath, "Scripts", "pip.exe");
            bool result = RunCommand(3000, "\"" + pippath + "\" " + "uninstall " + name+" -y", out string output);
            return result;
        }

        /// <summary>
        /// 运行Python脚本中的函数，输入参数，返回结果
        /// </summary>
        /// <param name="pypath">待执行脚本文件路径</param>
        /// <param name="funcname">函数名</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="param">函数参数</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static dynamic ExcuteFunction(string pypath, string funcname, TimeSpan timeout, params object[] param)
        {
            if (!IsInitialized)
                throw new NullReferenceException("调用ExcuteFunction方法前需要先进行初始化");

            if (!PythonFunctionListener.IsInitialized)
            {
                PythonFunctionListener.Initialize();
            }

            return PythonFunctionListener.RunFunction(pypath, funcname, param.ToList(), (int)timeout.TotalMilliseconds);
        }

        #endregion

        private static bool IsPipSearchInstalled()
        {
            return System.IO.File.Exists(Path.Combine(PythonInstallPath, "Scripts", "pip_search.exe"));
        }


        private static void DosposePython(object sender, EventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// 销毁资源
        /// </summary>
        internal static void Dispose()
        {
            PythonFunctionListener.Dispose();
        }


        #region python环境配置部分

        /// <summary>
        /// 运行cmd指令
        /// </summary>
        internal static bool RunCommand(int timeout, string commands, out string output)
        {
            Process process = new Process();
            try
            {
                new ProcessStartInfo();
                string text;
                string text2;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    text = "/bin/bash";
                    text2 = "-c \"" + commands + "\"";
                }
                else
                {
                    text = "cmd.exe";
                    text2 = "/C \"" + commands + "\"";
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = text,
                    Arguments = text2,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                string outstr = "";
                process.StartInfo = startInfo;
                process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    InstallMessageDelegate?.Invoke(e.Data);
                    outstr += e.Data;
                });
                process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    InstallMessageDelegate?.Invoke(e.Data);
                });
                bool res = process.Start();
                if (res == false)
                {
                    endcode = -1;
                    output = outstr;
                    return false;
                }
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                res = process.WaitForExit(timeout);
                if (process.ExitCode != 0 || res == false)
                {
                    output = outstr;
                    return false;
                }
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                }
                output = outstr;
                return true;
            }
            catch (Exception ex)
            {
                InstallMessageDelegate?.Invoke("RunCommand: Error with command: '" + commands + "'\r\n" + ex.Message);
                output = "";
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// 安装信息委托
        /// </summary>
        public static event Action<string> InstallMessageDelegate = null;

        internal static string PythonInstallPath = "";

        /// <summary>
        /// 下载python_embedding并解压到当前文件夹中
        /// </summary>
        internal static void SetupPython()
        {
            try
            {
                string res = "";
                //查找目标文件
                using (StreamReader reader = new StreamReader(System.IO.File.Open(Path.Combine(PythonInstallPath, "python" + PythonVersion.Replace(".", "").Replace("0", "") + "._pth"), FileMode.Open)))
                {
                    res = reader.ReadToEnd();
                }
                if (res != "")
                {
                    using (StreamWriter writer = new StreamWriter(System.IO.File.Create(Path.Combine(PythonInstallPath, "python" + PythonVersion.Replace(".", "").Replace("0", "") + "._pth"))))
                    {
                        res = res.Replace("#import site", "import site");
                        writer.Write(res);
                    }
                }
            }
            catch (Exception exc) { }
            if (IsPythonInstalled()) { return; }
            if (!Directory.Exists(PythonInstallPath))
            {
                Directory.CreateDirectory(PythonInstallPath);
            }
            //下载Python压缩包
            string zip = DownloadPythonZip(PythonInstallPath);
            if (string.IsNullOrWhiteSpace(zip))
            {
                InstallMessageDelegate?.Invoke("无法从指定源下载Python包:" + GetPackageDownloadPath());
                return;
            }

            Task t = Task.Run(delegate
            {
                try
                {
                    ZipFile.ExtractToDirectory(zip, PythonInstallPath);
                }
                catch (Exception exc)
                {
                    InstallMessageDelegate?.Invoke("解压Python包时发生错误" + zip + "\n" + exc.Message);
                }
            });
            t.Wait();
        }

        /// <summary>
        /// 模块是否安装
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        internal static bool IsModuleInstalled(string module)
        {
            if (!IsPythonInstalled())
            {
                return false;
            }

            string text = Path.Combine(PythonInstallPath, "Lib", "site-packages", module);
            if (Directory.Exists(text))
            {
                return System.IO.File.Exists(Path.Combine(text, "__init__.py"));
            }

            return false;
        }

        /// <summary>
        /// 系统名称
        /// </summary>
        internal static string SystemName = "";

        internal static string PythonVersion = "";

        static bool Is64Bit = false;

        private static void GetSystemParams()
        {
            SystemName = SystemTools.GetSystemName();
            PythonVersion = SystemTools.GetNewestPythonVersion(SystemName);
            InstallMessageDelegate?.Invoke("当前系统版本：" + SystemName);
            InstallMessageDelegate?.Invoke("需要下载的Python版本：" + PythonVersion);
            PythonVersion = SystemTools.GetNewestPythonVersion(SystemName);
            Is64Bit = Environment.Is64BitProcess;
            return;
        }
        /// <summary>
        /// 根据不同Win设备型号获取下载链接
        /// </summary>
        /// <returns></returns>
        private static string GetPackageDownloadPath()
        {
            if (PythonVersion == "")
            {
                InstallMessageDelegate?.Invoke("找不到和系统适配的Python版本");
                return "";
            }
            string baseurl = "https://www.python.org/ftp/python/";

            baseurl += PythonVersion + "/";
            baseurl += "python-" + PythonVersion + "-embed-";
            if (Is64Bit)
            {
                baseurl += "amd64.zip";
            }
            else
            {
                baseurl += "win32.zip";
            }

            return baseurl;
        }

        internal static string DownloadPythonZip(string destinationDirectory)
        {
            string downloadUri = GetPackageDownloadPath();
            string zipFile = Path.Combine(destinationDirectory, Path.GetFileName(new Uri(downloadUri).LocalPath));
            try
            {
                if (System.IO.File.Exists(zipFile))
                {
                    System.IO.File.Delete(zipFile);
                }
                InstallMessageDelegate?.Invoke("正在下载Python安装包...");
                Download(downloadUri, zipFile, delegate (float progress)
                {
                    InstallMessageDelegate?.Invoke("当前进度:" + Math.Round(progress, 2).ToString() + "%");
                });
                InstallMessageDelegate?.Invoke("下载完成!");
                return zipFile;
            }
            catch (Exception ex)
            {
                InstallMessageDelegate?.Invoke("下载过程中出现异常: " + ex.Message);
                return string.Empty;
            }
        }

        private static readonly HttpClient httpClient = new HttpClient() { };
        internal static void Download(string downloadUrl, string outputFilePath, Action<float> progress = null, CancellationToken token = default(CancellationToken))
        {
            try
            {
                using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    Task t = httpClient.DownloadWithProgressAsync(downloadUrl, fileStream, progress, token);
                    t.Wait();
                }
            }
            catch
            {
                if (System.IO.File.Exists(outputFilePath))
                {
                    System.IO.File.Delete(outputFilePath);
                }

                throw;
            }
        }
        #endregion
    }
}
