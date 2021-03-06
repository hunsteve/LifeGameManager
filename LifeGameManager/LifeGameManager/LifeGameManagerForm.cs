﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;
using System.Globalization;
using Ionic.Zip;
using System.Diagnostics;
using System.Threading;

namespace LifeGameManager
{
    public partial class LifeGameManagerForm : Form
    {
        //default values
        int[] taskIDs = { 37, 38, 39 ,46 };
        int StateNewSubmission = 0;
        int StateUnderAutoProcessing = 3;
        int StateProcessingFinished = 7;
        int StateProcessingAborted = 9;

        string SambaFileSharePath = "z:\\";
        string ArchivePath = "c:\\LifeGame\\Archive\\";
        string WorkingPath = "c:\\LifeGame\\Uploads\\";

        string ResultFilename = "result.txt";
        string OutputFilename = "matlab_output.txt";

        string MatlabStartPath = "\"c:\\Program Files\\MATLAB\\R2013a\\bin\\matlab.exe\"";
        string MatlabArguments = "-automation";
        string verificationScriptName = "verify";
        string verificationScriptsZip = "Uploads.zip";

        int timeoutInterval = 180000; //180 sec

        int checkInterval = 60000; //60 sec
        int verboseLevel = 2;//0 - minimal, 1 - something,  2 - all
        int fileVerboseLevel = 2;//0 - minimal, 1 - something,  2 - all
        bool debugModeEnabled = true;
        string LogFilename = "log.txt";

        string mySqlServer = "hf.mit.bme.hu";
        string mySqlUser = "vimia357";
        string mySqlPassword = "vimia35753";
        string mySqlDatabase = "hf.mit.bme.hu";

        //built in constants        
        string[] messageInitials = { "***", "+++", "---" };
        const string appName = "LifeGameManager V1.04";
        const string iniFile = "lifegamemanager.ini";

        const string iniConnection = "mysql_connection";
        const string iniLifeGame = "lifegame";
        const string iniHWServer = "hw_server";
        const string iniMaintenance = "maintenance";


        //variables
        enum LGMAppState { Idle, ProcessingOngoing } ;
        LGMAppState state;
        Dictionary<string, object> currentJob;
        Process currentMaltabProcess;
        int taskIDoffset = 0;
        bool timeouted;

        private void AddLine(string s)
        {
            AddLine(s, 0);
        }

        private void AddLine(string s, int verbose)
        {
            string date = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss ");
            string line = "\r\n" + date + messageInitials[verbose] + " " + s + " " + messageInitials[verbose];
            if (verboseLevel >= verbose)
            {
                textBoxConsole.Text += line;
                textBoxConsole.SelectionStart = textBoxConsole.TextLength;
                textBoxConsole.ScrollToCaret();
            }
            if (fileVerboseLevel >= verbose)
            {
                File.AppendAllText(LogFilename, line);
            }
        }


        public LifeGameManagerForm()
        {
            InitializeComponent();
        }

        private void LifeGameManagerForm_Load(object sender, EventArgs e)
        {
            ReadSettings();            

            state = LGMAppState.Idle;
            ConnectToDatabase();
            FixUnfinishedJobs();            
            startTimer();
        }

        private MySqlConnection ConnectToDatabase()
        {
            MySqlConnection conn;
            string cs = "Server=" + mySqlServer + ";Uid=" + mySqlUser + ";Pwd=" + mySqlPassword + ";Database=" + mySqlDatabase;
            //AddLine("CONNECTING: " + cs, 2);
            try
            {
                conn = new MySqlConnection(cs);
                conn.Open();
            }
            catch (Exception ex)
            {
                AddLine("ERROR: " + ex.Message + "\r\n" + ex.StackTrace);
                return null;
            }
            //AddLine("CONNECTION SUCCESSFUL", 2);
            state = LGMAppState.Idle;
            return conn;
        }
        
        private void ReadSettings()
        {
            string inifile = Environment.CurrentDirectory + "\\" + iniFile;
            IniFile ini = new IniFile(inifile);
            if (File.Exists(inifile))
            {
                mySqlServer = ini.IniReadValue(iniConnection, "mySqlServer");
                mySqlUser = ini.IniReadValue(iniConnection, "mySqlUser");
                mySqlPassword = ini.IniReadValue(iniConnection, "mySqlPassword");
                mySqlDatabase = ini.IniReadValue(iniConnection, "mySqlDatabase");

                ArchivePath = ini.IniReadValue(iniLifeGame, "ArchivePath");
                WorkingPath = ini.IniReadValue(iniLifeGame, "WorkingPath");
                ResultFilename = ini.IniReadValue(iniLifeGame, "ResultFilename");
                OutputFilename = ini.IniReadValue(iniLifeGame, "OutputFilename");
                try { timeoutInterval = int.Parse(ini.IniReadValue(iniLifeGame, "timeoutInterval")); }
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                MatlabStartPath = ini.IniReadValue(iniLifeGame, "MatlabStartPath");
                MatlabArguments = ini.IniReadValue(iniLifeGame, "MatlabArguments");
                verificationScriptName = ini.IniReadValue(iniLifeGame, "verificationScriptName");
                verificationScriptsZip = ini.IniReadValue(iniLifeGame, "verificationScriptsZip");
                
                try { taskIDs = Array.ConvertAll(ini.IniReadValue(iniHWServer, "taskIDs").Split(new char[]{','}), s => int.Parse(s.Trim())); }
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }                
                try { StateNewSubmission = int.Parse(ini.IniReadValue(iniHWServer, "StateNewSubmission"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                try { StateUnderAutoProcessing = int.Parse(ini.IniReadValue(iniHWServer, "StateUnderAutoProcessing"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                try { StateProcessingFinished = int.Parse(ini.IniReadValue(iniHWServer, "StateProcessingFinished"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                try { StateProcessingAborted = int.Parse(ini.IniReadValue(iniHWServer, "StateProcessingAborted"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                SambaFileSharePath = ini.IniReadValue(iniHWServer, "SambaFileSharePath");
                try { checkInterval = int.Parse(ini.IniReadValue(iniHWServer, "checkInterval"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }

                try { verboseLevel = int.Parse(ini.IniReadValue(iniMaintenance, "verboseLevel"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                try { fileVerboseLevel = int.Parse(ini.IniReadValue(iniMaintenance, "fileVerboseLevel")); }
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                try { debugModeEnabled = bool.Parse(ini.IniReadValue(iniMaintenance, "debugModeEnabled"));}
                catch (Exception ex) { AddLine("Error parsing INI file: " + ex.Message + "\r\n" + ex.StackTrace); }
                LogFilename = ini.IniReadValue(iniMaintenance, "LogFilename");
            }
            else
            {
                ini.IniWriteValue(iniConnection, "mySqlServer",mySqlServer);
                ini.IniWriteValue(iniConnection, "mySqlUser",mySqlUser);
                ini.IniWriteValue(iniConnection, "mySqlPassword",mySqlPassword);
                ini.IniWriteValue(iniConnection, "mySqlDatabase",mySqlDatabase);
                                 
                ini.IniWriteValue(iniLifeGame, "ArchivePath",ArchivePath);
                ini.IniWriteValue(iniLifeGame, "WorkingPath",WorkingPath);
                ini.IniWriteValue(iniLifeGame, "ResultFilename",ResultFilename);
                ini.IniWriteValue(iniLifeGame, "OutputFilename",OutputFilename);
                ini.IniWriteValue(iniLifeGame, "timeoutInterval",timeoutInterval.ToString());
                ini.IniWriteValue(iniLifeGame, "MatlabStartPath", MatlabStartPath);
                ini.IniWriteValue(iniLifeGame, "MatlabArguments", MatlabArguments);
                ini.IniWriteValue(iniLifeGame, "verificationScriptName", verificationScriptName);
                ini.IniWriteValue(iniLifeGame, "verificationScriptsZip", verificationScriptsZip);
                                                
                ini.IniWriteValue(iniHWServer, "taskIDs",taskIDs.Skip(1).Aggregate(taskIDs[0].ToString(), (s, i) => s + "," + i.ToString()));
                ini.IniWriteValue(iniHWServer, "StateNewSubmission",StateNewSubmission.ToString());                
                ini.IniWriteValue(iniHWServer, "StateUnderAutoProcessing",StateUnderAutoProcessing.ToString());                
                ini.IniWriteValue(iniHWServer, "StateProcessingFinished",StateProcessingFinished.ToString());                
                ini.IniWriteValue(iniHWServer, "StateProcessingAborted",StateProcessingAborted.ToString());
                ini.IniWriteValue(iniHWServer, "SambaFileSharePath",SambaFileSharePath);
                ini.IniWriteValue(iniHWServer, "checkInterval",checkInterval.ToString());                

                ini.IniWriteValue(iniMaintenance, "verboseLevel",verboseLevel.ToString());
                ini.IniWriteValue(iniMaintenance, "fileVerboseLevel", fileVerboseLevel.ToString()); 
                ini.IniWriteValue(iniMaintenance, "debugModeEnabled",debugModeEnabled.ToString());                
                ini.IniWriteValue(iniMaintenance, "LogFilename",LogFilename);
            }            
        }

        private void FixUnfinishedJobs()
        {
            AddLine("fixing unfinished jobs", 2);
            try
            {
                for (int i = 0; i < taskIDs.Length; ++i)
                {
                    int taskID = taskIDs[i];

                    List<Dictionary<string, object>> jobs = new List<Dictionary<string, object>>();
                    MySqlConnection conn = ConnectToDatabase();
                    using (MySqlCommand cmd = conn.CreateCommand())
                    {                                 
                        cmd.CommandText = "SELECT * FROM `list_task_" + taskID + "` WHERE Allapot = " + StateUnderAutoProcessing;
                        AddLine("sql: " + cmd.CommandText, 2);
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                Dictionary<string, object> ret = new Dictionary<string, object>();

                                string s = "";
                                for (int j = 0; j < rdr.FieldCount; ++j)
                                {
                                    s += rdr.GetName(j) + " = " + rdr.GetValue(j) + "; ";
                                    ret.Add(rdr.GetName(j), rdr.GetValue(j));
                                }
                                AddLine("received unfinished job: " + s, 2);
                                jobs.Add(ret);
                            }
                        }
                    }
                    foreach (Dictionary<string, object> job in jobs)
                    {
                        UpdateProcedure((uint)job["FeladatID"], (uint)job["ID"], StateNewSubmission, "0", "Félbehagyott javítás újrakezdése", appName, 1);
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                AddLine("Error during FixUnfinishedJobs: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private Dictionary<string, object> GetNextJob(int taskID)
        {
            Dictionary<string, object> ret = null;
            try
            {
                MySqlConnection conn = ConnectToDatabase();
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    string debugCriterion = "";
                    if (debugModeEnabled) debugCriterion = " AND Neptun LIKE 'TEST%'";
                    cmd.CommandText = "SELECT * FROM `list_task_" + taskID + "` WHERE Allapot = " + StateNewSubmission + debugCriterion + " ORDER BY BeadasDatuma ASC LIMIT 1";
                    AddLine("sql: " + cmd.CommandText, 2);
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        ret = new Dictionary<string, object>();
                        while (rdr.Read())
                        {
                            string s = "";
                            for (int i = 0; i < rdr.FieldCount; ++i)
                            {
                                s += rdr.GetName(i) + " = " + rdr.GetValue(i) + "; ";
                                ret.Add(rdr.GetName(i), rdr.GetValue(i));
                            }
                            AddLine("received: " + s, 2);
                        }
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                ret = null;
                AddLine("Error during GetNextJob: " + ex.Message + "\r\n" + ex.StackTrace);
            }
            return ret;
        }

        private string SanitizeSQLString(string value)
        {
            return value.Replace(@"\", @"\\").Replace("'", @"\'").Replace("\"", "\\\"");
        }

        private void UpdateProcedure(uint taskID, uint update_id, int update_state, string update_result, string update_comment, string update_signature, int update_format)
        {
            AddLine("updating a " + taskID + " job, with ID: " + update_id + " to state: " + update_state + " with result: " + SanitizeSQLString(update_result) + " and comment: " + SanitizeSQLString(update_comment), 1);
            try
            {
                MySqlConnection conn = ConnectToDatabase();
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CALL update_task_" + taskID + "(" + update_id + ", " + update_state + ", \"" + SanitizeSQLString(update_result) + "\", \"" + SanitizeSQLString(update_comment) + "\", \"" + update_signature + "\", " + update_format + ")";
                    int retval = cmd.ExecuteNonQuery();
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                AddLine("Error during UpdateProcedure: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void GetAllViews()
        {
            MySqlConnection conn = ConnectToDatabase();
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT table_name FROM information_schema.views;";
            }
            conn.Close();
        }

        private void GetAllProcs()
        {
            MySqlConnection conn = ConnectToDatabase();
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT specific_name FROM information_schema.routines;";
            }
            conn.Close();
        }

        private void LifeGameManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (state == LGMAppState.ProcessingOngoing)
            {
                UpdateProcedure((uint)currentJob["FeladatID"], (uint)currentJob["ID"], StateProcessingAborted, "0", "LifeGameManager application terminated", appName, 1);
                if (currentMaltabProcess != null)
                {
                    currentMaltabProcess.Kill();
                }
            }
        }

        private void startTimer()
        {
            timerJobSchedule.Interval = checkInterval;
            timerJobSchedule.Start();
            AddLine("job schedule timer started", 2);

            DoMyJob();
        }

        private void stopTimer()
        {
            timerJobSchedule.Stop();
            AddLine("job schedule timer stopped", 2);
        }

        private void timerJobSchedule_Tick(object sender, EventArgs e)
        {
            DoMyJob();
        }


        private void DoMyJob()
        {            
            AddLine("checking database for new jobs", 2);

            int errorCount = 0;

            for (int i = 0; i < taskIDs.Length; ++i)
            {                
                int taskID = taskIDs[(i + taskIDoffset) % taskIDs.Length];
                Dictionary<string, object> job = GetNextJob(taskID);                
                if (job == null)
                {
                    AddLine("Error during job acquisition. Retrying...");
                    i--;
                    errorCount++;
                    if (errorCount > 5)
                    {
                        AddLine("Too much retrying, lets wait a bit.");
                        return;
                    }
                    continue;
                }
                errorCount = 0;

                if (job.Count > 0)
                {
                    if (debugModeEnabled && !job["Neptun"].ToString().StartsWith("TEST"))
                    {
                        AddLine("ignoring " + taskID + " job, with ID: " + job["ID"] + " and neptun: " + job["Neptun"] + " because not TEST user", 1);
                        return;
                    }

                    AddLine("got a " + taskID + " job, with ID: " + job["ID"] + " and neptun: " + job["Neptun"], 1);

                    ProcessJob(job);
                    return;
                }
                else
                {
                    AddLine("no job for " + taskID, 2);
                }
            }
            taskIDoffset = (taskIDoffset + 1) % taskIDs.Length;
        }

        private void ProcessJob(Dictionary<string, object> job)
        {
            stopTimer();
            state = LGMAppState.ProcessingOngoing;
            currentJob = job;

            try
            {
                CopyToArchiveAndUnzipToWork(job);

                AddLine("processing job " + (uint)job["ID"] + " id: " + (uint)job["FeladatID"] + " neptun: " + job["Neptun"], 2);
                UpdateProcedure((uint)job["FeladatID"], (uint)job["ID"], StateUnderAutoProcessing, "0", "Feldolgozás alatt...", appName, 1);

                StartMATLABProcess(job);
            }
            catch (Exception ex)
            {
                AddLine("ERROR: " + ex.Message + "\r\n" + ex.StackTrace);
                UpdateProcedure((uint)job["FeladatID"], (uint)job["ID"], StateProcessingAborted, "0", ex.Message, appName, 1);

                AddLine("aborted job " + (uint)job["ID"] + " id: " + (uint)job["FeladatID"] + " neptun: " + job["Neptun"], 2);                
                state = LGMAppState.Idle;
                currentJob = null;
                stopProcessTimeoutTimer();
                startTimer();
            }
        }

        private void FinishJob(Dictionary<string, object> job)
        {
            try
            {
                if (!timeouted)
                {
                    AddLine("reading results for job " + (uint)job["ID"] + " id: " + (uint)job["FeladatID"] + " neptun: " + job["Neptun"], 2);
                    string result;
                    string resultText;
                    ReadMATLABResults(job, out result, out resultText);
                    if (resultText.Length > 250) {
                        resultText = "% túl hosszú output eleje csonkolva % \r\n" + resultText.Substring(resultText.Length - 250);
                    }
                    UpdateProcedure((uint)job["FeladatID"], (uint)job["ID"], StateProcessingFinished, result, resultText, appName, 1);
                    AddLine("finished job " + (uint)job["ID"] + " id: " + (uint)job["FeladatID"] + " neptun: " + job["Neptun"], 2);
                }
                else
                {
                    UpdateProcedure((uint)job["FeladatID"], (uint)job["ID"], StateProcessingFinished, "0", "Időtúllépés (60 s) miatt a Matlab bezárásra került!", appName, 1);
                    AddLine("aborted job due to timeout " + (uint)job["ID"] + " id: " + (uint)job["FeladatID"] + " neptun: " + job["Neptun"], 2);
                }
            }
            catch (Exception ex)
            {
                AddLine("ERROR: " + ex.Message + "\r\n" + ex.StackTrace);
                UpdateProcedure((uint)job["FeladatID"], (uint)job["ID"], StateProcessingAborted, "0", ex.Message, appName, 1);

                AddLine("aborted job " + (uint)job["ID"] + " id: " + (uint)job["FeladatID"] + " neptun: " + job["Neptun"], 2);
            }
            
            state = LGMAppState.Idle;
            currentJob = null;
            startTimer();
        }

        private void ReadMATLABResults(Dictionary<string, object> job, out string result, out string resultText)
        {
            result = "0";
            resultText = "";

            string workingDir = WorkingPath + (uint)job["FeladatID"] + "\\" + job["Neptun"] + "\\";
            string resultFile = workingDir + ResultFilename;
            string outputFile = workingDir + OutputFilename;

            try
            {
                AddLine("reading file: " + resultFile, 2);
                result = File.ReadAllText(resultFile);
            }
            catch (Exception ex)
            {
                AddLine("ERROR: " + ex.Message + "\r\n" + ex.StackTrace);
            }

            try
            {
                AddLine("reading file: " + outputFile, 2);
                resultText = File.ReadAllText(outputFile);
            }
            catch (Exception ex)
            {
                AddLine("ERROR: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void CopyToArchiveAndUnzipToWork(Dictionary<string, object> job)
        {
            string filename = SambaFileSharePath + (uint)job["FeladatID"] + "\\" + job["Neptun"] + ".zip";
            string todir = ArchivePath + (uint)job["FeladatID"] + "\\" + job["Neptun"] + "\\";
            if (!Directory.Exists(todir))
            {
                AddLine("creating directory: " + todir, 2);
                Directory.CreateDirectory(todir);
            }

            DateTime dt = (DateTime)job["BeadasDatuma"];
            string destFilenameBase = todir + job["Neptun"] + "_" + dt.ToString("yyyyMMddHHmmss");
            string destFilename = destFilenameBase + ".zip";
            int i = 1;
            while (File.Exists(destFilename))
            {
                destFilename = destFilenameBase + "_" + i + ".zip";
                ++i;
            }
            AddLine("copying file: " + filename + " to: " + destFilename, 2);
            File.Copy(filename, destFilename);

            string workingDir = WorkingPath + (uint)job["FeladatID"] + "\\" + job["Neptun"] + "\\";

            if (Directory.Exists(workingDir))
            {
                AddLine("deleting directory: " + workingDir, 2);
                Directory.Delete(workingDir, true);
            }
            AddLine("creating directory: " + workingDir, 2);
            Directory.CreateDirectory(workingDir);

            AddLine("unzipping file: " + destFilename + " to: " + workingDir, 2);
            using (ZipFile zip = ZipFile.Read(destFilename))
            {
                zip.ExtractAll(workingDir);
            }
        }

        private void StartMATLABProcess(Dictionary<string, object> job)
        {
            string verifilename = WorkingPath + job["FeladatID"] + "\\" + verificationScriptName + ".m";
            if (!File.Exists(verifilename))
            {
                AddLine("Not found: " + verifilename + ", unzipping verification scripts:" + verificationScriptsZip, 2);
                using (ZipFile zip = ZipFile.Read(verificationScriptsZip))
                {
                    zip.ExtractAll(WorkingPath);
                }
            }

            Process proc = Process.Start(MatlabStartPath, MatlabArguments + " -r cd('" + WorkingPath + job["FeladatID"] + "');" + verificationScriptName + "('" + job["Neptun"] + "')");
            AddLine("starting MATLAB, spawned process id:" + proc.Id, 2);

            currentMaltabProcess = ProcessFinder.FindProcess(proc.Id, "MATLAB", 10000);
            if (currentMaltabProcess != null)
            {
                currentMaltabProcess.EnableRaisingEvents = true;
                currentMaltabProcess.Exited += new EventHandler(process_Exited);                
                AddLine("found MATLAB, process id:" + currentMaltabProcess.Id, 2);
            }
            startProcessTimeoutTimer();
        }
      
        void process_Exited(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Process p = (Process)sender;
                AddLine("closed MATLAB, process id:" + p.Id, 2);
                if (currentMaltabProcess != null && p.Id == currentMaltabProcess.Id)
                {
                    stopProcessTimeoutTimer();
                }
                Thread.Sleep(3000);
            });
        }

        private void timerProcessTimeout_Tick(object sender, EventArgs e)
        {
            if (currentMaltabProcess != null)
            {
                timeouted = true;
                currentMaltabProcess.Kill();
                Thread.Sleep(100);                
                AddLine("MATLAB timeouted, closing, process id:" + currentMaltabProcess.Id, 2);
            }
            else
            {
                AddLine("MATLAB timeouted, process not found", 2);
            }
            stopProcessTimeoutTimer();
        }

        private void startProcessTimeoutTimer()
        {
            timerProcessTimeout.Interval = timeoutInterval;
            timerProcessTimeout.Start();
            timeouted = false;
            AddLine("process timeout timer started", 2);
        }

        private void stopProcessTimeoutTimer()
        {
            if (timerProcessTimeout.Enabled)
            {
                timerProcessTimeout.Stop();
                currentMaltabProcess = null;
                AddLine("process timeout stopped", 2);

                if (currentJob != null)
                {
                    FinishJob(currentJob);
                }
            }
        }


    }
}



