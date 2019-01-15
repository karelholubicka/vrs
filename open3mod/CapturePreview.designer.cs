namespace open3mod
{
    partial class CapturePreview
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.labelTimecode = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelPixelFormat = new System.Windows.Forms.Label();
            this.buttonStartStop = new System.Windows.Forms.Button();
            this.checkBoxAutodetectFormat = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxVideoFormat = new System.Windows.Forms.ComboBox();
            this.comboBoxInputDevice = new System.Windows.Forms.ComboBox();
            this.labelInvalidInput = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.glWindow = new open3mod.GLWindow();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.labelTimecode);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.labelPixelFormat);
            this.groupBox1.Controls.Add(this.buttonStartStop);
            this.groupBox1.Controls.Add(this.checkBoxAutodetectFormat);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.comboBoxVideoFormat);
            this.groupBox1.Controls.Add(this.comboBoxInputDevice);
            this.groupBox1.Controls.Add(this.labelInvalidInput);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(268, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(374, 161);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Capture Properties";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(83, 115);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Timecode";
            // 
            // labelTimecode
            // 
            this.labelTimecode.AutoSize = true;
            this.labelTimecode.Location = new System.Drawing.Point(150, 115);
            this.labelTimecode.Name = "labelTimecode";
            this.labelTimecode.Size = new System.Drawing.Size(40, 13);
            this.labelTimecode.TabIndex = 10;
            this.labelTimecode.Text = "--:--:--:--";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(83, 97);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Pixel format";
            // 
            // labelPixelFormat
            // 
            this.labelPixelFormat.AutoSize = true;
            this.labelPixelFormat.Location = new System.Drawing.Point(150, 97);
            this.labelPixelFormat.Name = "labelPixelFormat";
            this.labelPixelFormat.Size = new System.Drawing.Size(69, 13);
            this.labelPixelFormat.TabIndex = 8;
            this.labelPixelFormat.Text = "Not detected";
            // 
            // buttonStartStop
            // 
            this.buttonStartStop.Location = new System.Drawing.Point(266, 129);
            this.buttonStartStop.Name = "buttonStartStop";
            this.buttonStartStop.Size = new System.Drawing.Size(95, 23);
            this.buttonStartStop.TabIndex = 7;
            this.buttonStartStop.Text = "Start Capture";
            this.buttonStartStop.UseVisualStyleBackColor = true;
            this.buttonStartStop.Click += new System.EventHandler(this.buttonStartStop_Click);
            // 
            // checkBoxAutodetectFormat
            // 
            this.checkBoxAutodetectFormat.AutoSize = true;
            this.checkBoxAutodetectFormat.Location = new System.Drawing.Point(153, 44);
            this.checkBoxAutodetectFormat.Name = "checkBoxAutodetectFormat";
            this.checkBoxAutodetectFormat.Size = new System.Drawing.Size(15, 14);
            this.checkBoxAutodetectFormat.TabIndex = 6;
            this.checkBoxAutodetectFormat.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(136, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Apply detected input format";
            // 
            // comboBoxVideoFormat
            // 
            this.comboBoxVideoFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVideoFormat.FormattingEnabled = true;
            this.comboBoxVideoFormat.Location = new System.Drawing.Point(153, 64);
            this.comboBoxVideoFormat.Name = "comboBoxVideoFormat";
            this.comboBoxVideoFormat.Size = new System.Drawing.Size(212, 21);
            this.comboBoxVideoFormat.TabIndex = 5;
            // 
            // comboBoxInputDevice
            // 
            this.comboBoxInputDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInputDevice.FormattingEnabled = true;
            this.comboBoxInputDevice.Location = new System.Drawing.Point(153, 17);
            this.comboBoxInputDevice.Name = "comboBoxInputDevice";
            this.comboBoxInputDevice.Size = new System.Drawing.Size(212, 21);
            this.comboBoxInputDevice.TabIndex = 4;
            this.comboBoxInputDevice.SelectedValueChanged += new System.EventHandler(this.comboBoxInputDevice_SelectedValueChanged);
            // 
            // labelInvalidInput
            // 
            this.labelInvalidInput.AutoSize = true;
            this.labelInvalidInput.Location = new System.Drawing.Point(42, 138);
            this.labelInvalidInput.Name = "labelInvalidInput";
            this.labelInvalidInput.Size = new System.Drawing.Size(102, 13);
            this.labelInvalidInput.TabIndex = 3;
            this.labelInvalidInput.Text = "No valid input signal";
            this.labelInvalidInput.Visible = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(78, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Video format";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(78, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Input device";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.glWindow);
            this.groupBox2.Location = new System.Drawing.Point(9, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(253, 161);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Preview";
            // 
            // glWindow
            // 
            this.glWindow.BackColor = System.Drawing.Color.Black;
            this.glWindow.Location = new System.Drawing.Point(6, 17);
            this.glWindow.Name = "glWindow";
            this.glWindow.Size = new System.Drawing.Size(240, 135);
            this.glWindow.TabIndex = 0;
            this.glWindow.VSync = false;
            // 
            // CapturePreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(654, 182);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "CapturePreview";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Camera #";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CapturePreview_FormClosing);
            this.Load += new System.EventHandler(this.CapturePreview_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonStartStop;
        private System.Windows.Forms.CheckBox checkBoxAutodetectFormat;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxVideoFormat;
        private System.Windows.Forms.ComboBox comboBoxInputDevice;
        private System.Windows.Forms.Label labelInvalidInput;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private GLWindow glWindow;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelPixelFormat;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelTimecode;
    }
}

