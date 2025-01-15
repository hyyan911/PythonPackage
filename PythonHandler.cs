using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;

namespace PythonPackage
{
    /// <summary>
    /// Python解释器
    /// </summary>
    public class Python_NetInterpretor
    {
        public static string PackageFolder { get; internal set; } = "PythonPackages";

        public static ScriptEngine Engine = null;

        public void Initialize()
        {
            Engine = Python.CreateEngine();

            //设置依赖包位置
            var paths = Engine.GetSearchPaths();
            paths.Add(Environment.CurrentDirectory + "\\" + PackageFolder);
            Engine.SetSearchPaths(paths);
        }

        /// <summary>
        /// 运行Python脚本，输入参数，返回结果(Python中的函数输入必须是一个列表形式,输出也必须是一个列表)
        /// </summary>
        /// <param name="codePath"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public List<object> RunPythonCode(string codePath, string funcName, List<object> param, out Exception exc)
        {
            if (Engine == null)
            {
                throw new NullReferenceException("调用此方法前需要先进行初始化");
            }

            ScriptScope funcs = Engine.ExecuteFile(codePath);

            dynamic funcmethod = funcs.GetVariable(funcName);
            IronPython.Runtime.PythonList list = new IronPython.Runtime.PythonList();
            list.AddRange(param);
            try
            {
                PythonList result = funcmethod(list);
                exc = null;
                return DecodeList(result);
            }
            catch (Exception e)
            {
                exc = e;
                return null;
            }
        }

        internal static List<object> DecodeList(PythonList list)
        {
            List<object> result = new List<object>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || list[i] is string || list[i] is double || list[i] is int || list[i] is bool)
                {
                    result.Add(list[i]);
                    continue;
                }
                if (list[i] is PythonList)
                {
                    result.Add(DecodeList(list[i] as PythonList));
                    continue;
                }
                if (list[i] is PythonDictionary)
                {
                    result.Add(DecodeDict(list[i] as PythonDictionary));
                    continue;
                }
                throw new Exception("无法转换特殊的数据类型:" + list[i].GetType().ToString());
            }
            return result;
        }

        internal static Dictionary<object, object> DecodeDict(PythonDictionary list)
        {
            PythonList reskeys = new PythonList();
            reskeys.AddRange(list.Keys);

            PythonList resvalues = new PythonList();
            resvalues.AddRange(list.Values);

            List<object> l1 = DecodeList(reskeys);
            List<object> l2 = DecodeList(resvalues);

            Dictionary<object, object> result = new Dictionary<object, object>();

            for (int i = 0; i < l1.Count; i++)
            {
                result.Add(l1[i], l2[i]);
            }
            return result;
        }


    }
}
