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
            this.pictureBoxDisplay = new System.Windows.Forms.PictureBox();
            this.comboBoxBinFiles = new System.Windows.Forms.ComboBox();
            this.comboBoxImages = new System.Windows.Forms.ComboBox();
            this.buttonAbrirArquivos = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxDisplay
            // 
            this.pictureBoxDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxDisplay.Location = new System.Drawing.Point(204, 12);
            this.pictureBoxDisplay.Name = "pictureBoxDisplay";
            this.pictureBoxDisplay.Size = new System.Drawing.Size(584, 558);
            this.pictureBoxDisplay.TabIndex = 0;
            this.pictureBoxDisplay.TabStop = false;
            // 
            // comboBoxBinFiles
            // 
            this.comboBoxBinFiles.FormattingEnabled = true;
            this.comboBoxBinFiles.Location = new System.Drawing.Point(12, 12);
            this.comboBoxBinFiles.Name = "comboBoxBinFiles";
            this.comboBoxBinFiles.Size = new System.Drawing.Size(186, 21);
            this.comboBoxBinFiles.TabIndex = 1;
            // 
            // comboBoxImages
            // 
            this.comboBoxImages.Enabled = false;
            this.comboBoxImages.FormattingEnabled = true;
            this.comboBoxImages.Location = new System.Drawing.Point(12, 39);
            this.comboBoxImages.Name = "comboBoxImages";
            this.comboBoxImages.Size = new System.Drawing.Size(186, 21);
            this.comboBoxImages.TabIndex = 2;
            // 
            // buttonAbrirArquivos
            // 
            this.buttonAbrirArquivos.Location = new System.Drawing.Point(36, 108);
            this.buttonAbrirArquivos.Name = "buttonAbrirArquivos";
            this.buttonAbrirArquivos.Size = new System.Drawing.Size(75, 23);
            this.buttonAbrirArquivos.TabIndex = 3;
            this.buttonAbrirArquivos.Text = "button1";
            this.buttonAbrirArquivos.UseVisualStyleBackColor = true;
            this.buttonAbrirArquivos.Click += new System.EventHandler(this.buttonAbrirArquivos_Click);
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 582);
            this.Controls.Add(this.buttonAbrirArquivos);
            this.Controls.Add(this.comboBoxImages);
            this.Controls.Add(this.comboBoxBinFiles);
            this.Controls.Add(this.pictureBoxDisplay);
            this.Name = "Form2";
            this.Text = "Form2";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDisplay)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxDisplay;
        private System.Windows.Forms.ComboBox comboBoxBinFiles;
        private System.Windows.Forms.ComboBox comboBoxImages;
        private System.Windows.Forms.Button buttonAbrirArquivos;
    }
}