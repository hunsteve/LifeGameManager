namespace LifeGameManager
{
    partial class LifeGameManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timerJobSchedule = new System.Windows.Forms.Timer(this.components);
            this.textBoxConsole = new System.Windows.Forms.TextBox();
            this.timerProcessTimeout = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // timerJobSchedule
            // 
            this.timerJobSchedule.Tick += new System.EventHandler(this.timerJobSchedule_Tick);
            // 
            // textBoxConsole
            // 
            this.textBoxConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxConsole.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxConsole.Location = new System.Drawing.Point(0, 0);
            this.textBoxConsole.MaxLength = 10000000;
            this.textBoxConsole.Multiline = true;
            this.textBoxConsole.Name = "textBoxConsole";
            this.textBoxConsole.ReadOnly = true;
            this.textBoxConsole.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxConsole.Size = new System.Drawing.Size(983, 307);
            this.textBoxConsole.TabIndex = 1;
            // 
            // timerProcessTimeout
            // 
            this.timerProcessTimeout.Tick += new System.EventHandler(this.timerProcessTimeout_Tick);
            // 
            // LifeGameManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(983, 307);
            this.Controls.Add(this.textBoxConsole);
            this.Name = "LifeGameManagerForm";
            this.Text = "LifeGame Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LifeGameManagerForm_FormClosing);
            this.Load += new System.EventHandler(this.LifeGameManagerForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer timerJobSchedule;
        private System.Windows.Forms.TextBox textBoxConsole;
        private System.Windows.Forms.Timer timerProcessTimeout;

    }
}

