using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using WADY.Core;
using System.Threading;

namespace WADY_GUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        int TimerTick;
        Timer TaskTimer;
        WADYProcessHelper ProcessHelper;

        public MainWindow()
        {
            InitializeComponent();

            ProcessHelper = new WADYProcessHelper();

            //Timer;
            TimerTick = 500; // 500ms 
            TaskTimer = new Timer(Excute, null, 0, TimerTick); 
            
        }

        static void Excute(object obj)
        {
            
        }





        int abc(int b)
        {
            return 1;
        }

    }
}
