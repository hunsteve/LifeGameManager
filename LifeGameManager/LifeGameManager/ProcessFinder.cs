using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Diagnostics;

namespace LifeGameManager
{
    delegate void ProcessFoundEventHandler(object sender, uint processId);

    class ProcessFinder
    {
        HashSet<uint> procIds = new HashSet<uint>();
        ManagementEventWatcher watcher;
        string searchedProcessName;

        
        public event ProcessFoundEventHandler ProcessFound;


        public ProcessFinder(int parentProcessId, string searchedProcessName)
        {
            this.searchedProcessName = searchedProcessName;
            procIds.Add((uint)parentProcessId);
            WatchForProcessStart();
        }

        public void WatchForProcessStart()
        {            
            string queryString =
                "SELECT *" +
                "  FROM __InstanceCreationEvent " +
                "WITHIN  0.1 " +
                " WHERE TargetInstance ISA 'Win32_Process' ";            
            string scope = @"\\.\root\CIMV2";
            watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += ProcessStarted;
            watcher.Start();            
        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = (string)targetInstance.Properties["Name"].Value;
            uint processId = (uint)targetInstance.Properties["ProcessId"].Value;
            uint parentId = (uint)targetInstance.Properties["ParentProcessId"].Value;
            
            if (procIds.Contains(parentId))
            {
                Console.WriteLine("{0} ({1}) was started by {2}", processName, processId, parentId);
                procIds.Add(processId);
                if (searchedProcessName.Equals(processName))
                {
                    watcher.Stop();
                    if (ProcessFound != null)
                    {
                        ProcessFound.Invoke(this, processId);
                    }
                }
            }
        }
    }
}
