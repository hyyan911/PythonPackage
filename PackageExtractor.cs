

using PythonPackage.Properties;
using System.IO.Compression;

namespace PythonPackage
{
    /// <summary>
    /// Python抽取类
    /// </summary>
    internal class PackageExtractor
    {
        /// <summary>
        /// 抽取文件夹名
        /// </summary>
        public static string FolderName = "PythonPackages";

        public static bool HasNewestPackage()
        {
            return Directory.Exists(Environment.CurrentDirectory + "\\" + FolderName);
        }

        /// <summary>
        /// 检查当前目录下是否存在Python包
        /// </summary>
        static PackageExtractor()
        {
            if (HasNewestPackage() == false)
            {
                //解压并复制到当前文件夹
                FileStream fs = new FileStream(Environment.CurrentDirectory + "\\" + "temp.zip", FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(Resources.site_packages, 0, Resources.site_packages.Length);
                fs.Close();
                ZipFile.ExtractToDirectory(Environment.CurrentDirectory + "\\" + "temp.zip", Environment.CurrentDirectory + "\\" + FolderName);
                File.Delete(Environment.CurrentDirectory + "\\" + "temp.zip");
            }
            else
            {
                return;
            }
        }
    }
}
