using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace PythonHandler
{
    /// <summary>
    /// C#和python的数据转换类
    /// 目前支持的数据类型：int,double,string,bool,null,列表及其嵌套
    /// </summary>
    internal class DataConverter
    {
        /// <summary>
        /// C#向Json数据转换时的类型检查
        /// </summary>
        /// <returns></returns>
        public static bool TypeCheck(object item)
        {
            if (item is double || item is bool || item is null || item is string || item is char)
            {
                return true;
            }
            if (item is IList)
            {
                bool check = true;
                foreach (var ele in item as IList)
                {
                    check &= TypeCheck(ele);
                    if (check == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 将CSharp数据转换成Json类型
        /// </summary>
        /// <returns></returns>
        public static string CSharpToJson(object item)
        {
            return JsonConvert.SerializeObject(item);
        }

        /// <summary>
        /// 将python传回的json数据转换成C#类型
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static dynamic JonToCSharp(string dataJson)
        {
            JToken t = JToken.Parse(dataJson);
            return ParseNodeValue(t);
        }

        private static dynamic ParseNodeValue(JToken t)
        {
            JTokenType kind = t.Type;
            if (kind == JTokenType.Object)
            {
                throw new Exception("C#和Python的数据传递不允许存在复杂的数据结构");
            }
            //不是数组
            if (kind != JTokenType.Array)
            {
                if (kind == JTokenType.Float || kind == JTokenType.Integer)
                    return t.ToObject<double>();
                if (kind == JTokenType.Boolean)
                    return t.ToObject<bool>();
                if (kind == JTokenType.String)
                    return t.ToObject<string>();
                if (kind == JTokenType.Guid)
                    return t.ToObject<Guid>();
                if (kind == JTokenType.Null)
                    return null;
                if (kind == JTokenType.Bytes)
                    return t.ToObject<byte[]>().ToArray();
                if (kind == JTokenType.Date)
                    return t.ToObject<DateTime>();
                else
                    return null;
            }
            else
            {
                //是数组
                List<object> list = new List<object>();
                foreach (var item in t.ToArray())
                {
                    list.Add(ParseNodeValue(item));
                }
                return list;
            }
        }
    }
}
