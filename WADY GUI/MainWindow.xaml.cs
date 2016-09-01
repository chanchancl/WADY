using System.Windows;
using System.Windows.Forms;
using WADY.Core;
using System.Threading;

namespace WADY_GUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        static double WindowWidth = 1024;
        static double WindowHeight = 768;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

            tabControl.Width = this.Width;
            tabControl.Height = this.Height * 9/16;

        }
    }
}
