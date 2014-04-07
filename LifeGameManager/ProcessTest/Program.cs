using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace ProcessTest
{
    class Program
    {
        static void Main(string[] args)
        {

            Process proc = Process.Start("\"c:\\Program Files\\MATLAB\\R2013a\\bin\\matlab.exe\"");         
            int id = proc.Id;

            Process pp = null;
            HashSet<int> seenProcesses = new HashSet<int>();
            while (pp == null) {
                Thread.Sleep(10);
                Process[] processlist = Process.GetProcesses();   
                foreach(Process p in processlist) {
                    if (!seenProcesses.Contains(p.Id))
                    {
                        seenProcesses.Add(p.Id);
                        if (p.ProcessName == "MATLAB" && (p.ParentID() == id || p.Id == id))
                        {
                            pp = p;
                            break;
                        }                
                    }                    
                }                
            }
         
           
        }
    }
}
