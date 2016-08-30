using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WADY.Core
{
    class Program
    {

        static void Main(string[] args)
        {
            WADYProcessHelper helper = new WADYProcessHelper();
            helper.StartTask();
            while (true)
            {
                var list = helper.QueryTotalTimeList();
                int count = 1;

                foreach(var item in list)
                {
                    Console.WriteLine("{0}. {1} 在前台运行了 {2} s   {3}",count, item.ProcessDescription, item.TotalTime, Process.GetCurrentProcess().TotalProcessorTime);
                    count++;
                }
                Thread.Sleep(500);
                Console.Clear();
                
            }
        }

    }

    public class WADYProcessHelper
    {

        #region data struct
        /*
         * InfoMap 
         *    | -- ProcessName1 -> ProcessInfo1
         *    |                         | -- Name,Path,Description,
         *    |                              TotalTime,ProcessTimeInfo(It's a list)
         *    |                                               | -- TimeInfo5, TimeInfo4, ... ,TimeInfo1
         *    |                                                    (倒序排列)
         *    | -- ProcessName2 -> ProcessInfo2
         *    | -- ProcessName3 -> ProcessInfo3
         *    
         *  
         *    
        */

        /*
            记录一个进程的使用情况
            同名的进程，将使用相同信息，这个信息使用一个Dictionary，由进程名，映射到该结构。

            TotalTime为这个进程在前台的总时间。

            ProcessTimeInfo为一个列表，记录了每次切换到该进程时的时间，以及使用时间（截止到切换别的进程）
            这个列表倒着排列，

        */
        #endregion
        public class ProcessInfo
        {
            public ProcessInfo()
            {
                ProcessTimeInfo = new List<TimeInfo>();
                TotalTime = TimeSpan.Zero;
            }
            public class TimeInfo
            {
                public DateTime Start; // 切换到的时间
                public TimeSpan Last;  // 这次的持续时间
            }
            public string ProcessName; // 进程名
            public string ProcessPath; // 进程的路径  有些进程，比如任务管理器，会拒绝这一请求
            public string ProcessDescription; // 进程的文件描述

            public TimeSpan TotalTime;
            public List<TimeInfo> ProcessTimeInfo;
        }

        public bool Error;
        public string LastProcessName; // 用来确认一个进程是否持续在前台。
        Dictionary<string, ProcessInfo> InfoMap { get; }
        Timer TaskTimer;
        int TimerTick;

        public WADYProcessHelper()
        {
            InfoMap = new Dictionary<string, ProcessInfo>();
            LastProcessName = "";
            Error = false;

            TimerTick = 500;
        }

        #region 导入Win32函数
        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);
        #endregion
        //public static void Update()
        public string CurrentProcess()
        {
            IntPtr CurrentWindow = (IntPtr)0;
            uint CurrentWindowId = 0 ;
            Process CurrentWindowProcess;

            ProcessInfo CurInfo = null;
            ProcessInfo.TimeInfo thisTime;

            try
            {
                CurrentWindow = GetForegroundWindow();

                // 是否成功获得有效的句柄
                if (CurrentWindow.ToInt32() == 0)
                    return "";
                GetWindowThreadProcessId(CurrentWindow, ref CurrentWindowId);
                CurrentWindowProcess = Process.GetProcessById((int)CurrentWindowId);
                
                string Description="",Path="",Name;

                Name = CurrentWindowProcess.ProcessName;

                // 这个进程是否是 Universal(UWP) 应用
                if(Name == "ApplicationFrameHost")
                {
                    #region dog die
                    /*IntPtr tCurrentWindow = (IntPtr)0;
                    uint tCurrentWindowId = 0;
                    Process tCurrentWindowProcess;

                    tCurrentWindow = FindWindow(null, CurrentWindowProcess.MainWindowTitle);
                    GetWindowThreadProcessId(tCurrentWindow, ref tCurrentWindowId);
                    tCurrentWindowProcess = Process.GetProcessById((int)tCurrentWindowId);*/
                    // Dog die,并不能通过 FindWindow找到真正进程的句柄，但貌似可以用FileDescription来找

                    /*Process[] AllProcess = Process.GetProcesses();

                    foreach (var CurProc in  AllProcess)
                    {
                        try
                        {
                            if (CurProc.MainModule.FileVersionInfo.FileDescription == CurrentWindowProcess.MainWindowTitle)
                            {
                                Console.WriteLine(CurProc.MainWindowTitle);
                                int a = 5;
                            }

                        }
                        catch
                        {
                            continue;
                        }
                        
                    }*/
                    #endregion

                    Name = CurrentWindowProcess.MainWindowTitle+ " " + Name;
                }
                if (!this.InfoMap.ContainsKey(Name))
                {
                    // 并没有改名字的记录
                    try
                    { 
                        Description = CurrentWindowProcess.MainModule.FileVersionInfo.FileDescription;
                    }
                    catch
                    {
                        Description = CurrentWindowProcess.MainWindowTitle;
                    }
                    try
                    {
                        Path = CurrentWindowProcess.MainModule.FileName;
                    }
                    catch
                    {
                        Path = "\\";
                    }

                    // 创建信息结构
                    CurInfo = new ProcessInfo();
                    CurInfo.ProcessName = Name;
                    CurInfo.ProcessPath = Path;
                    CurInfo.ProcessDescription = Description;

                    // 创建时间信息
                    thisTime = new ProcessInfo.TimeInfo();
                    thisTime.Start = DateTime.Now;
                    thisTime.Last = TimeSpan.Zero;
                    CurInfo.ProcessTimeInfo.Insert(0, thisTime);

                    // 通过名字映射一下
                    this.InfoMap[Name] = CurInfo;
                }
                else
                {
                    // 从名字获取映射的数据
                    CurInfo = InfoMap[Name];
                    if (this.LastProcessName == Name)
                    {
                        // 说明在WADY sleep的时候，进程没有切换


                        // aaaaaaaaaaaaa
                        // bug fix 1.2 
                        // 终极修复，这个东西的原因是，ProcessTimeInfo这个东西是结构。。改成类后，BUG消失。

                        // error  fix 1.1  please look up fix 1.2
                        // 这里获得的，First，只是一个副本。对thistime的修改，不会影响First。
                        thisTime = CurInfo.ProcessTimeInfo.First();

                        TimeSpan tmpLast = thisTime.Last;
                        //TimeSpan tmpLast = new TimeSpan(thisTime.Last.Ticks);

                        // error bug fix 1.0    ..look up  fix1.1
                        // """为什么 thisTime.Last = DateTime.Now - thisTime.Start;
                        // 这样获得的TimeSpan，在函数退出后，会被GC掉？
                        // GC掉后，导致上面的tmpLast获取的上一个Last 一直为0 。。。奇葩的Bug，还是姿势太少。"""
                        //thisTime.Last = DateTime.Now - thisTime.Start;
                        thisTime.Last = new TimeSpan((DateTime.Now - thisTime.Start).Ticks);
                        //var test = thisTime.Last - tmpLast;
                        CurInfo.TotalTime = CurInfo.TotalTime + (thisTime.Last - tmpLast);

                        this.LastProcessName = Name;
                        //return thisTime.Last + "  " + tmpLast + "    " + thisTime.GetHashCode() + "   " + CurInfo.ProcessTimeInfo.First().GetHashCode();
                        return CurInfo.TotalTime + "      " + thisTime.Last + "        " + (thisTime.Last - tmpLast);
                    }
                    else
                    {
                        //InfoMap[LastProcessName].TotalTime += InfoMap[LastProcessName].ProcessTimeInfo.First().Last;

                        thisTime = new ProcessInfo.TimeInfo();
                        thisTime.Start = DateTime.Now;
                        thisTime.Last = TimeSpan.Zero;

                        CurInfo.ProcessTimeInfo.Insert(0, thisTime);

                    }
                }


                this.LastProcessName = Name;

                return CurInfo.ProcessPath + "   " + CurInfo.ProcessName + "\n" + CurInfo.ProcessDescription + "  " ;
            }
            catch(Exception e)
            {
                Console.WriteLine("Message {0}, Source {1}\n{2}", e.Message,e.Source,e.StackTrace);
                //Error = true;
            }
            return "";
        }

        

        public bool StartTask()
        {
            if (TaskTimer != null)
                return false;
            TaskTimer = new Timer(TaskExecute,null,0, TimerTick);

            return true;
        }
        void TaskExecute(object obj)
        {
            this.CurrentProcess();
        }




        //下面定义一些查询接口
        /// <summary>
        /// 获取在MapInfo中进程的个数
        /// </summary>
        /// <returns></returns>
        public int QueryInfoCount()
        {
            return InfoMap.Count;
        }
        /// <summary>
        /// 获取指定进程的 信息。
        /// </summary>
        /// <param name="processName">指定进程的名字</param>
        /// <returns></returns>
        public ProcessInfo QueryProcessInfo(string processName)
        {
            try
            { 
                return this.InfoMap[processName];
            }
            catch 
            {
                return null;
            }
        }
        /// <summary>
        /// 按总时间降序排列 Process
        /// </summary>
        /// <returns>返回一个列表，这个列表中的</returns>
        public List<ProcessInfo> QueryTotalTimeList()
        {
            // 预分配内存
            List<ProcessInfo> ret = new List<ProcessInfo>(QueryInfoCount());

            // 将所有的ProcessInfo 添加到list
            foreach( var item in InfoMap)
                ret.Add(item.Value);

            // 按照TimeSpan的大小来，排序
            ret.Sort(delegate (ProcessInfo left, ProcessInfo right)
            {
                if (left.TotalTime > right.TotalTime)
                    return -1;
                return 1;
            });

            return ret;
        }

        /*
        1.查询记录数目
        2.查询总时间的排序
        */
    }

}
