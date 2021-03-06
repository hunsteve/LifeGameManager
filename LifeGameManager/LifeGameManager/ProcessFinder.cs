﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Diagnostics;
using System.Threading;

namespace LifeGameManager
{    
    class ProcessFinder
    {        
        public static Process FindProcess(int parentProcessId, string searchedProcessName, int timeout)
        {
            Process pp = null;
            HashSet<int> seenProcesses = new HashSet<int>();
            DateTime start = DateTime.Now;
            while (pp == null && (DateTime.Now - start) < TimeSpan.FromMilliseconds(timeout))
            {
                Thread.Sleep(10);
                Process[] processlist = Process.GetProcesses();
                foreach (Process p in processlist)
                {
                    if (!seenProcesses.Contains(p.Id))
                    {
                        seenProcesses.Add(p.Id);
                        if (p.ProcessName == searchedProcessName && (p.ParentID() == parentProcessId || p.Id == parentProcessId))
                        {
                            pp = p;
                            break;
                        }
                    }
                }
            }

            return pp;
        }
    }

    public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if ((int)processId.NextValue() == pid)
                {
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int)parentId.NextValue());
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }

        public static int ParentID(this Process process)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", FindIndexedProcessName(process.Id));
            return (int)parentId.NextValue();
        }
    }
}
