using Python.Deployment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace PythonHandler
{
    /// <summary>
    /// C#程序向cmd发送指令，并等待python程序返回结果
    /// </summary>
    internal class PythonFunctionListener
    {
        internal static Process PythonProcess = new Process();

        public PythonFunctionListener()
        {
        }

        static object lockobject = new object();

        /// <summary>
        /// 初始化
        /// </summary>
        internal static void Initialize()
        {
            //配置Python解释器
            PythonProcess.StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Python_NetInterpretor.PythonInstallPath, "python.exe"),
                Arguments = "-i",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            PythonProcess.Start();
            PythonProcess.OutputDataReceived += ReceiveOutput;
            PythonProcess.ErrorDataReceived += ReceiveError;
            PythonProcess.BeginErrorReadLine();
            PythonProcess.BeginOutputReadLine();

            PythonProcess.StandardInput.WriteLine("# coding=gb2312\n" + "import sys\n" + "import json");
            PythonProcess.StandardInput.WriteLine("import warnings\n" + "warnings.filterwarnings(\"ignore\")\n");

            Thread.Sleep(2000);
            OutputBuffer = "";
            ErrorBuffer = "";

        }

        /// <summary>
        /// 关闭解释器进程，释放资源
        /// </summary>
        internal static void Dispose()
        {
            PythonProcess.StandardInput.WriteLine("exit()");
            PythonProcess.Kill();
        }

        static string OutputBuffer { get; set; } = "";

        static string ErrorBuffer { get; set; } = "";

        private static void ReceiveError(object sender, DataReceivedEventArgs e)
        {
            lock (lockobject)
            {
                if (!string.IsNullOrEmpty(e.Data))
                    ErrorBuffer += e.Data;
            }
        }

        private static void ReceiveOutput(object sender, DataReceivedEventArgs e)
        {
            lock (lockobject)
            {
                if (!string.IsNullOrEmpty(e.Data))
                    OutputBuffer += e.Data;
            }
        }


        /// <summary>
        /// 运行cmd指令
        /// </summary>
        internal static string RunCommand(int timeout, string command, out string errout)
        {
            lock (lockobject)
            {
                OutputBuffer = "";
                ErrorBuffer = "";
            }
            string input = command;
            PythonProcess.StandardInput.AutoFlush = true;
            string input1 = input.Replace(";", "\n");
            PythonProcess.StandardInput.WriteLine(input1);
            int time = 0;
            while (!OutputBuffer.Replace("print('python_code_finished')", "").Contains("python_code_finished") && time < timeout)
            {
                Thread.Sleep(20);
                time += 20;
                if (ErrorBuffer != "")
                {
                    throw new Exception(ErrorBuffer);
                }
            }
            if (time >= timeout)
            {
                errout = ErrorBuffer + "\nError: TimeOut";
                if (errout != "")
                {
                    throw new Exception(errout);
                }
                return "";
            }
            errout = ErrorBuffer;
            return OutputBuffer.Replace(input, "").Replace(Environment.CurrentDirectory + ">", "").Replace("python_code_finished", "");
        }

        /// <summary>
        /// 运行脚本中的函数
        /// </summary>
        /// <param name="pypath"></param>
        /// <param name="funcname"></param>
        /// <param name="param"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        internal static dynamic RunFunction(string pypath, string funcname, List<object> param, int timeout)
        {
            string code = GeneratePythonCode(pypath, funcname, param);
            string result = RunCommand(timeout, code, out string err);
            return DataConverter.JonToCSharp(result);
        }


        /// <summary>
        /// 根据给定的pypath和函数名得到可执行的py脚本代码
        /// </summary>
        /// <returns></returns>
        public static string GeneratePythonCode(string pypath, string funcname, List<object> param)
        {
            if (!File.Exists(pypath))
            {
                throw new Exception("给定路径的文件不存在");
            }

            string directoryPath = Path.GetDirectoryName(pypath);
            string filename = Path.GetFileName(pypath).Replace(".py", "");

            string code = "";
            //导入模块的代码
            code += "sys.path.append(" + "'" + directoryPath.Replace("\\", "/") + "'" + ")\n";
            code += "import " + filename + "\n";

            int paramcount = 0;
            string functioncontent = "";

            if (funcname != "")
            {
                //如果是求函数值则输入函数表达式
                foreach (var item in param)
                {
                    if (!DataConverter.TypeCheck(item))
                    {
                        throw new Exception("待传送的C#数据存在不支持的数据类型");
                    }
                    string json = DataConverter.CSharpToJson(item);
                    code += "CSharp_ConvertParam_" + paramcount.ToString() + "=json.loads(" + "\'" + json + "\'" + ")\n";
                    functioncontent += "CSharp_ConvertParam_" + paramcount.ToString() + ",";
                    ++paramcount;
                }
                if (param.Count != 0)
                {
                    functioncontent = functioncontent.Substring(0, functioncontent.Length - 1);
                }

                //定义函数
                code += "CSharp_res=" + filename + "." + funcname + "(" + functioncontent + ")\n";

                code += "print(json.dumps(CSharp_res))\n";

                code += "print('python_code_finished')\n";
                code += "sys.stdout.flush()\n";
            }

            return code;
        }
    }
}
