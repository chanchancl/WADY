using System;
using System.Windows;
using System.Windows.Controls;
using WADY.Core;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Data;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace WADY_GUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        WADYProcessHelper helper;
        public MainWindow()
        {
            InitializeComponent();
            helper = new WADYProcessHelper();
            helper.StartTask();

            helper.AddDelgate(ListViewUpdate);
        }



        [DllImport("coredll.dll", SetLastError = true)]
        private static extern IntPtr ExtractIconEx(string fileName, int index, ref IntPtr hIconLarge, ref IntPtr hIconSmall, uint nIcons);
        //List<WADY.Core.WADYProcessHelper.ProcessInfo> bindingData;
        ObservableCollection<WADYProcessHelper.ProcessInfo> bindingData
            = new ObservableCollection<WADYProcessHelper.ProcessInfo>();

        public delegate void UpdateUIDelegate();
        private void ListViewUpdate()
        {
            //var col = GridView.GetColumnCollection(listView);

            //var view = listView.View as GridView;

            //ListViewItem a;
            //listView.ItemsSource = helper.QueryTotalTimeList();
            try
            {
                UpdateUIDelegate updateUIDelegate = new UpdateUIDelegate(update);

                //通过调用委托
                this.listView.Dispatcher.Invoke(updateUIDelegate);

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }


        }
        private void update()
        {
            //对Listview绑定的数据源进行更新
            var tmpData = helper.QueryTotalTimeList();
            bindingData.Clear();
            foreach (var i in tmpData)
            {
                bindingData.Add(i);
            }
            
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region
            /*double SH = Screen.PrimaryScreen.Bounds.Height;
            double SW = Screen.PrimaryScreen.Bounds.Width;

            double WDH = SW / SH;
            double WDH4D3 = (double)1024 / (double)768;
            double WDH16D9 = (double)1920 / (double)1080;*/

            /*
             * SystemInformation.WorkingArea 除去任务栏剩下的区域
             * Screen.PrimaryScreen.Bounds   显示器的分辨率
             * 
             * 有没有啥能获取屏幕的实际大小
             * 比如我的电脑分辨率为 1920*1080，但用了 125%的放大。
             */

            // 判断屏幕的横纵比
            /*if(WDH == WDH4D3)
            {
                // 4/3

            }
            else if (WDH == WDH16D9)
            {
                // 16/9
                
                //this.Width = SystemInformation.WorkingArea.Width * 0.75f;
                //this.Height = SH * 0.6666667;
            }*/
            #endregion

            // 适应窗口的实际大小
            menu.Width = listView.Width = tabControl.Width = this.Width;
            listView.Height = tabControl.Height = this.Height * 9/16;

            // 获取该listview对应的 列集合
            //var col = GridView.GetColumnCollection(listView);

            listView.ItemsSource = bindingData;

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //结束当前进程
            Process.GetCurrentProcess().Kill();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = sender as ListView;
            //MessageBox.Show( (lv.SelectedItem as WADY.Core.WADYProcessHelper.ProcessInfo).ProcessName );

        }

        private void Button_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MessageBox.Show("button down.");
        }
    }
}
