using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PythonHandler
{
    /// <summary>
    /// PythonManager.xaml 的交互逻辑
    /// </summary>
    public partial class PythonManager : Grid
    {
        public PythonManager()
        {
            InitializeComponent();
            Python_NetInterpretor.InitializeConfig();
            UpdatePythonInstallationState();
            Python_NetInterpretor.InstallMessageDelegate += AddToControllerPanel;
        }

        private void AddToControllerPanel(string obj)
        {
            Dispatcher.Invoke(() =>
            {
                ControllPanel.Text += obj + "\n";
            });
        }

        private void RefreshPackages(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    PackageRefreshBtn.IsEnabled = false;
                    ControllPanel.Text += ">>> 获取已安装包列表..." + "\n";
                });
                Dictionary<string, string> dic = Python_NetInterpretor.GetInstalledPackageList();
                Dispatcher.Invoke(() =>
                {
                    PythonPackageList.ClearItems();
                    foreach (var item in dic)
                    {
                        PythonPackageList.AddItem(item.Key, item.Key, item.Value);
                    }
                    PackageRefreshBtn.IsEnabled = true;
                });
            });
            t.Start();
        }

        /// <summary>
        /// 安装指定包
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallPackage(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                string text = "";
                Dispatcher.Invoke(() =>
                {
                    InstallBtn.IsEnabled = false;
                    ControllPanel.Text += ">>> 安装" + InstallPackageNameText.Text + "\n";
                    text = InstallPackageNameText.Text;
                });
                Python_NetInterpretor.DownloadWheelAndInstall(text);
                Dispatcher.Invoke(() =>
                {
                    RefreshPackages(null, new RoutedEventArgs());
                    InstallBtn.IsEnabled = true;
                });
            });
            t.Start();
        }

        /// <summary>
        /// 刷新Python安装情况
        /// </summary>
        public void UpdatePythonInstallationState()
        {
            if (Python_NetInterpretor.IsPipInstalled())
            {
                PipState.Content = "已安装";
            }
            else
            {
                PipState.Content = "未安装";
            }
            if (Python_NetInterpretor.IsPipInstalled())
            {
                PythonVersion.Content = Python_NetInterpretor.PythonVersion;
            }
            else
            {
                PythonVersion.Content = "未安装";
            }
        }

        /// <summary>
        /// 安装python环境
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallPython(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    PackageRefreshBtn.IsEnabled = false;
                });
                Python_NetInterpretor.Initialize(null, null);
                Dispatcher.Invoke(() =>
                {
                    PackageRefreshBtn.IsEnabled = true;
                });
            });
            t.Start();
        }

        public delegate bool ConfirmMethod(string packagename);

        /// <summary>
        /// 删除时的确认方法，如果此方法返回true则确认删除,否则不删除
        /// </summary>
        public ConfirmMethod DeleteConfirmMethod = null;

        private void PythonPackageList_ItemContextMenuSelected(int arg1, int arg2, object arg3)
        {
            bool conres = true;
            if (DeleteConfirmMethod != null)
            {
                conres = DeleteConfirmMethod.Invoke((string)arg3);
            }
            if (conres == false)
            {
                return;
            }
            Thread t = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    PythonPackageList.IsEnabled = false;
                });
                //删除库
                bool result = Python_NetInterpretor.DeletePackage((string)arg3);
                Dispatcher.Invoke(() =>
                {
                    if (result == true)
                    {
                        ControllPanel.Text += ">>> 成功卸载库" + (string)arg3 + "\n";
                    }
                    else
                    {
                        ControllPanel.Text += ">>> 卸载库" + (string)arg3 + "失败" + "\n";
                    }
                    RefreshPackages(null, new RoutedEventArgs());
                    PythonPackageList.IsEnabled = true;
                });
            });
            t.Start();
        }

        private void ClearCommands(object sender, RoutedEventArgs e)
        {
            ControllPanel.Text = "";
        }
    }
}
