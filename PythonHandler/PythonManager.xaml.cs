using Controls;
using Controls.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
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
        /// <summary>
        /// 列表数据分隔符
        /// </summary>
        public char SplitCharacter { get; set; } = ',';

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

        /// <summary>
        /// 选择Python脚本路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectPythonFilePath(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Python脚本文件 (*.py)|*.py";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                PythonFileDir.Text = dlg.FileName;
                PythonFileDir.ToolTip = dlg.FileName;
            }
            //刷新函数列表
            UpdateFuncPanel();
        }

        /// <summary>
        /// 刷新函数列表
        /// </summary>
        public void UpdateFuncPanel()
        {
            try
            {
                List<FuncInstance> funcs = Python_NetInterpretor.GetFuncs(PythonFileDir.Text);
                PyhtonFunc.Items.Clear();
                foreach (var item in funcs)
                {
                    DecoratedButton btn = new DecoratedButton() { Text = item.FuncName };
                    PyhtonFunc.CloneStyleTo(btn);
                    btn.FontSize = 10;
                    btn.Tag = item;
                    btn.Click += ShowPythonFuncInformation;
                    PyhtonFunc.Items.Add(btn);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 显示函数信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPythonFuncInformation(object sender, RoutedEventArgs e)
        {
            InputParam.ClearItems();
            DecoratedButton btn = sender as DecoratedButton;
            foreach (var item in (btn.Tag as FuncInstance).InputParams)
            {
                InputParam.AddItem(null, item, "");
            }
        }

        private void ShowParamInformation(object sender, RoutedEventArgs e)
        {
            MessageWindow.ShowTipWindow("参数格式说明:\n1.输入数据类型支持列表，数据型，布尔型和字符串，字符串用\"\"包围，布尔型用True，False表示，列表的不同元素之间用\"" + SplitCharacter.ToString() + "\"隔开,并用{}包围", null);
        }

        /// <summary>
        /// 运行脚本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunPython(object sender, RoutedEventArgs e)
        {
            if (PyhtonFunc.SelectedItem == null)
            {
                ControllPanel.Text += "未选择待执行函数，已终止运行\n";
                return;
            }
            int timeout = 5000;
            try
            {
                timeout = int.Parse(Timeout.Text);
            }
            catch (Exception)
            {
                ControllPanel.Text += "超时时间参数设置错误，已终止运行\n";
                return;
            }

            FuncInstance func = PyhtonFunc.SelectedItem.Tag as FuncInstance;
            ControllPanel.Text += "计算" + func.FuncName + "...\n";
            //读取参数值
            int row = InputParam.GetRowCount();
            List<object> objs = new List<object>();
            for (int i = 0; i < row; i++)
            {
                objs.Add(InputParam.GetTag(i));
            }

            Thread t = new Thread(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    RunResult.IsEnabled = false;
                });
                try
                {
                    object result = func.Excute(timeout, objs);
                    Dispatcher.Invoke(() =>
                    {
                        ControllPanel.Text += "计算已完成\n";
                        RunResult.IsEnabled = true;
                        RunResult.Tag = result;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ControllPanel.Text += "发生异常，函数执行已终止\n";
                    });
                }
            });
            t.Start();
        }

        /// <summary>
        /// 处理输入值
        /// </summary>
        /// <returns></returns>
        private object ProcessSingleValue(string value)
        {
            if (value.Contains("\""))
            {
                //字符串
                return value.Replace("\"", "");
            }
            if (value == "True" || value == "False")
            {
                return value == "True" ? true : false;
            }
            try
            {
                return double.Parse(value);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 输入参数
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void OpenInput(int arg1, object arg2)
        {
            DataDisplayWindow win = new DataDisplayWindow(arg2);
            win.ShowDialog();
            if (win.SourceLink == null) return;
            InputParam.SetTag(arg1, win.SourceLink);
            if (win.SourceLink is IList)
            {
                InputParam.SetCelValue(arg1, 1, "List[" + (win.SourceLink as IList).Count.ToString() + "]");
            }
            else
            {
                InputParam.SetCelValue(arg1, 1, win.SourceLink);
            }
        }

        /// <summary>
        /// 显示运行结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowPythonResult(object sender, RoutedEventArgs e)
        {
            if ((sender as DecoratedButton).Tag == null) return;
            DataDisplayWindow win = new DataDisplayWindow((sender as DecoratedButton).Tag);
            win.ShowDialog();
        }
    }
}
