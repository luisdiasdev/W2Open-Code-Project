namespace W2Open.Server
{
    partial class MainForm
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
            this.rtbMainLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // rtbMainLog
            // 
            this.rtbMainLog.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.rtbMainLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbMainLog.Location = new System.Drawing.Point(0, 0);
            this.rtbMainLog.Name = "rtbMainLog";
            this.rtbMainLog.ReadOnly = true;
            this.rtbMainLog.Size = new System.Drawing.Size(498, 395);
            this.rtbMainLog.TabIndex = 0;
            this.rtbMainLog.Text = "";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(498, 395);
            this.Controls.Add(this.rtbMainLog);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbMainLog;
    }
}