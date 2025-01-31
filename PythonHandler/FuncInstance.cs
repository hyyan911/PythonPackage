using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonHandler
{

    /// <summary>
    /// Python函数实例
    /// </summary>
    public class FuncInstance
    {
        /// <summary>
        /// 输入参数
        /// </summary>
        public List<string> InputParams { get; internal set; } = new List<string>();

        /// <summary>
        /// 函数名
        /// </summary>
        public string FuncName { get; internal set; } = "";

        /// <summary>
        /// 脚本文件路径
        /// </summary>
        public string FuncPath { get; internal set; } = "";

        /// <summary>
        /// 执行函数
        /// </summary>
        /// <returns></returns>
        public dynamic Excute(int timeout, params object[] ps)
        {
            return PythonFunctionListener.RunFunction(FuncPath, FuncName, ps.ToList(), timeout);
        }
    }


}
