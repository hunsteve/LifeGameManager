using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient; 

namespace LifeGameManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateProcedure(MySqlConnection conn, int taskID, int update_id, int update_state, string update_result, string update_comment, string update_signature, int update_format)
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "CALL update_task_" + taskID + "(" + update_id + ", " + update_state + ", \"" + update_result + "\", \"" + update_comment + "\", \"" + update_signature + "\", " + update_format + ")";
                int retval = cmd.ExecuteNonQuery();                
            }
        }

        private void GetAllViews(MySqlConnection conn)
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT table_name FROM information_schema.views;";
            }
        }

        private void GetAllProcs(MySqlConnection conn)
        {
            using (MySqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT specific_name FROM information_schema.routines;";
            }
        }


        

        private void button1_Click(object sender, EventArgs e)
        {            
            string cs = "Server=hf.mit.bme.hu;Uid=vimia357;Pwd=vimia35753;Database=hf.mit.bme.hu";

            using (MySqlConnection conn = new MySqlConnection(cs))
            {

                conn.Open();                

                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText="select * from `list_task_37`";                    
                    using (MySqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            for (int i = 0; i < rdr.FieldCount; ++i)
                            {
                                Console.Write(rdr.GetName(i) + " = " + rdr.GetValue(i) + "; ");
                            }
                            Console.WriteLine();
                        }
                    }
                }

                //-----------------------------------------------


                UpdateProcedure(conn, 37, 5192, 3, "1", "komment", "LifeGameManager V1.0", 1);

            }
        }
    }
}
