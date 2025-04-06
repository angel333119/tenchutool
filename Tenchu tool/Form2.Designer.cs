namespace Tenchu_tool
{
    partial class Form2
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.pictureBoxDisplay = new System.Windows.Forms.PictureBox();
            this.comboBoxBinFiles = new System.Windows.Forms.ComboBox();
            this.comboBoxImages = new System.Windows.Forms.ComboBox();
            this.buttonAbrirArquivos = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxDisplay
            // 
            this.pictureBoxDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxDisplay.Location = new System.Drawing.Point(256, 12);
            this.pictureBoxDisplay.Name = "pictureBoxDisplay";
            this.pictureBoxDisplay.Size = new System.Drawing.Size(592, 558);
            this.pictureBoxDisplay.TabIndex = 0;
            this.pictureBoxDisplay.TabStop = false;
            // 
            // comboBoxBinFiles
            // 
            this.comboBoxBinFiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBinFiles.FormattingEnabled = true;
            this.comboBoxBinFiles.Location = new System.Drawing.Point(101, 65);
            this.comboBoxBinFiles.Name = "comboBoxBinFiles";
            this.comboBoxBinFiles.Size = new System.Drawing.Size(149, 21);
            this.comboBoxBinFiles.TabIndex = 1;
            // 
            // comboBoxImages
            // 
            this.comboBoxImages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImages.Enabled = false;
            this.comboBoxImages.FormattingEnabled = true;
            this.comboBoxImages.Location = new System.Drawing.Point(101, 92);
            this.comboBoxImages.Name = "comboBoxImages";
            this.comboBoxImages.Size = new System.Drawing.Size(149, 21);
            this.comboBoxImages.TabIndex = 2;
            // 
            // buttonAbrirArquivos
            // 
            this.buttonAbrirArquivos.Location = new System.Drawing.Point(101, 36);
            this.buttonAbrirArquivos.Name = "buttonAbrirArquivos";
            this.buttonAbrirArquivos.Size = new System.Drawing.Size(149, 23);
            this.buttonAbrirArquivos.TabIndex = 3;
            this.buttonAbrirArquivos.Text = "Open File";
            this.buttonAbrirArquivos.UseVisualStyleBackColor = true;
            this.buttonAbrirArquivos.Click += new System.EventHandler(this.buttonAbrirArquivos_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 73);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Select a BIN file";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 100);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Select a image";
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(860, 582);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonAbrirArquivos);
            this.Controls.Add(this.comboBoxImages);
            this.Controls.Add(this.comboBoxBinFiles);
            this.Controls.Add(this.pictureBoxDisplay);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form2";
            this.Text = "Tenchu Fatal Shadows Texture Visualizer, Extractor and Inserter for PS2 and PSP";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxDisplay;
        private System.Windows.Forms.ComboBox comboBoxBinFiles;
        private System.Windows.Forms.ComboBox comboBoxImages;
        private System.Windows.Forms.Button buttonAbrirArquivos;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}