using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WADY.Core
{
    // 记录一段时间，起点 与 持续时间
    public class TimeInfo
    {
        public TimeInfo()
        {
            Start = DateTime.Now;
            Last = TimeSpan.Zero;
        }
        public DateTime Start; // 切换到的时间
        public TimeSpan Last;  // 这次的持续时间
    }

    public class ProcessInfo
    {
        public ProcessInfo()
        {
            ProcessTimeInfo = new List<TimeInfo>();
            TotalTime = TimeSpan.Zero;
            StartTime = DateTime.Now;
        }

        public ProcessInfo(Process process) : this()
        {
            ProcessName = process.ProcessName;
            try
            {
                ProcessDescription = process.MainModule.FileVersionInfo.FileDescription;
            }
            catch
            {
                ProcessDescription = process.MainWindowTitle;
            }
            try
            {
                ProcessPath = process.MainModule.FileName;
            }
            catch
            {
                ProcessPath = "\\";
            }
        }

        public string ProcessName { get; set; } // 进程名
        public string ProcessPath { get; set; }  // 进程的路径  有些进程，比如任务管理器，会拒绝这一请求
        public string ProcessDescription { get; set; }  // 进程的文件描述
        public DateTime StartTime { get; set; }         // 进程的开启时间

        public TimeSpan TotalTime { get; set; }
        public List<TimeInfo> ProcessTimeInfo { get; set; }

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


        public WADYProcessHelper()
        {
            InfoMap = new Dictionary<string, ProcessInfo>();
            OrderedProcessList = new List<ProcessInfo>();
            LastProcessName = "";

            TimerTick = 333;
            TickDelegate = new List<Delegate>();
        }

        #region 导入Win32函数
        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);
        #endregion
        //public static void Update()

        bool IsHaveProcessInfo(string name)
        {
            return InfoMap.ContainsKey(name);
        }
        bool IsLastProcess(string name)
        {
            return name == LastProcessName;
        }

        ProcessInfo GetCurrentProcessInfo()
        {
            IntPtr CurrentWindowHandle = (IntPtr)0;
            uint CurrentWindowId = 0;
            Process CurrentWindowProcess;
            ProcessInfo CurrentInfo;

            CurrentWindowHandle = GetForegroundWindow();
            // 是否成功获得有效的句柄
            if (CurrentWindowHandle.ToInt32() == 0)
                return null;

            GetWindowThreadProcessId(CurrentWindowHandle, ref CurrentWindowId);
            CurrentWindowProcess = Process.GetProcessById((int)CurrentWindowId);
            

            string Name;
            Name = CurrentWindowProcess.ProcessName;
            // 这个进程是否是 Universal(UWP) 应用
            if (Name == "ApplicationFrameHost")
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
                Name = CurrentWindowProcess.MainWindowTitle;
            }
            if (!IsHaveProcessInfo(Name))
            {
                // 并没有改名字的记录

                // 创建信息结构
                CurrentInfo = new ProcessInfo(CurrentWindowProcess);
                
                CurrentInfo.ProcessTimeInfo.Insert(0, new TimeInfo());

                // 通过名字映射一下
                InfoMap[Name] = CurrentInfo;
                OrderedProcessList.Add(CurrentInfo);
            }
            else
                CurrentInfo = InfoMap[Name];

            return CurrentInfo;
        }

        public void UpdateProcess()
        {
            ProcessInfo CurrentInfo;
            TimeInfo thisTime;

            // 从名字获取映射的数据
            CurrentInfo = GetCurrentProcessInfo();
            if (CurrentInfo == null)
                return;
            if (IsLastProcess(CurrentInfo.ProcessName) || LastProcessName == null)
            {
                // 说明在WADY sleep的时候，进程没有切换

                thisTime = CurrentInfo.ProcessTimeInfo.First();
                TimeSpan tmpLast = thisTime.Last;
                thisTime.Last = new TimeSpan((DateTime.Now - thisTime.Start).Ticks);
                CurrentInfo.TotalTime = CurrentInfo.TotalTime + (thisTime.Last - tmpLast);
            }
            else
            {
                LastProcessName = CurrentInfo.ProcessName;
                thisTime = new TimeInfo();
                CurrentInfo.ProcessTimeInfo.Insert(0, thisTime);
            }

            OrderedProcessList.Sort(delegate (ProcessInfo left, ProcessInfo right)
            {
                if (left.TotalTime > right.TotalTime)
                    return -1;
                return 1;
            });

        }

        public void AddDelgate(Action listViewUpdate)
        {
            if (!TickDelegate.Contains(listViewUpdate))
                TickDelegate.Add(listViewUpdate);

        }
        public bool StartTask()
        {
            if (TaskTimer != null)
                return false;

            TaskTimer = new Timer(delegate (object obj)
            {
                //if(obj is Timer)
                UpdateProcess();
                foreach (var d in TickDelegate)
                {
                    d.DynamicInvoke();
                }
            }, null, 0, TimerTick);

            return true;
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
                return InfoMap[processName];
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
            // 这里的话，有一个小概率事件，当ProcessInfo添加进 OrderedProcessList后，
            // OrderedProcessLsit完成排序之前，调用QueryTotalTimeList，就有可能得到一个
            // UnOrderedList,这个可能和List的排序速度有关吧。反正测试过程中我还没遇到过
            // 到后面可以考虑在这里加一个锁
            return OrderedProcessList;
        }
        public Dictionary<string, ProcessInfo> QueryTotalTimeMap()
        {
            return InfoMap;
        }

        string LastProcessName; // 用来确认一个进程是否持续在前台。

        Timer TaskTimer;
        int TimerTick;

        List<ProcessInfo> OrderedProcessList;
        List<Delegate> TickDelegate;
        Dictionary<string, ProcessInfo> InfoMap;
    }


}
