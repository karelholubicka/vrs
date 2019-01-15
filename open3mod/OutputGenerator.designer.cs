namespace open3mod
{
    partial class OutputGenerator
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
            this.comboBoxOutputDevice = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.buttonStartStop = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBoxVideoFormat = new System.Windows.Forms.ComboBox();
            this.restartLabel = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxOutputDevice
            // 
            this.comboBoxOutputDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOutputDevice.FormattingEnabled = true;
            this.comboBoxOutputDevice.Location = new System.Drawing.Point(93, 22);
            this.comboBoxOutputDevice.Name = "comboBoxOutputDevice";
            this.comboBoxOutputDevice.Size = new System.Drawing.Size(187, 21);
            this.comboBoxOutputDevice.TabIndex = 8;
            this.comboBoxOutputDevice.SelectedIndexChanged += new System.EventHandler(this.comboBoxOutputDevice_SelectedValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 25);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(76, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Output Device";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // buttonStartStop
            // 
            this.buttonStartStop.Location = new System.Drawing.Point(14, 53);
            this.buttonStartStop.Name = "buttonStartStop";
            this.buttonStartStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStartStop.TabIndex = 1;
            this.buttonStartStop.Text = "Start";
            this.buttonStartStop.UseVisualStyleBackColor = true;
            this.buttonStartStop.Click += new System.EventHandler(this.buttonStartStop_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.restartLabel);
            this.groupBox1.Controls.Add(this.comboBoxVideoFormat);
            this.groupBox1.Controls.Add(this.buttonStartStop);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.comboBoxOutputDevice);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(297, 99);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Properties";
            // 
            // comboBoxVideoFormat
            // 
            this.comboBoxVideoFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVideoFormat.FormattingEnabled = true;
            this.comboBoxVideoFormat.Location = new System.Drawing.Point(124, 54);
            this.comboBoxVideoFormat.Name = "comboBoxVideoFormat";
            this.comboBoxVideoFormat.Size = new System.Drawing.Size(87, 21);
            this.comboBoxVideoFormat.TabIndex = 11;
            this.comboBoxVideoFormat.SelectedIndexChanged += new System.EventHandler(this.comboBoxVideoFormat_SelectedIndexChanged);
            // 
            // restartLabel
            // 
            this.restartLabel.AutoSize = true;
            this.restartLabel.Location = new System.Drawing.Point(105, 83);
            this.restartLabel.Name = "restartLabel";
            this.restartLabel.Size = new System.Drawing.Size(0, 13);
            this.restartLabel.TabIndex = 12;
            this.restartLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // OutputGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(321, 118);
            this.Controls.Add(this.groupBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "OutputGenerator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "BMD Output";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OutputGenerator_FormClosing);
            this.Load += new System.EventHandler(this.SignalGenerator_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxOutputDevice;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button buttonStartStop;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox comboBoxVideoFormat;
        private System.Windows.Forms.Label restartLabel;
    }
}