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

namespace LifeGameManager
{
    public partial class LifeGameManagerForm : Form
    {

        int[] taskIDs = { 37, 38, 39 };
        const int StateNewSubmission = 0;
        const int StateUnderAutoProcessing = 3;
        const int StateProcessingFinished = 7;
        const int StateProcessingAborted = 9;

        const string SambaFileSharePath = "y:\\";
        const string ArchivePath = "c:\\LifeGame\\Archive\\";
        const string WorkingPath = "c:\\LifeGame\\Uploads\\";
        

        
        
        MySqlConnection conn;
        string[] messageInitials = { "***", "+++", "---" };
        const int checkInterval = 15000; //15 sec
        const int verboseLevel = 2;//0 - minimal, 1 - something,  2 - all
        const string appName = "LifeGameManager V1.0";


        enum LGMAppState { NotConnected, Idle, ProcessingOngoing } ;
        LGMAppState state;
        Dictionary<string, object> currentJob;

        private void AddLine(string s)
        {
            AddLine(s, 0);            
        }

        private void AddLine(string s, int verbose)
        {
            if (verboseLevel >= verbose)
            {
                textBoxConsole.Text += "\r\n" + messageInitials[verbose] + " " + s + " " + messageInitials[verbose];
            }                
        }


        public LifeGameManagerForm()
        {
            InitializeComponent();
            state = LGMAppState.NotConnected;
            string cs = "Server=hf.mit.bme.hu;Uid=vimia357;Pwd=vimia35753;Database=hf.mit.bme.hu";
            AddLine("CONNECTING: " + cs);
            try
            {
                conn = new MySqlConnection(cs);
                conn.Open();
            }
            catch(Exception ex) {
                AddLine("ERROR: " + ex.Message);
                return;
            }
            AddLine("CONNECTION SUCCESSFUL");
            state = LGMAppState.Idle;
            startTimer();
            DoMyJob();
        }

        private Dictionary<string, object> GetNextJob(int taskID)
        {
            Dictionary<string, object> ret = null;
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM `list_task_" + taskID + "` WHERE Allapot = " + StateNewSubmission + " ORDER BY BeadasDatuma ASC LIMIT 1";
                AddLine("sql: " + cmd.CommandText, 2);
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {                    
                    while (rdr.Read())
                    {
                        if (ret == null) ret = new Dictionary<string, object>();

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
            return ret;
        }

        private void UpdateProcedure(int taskID, int update_id, int update_state, string update_result, string update_comment, string update_signature, int update_format)
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CALL update_task_" + taskID + "(" + update_id + ", " + update_state + ", \"" + update_result + "\", \"" + update_comment + "\", \"" + update_signature + "\", " + update_format + ")";
                int retval = cmd.ExecuteNonQuery();                
            }
        }

        private void GetAllViews()
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT table_name FROM information_schema.views;";
            }
        }

        private void GetAllProcs()
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT specific_name FROM information_schema.routines;";
            }
        }
        
        private void LifeGameManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {            
            if (conn != null)
            {
                if (state == LGMAppState.ProcessingOngoing)
                {
                    //!!! TODO: abort processing
                    //UpdateProcedure(taskID, (uint)job["ID"], StateProcessingFinished, "1", "komment", appName, 1);            
                }


                AddLine("CLOSING CONNECTION");
                try
                {
                    conn.Close();
                    state = LGMAppState.NotConnected;
                }
                catch (Exception ex)
                {              
                    AddLine("ERROR: " + ex.Message);
                    return;
                }
                AddLine("CONNECTION CLOSED");
            }
        }

        private void startTimer()
        {
            timer1.Interval = checkInterval;
            timer1.Enabled = true;
            timer1.Start();
            AddLine("timer started", 2);
        }

        private void stopTimer()
        {
            timer1.Stop();
            AddLine("timer stopped", 2);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DoMyJob();
        }


        private void DoMyJob()
        {
            AddLine("checking database for new jobs", 2);
            foreach (int taskID in taskIDs)
            {
                Dictionary<string, object> job = GetNextJob(taskID);
                if (job != null)
                {
                    AddLine("got a " + taskID + " job, with ID: " + job["ID"], 1);

                    ProcessJob(job);
                }
                else
                {
                    AddLine("no job for " + taskID, 2);
                }
            }
        }

        private void ProcessJob(Dictionary<string, object> job)
        {
            stopTimer();
            state = LGMAppState.ProcessingOngoing;
            currentJob = job;
            uint taskID = (uint)job["FeladatID"];

            try
            {
                CopyToArchiveAndUnzipToWork(job);

                //UpdateProcedure(taskID, (uint)job["ID"], StateProcessingFinished, "1", "komment", appName, 1);            
            }
            catch (Exception ex)
            {
                AddLine("ERROR: " + ex.Message);
                //UpdateProcedure(taskID, (uint)job["ID"], StateProcessingFinished, "1", "komment", appName, 1);            
            }

           

            startTimer();
            state = LGMAppState.Idle;
            currentJob = null;
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
            int i=1;
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

       
    }
}

