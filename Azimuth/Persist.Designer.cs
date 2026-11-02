namespace Azimuth
{
    partial class Persist
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
            this.lbAvailableGalaxies = new System.Windows.Forms.ListBox();
            this.lblSavedGalaxies = new System.Windows.Forms.Label();
            this.btnDeleteGalaxy = new System.Windows.Forms.Button();
            this.btnSetGalaxyAsCurrent = new System.Windows.Forms.Button();
            this.btnLoadSelected = new System.Windows.Forms.Button();
            this.lblDeletesSelected = new System.Windows.Forms.Label();
            this.lblLoadsSelected = new System.Windows.Forms.Label();
            this.lblCopiesSel = new System.Windows.Forms.Label();
            this.tbGalaxyName = new System.Windows.Forms.TextBox();
            this.btnRandomName = new System.Windows.Forms.Button();
            this.lblSaveGenMapAs = new System.Windows.Forms.Label();
            this.btnSaveNew = new System.Windows.Forms.Button();
            this.rtb1 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // lbAvailableGalaxies
            // 
            this.lbAvailableGalaxies.FormattingEnabled = true;
            this.lbAvailableGalaxies.Location = new System.Drawing.Point(12, 29);
            this.lbAvailableGalaxies.Name = "lbAvailableGalaxies";
            this.lbAvailableGalaxies.Size = new System.Drawing.Size(300, 300);
            this.lbAvailableGalaxies.TabIndex = 0;
            this.lbAvailableGalaxies.SelectedValueChanged += new System.EventHandler(this.lbAvailableGalaxies_SelectedValueChanged);
            // 
            // lblSavedGalaxies
            // 
            this.lblSavedGalaxies.AutoSize = true;
            this.lblSavedGalaxies.Location = new System.Drawing.Point(12, 13);
            this.lblSavedGalaxies.Name = "lblSavedGalaxies";
            this.lblSavedGalaxies.Size = new System.Drawing.Size(84, 13);
            this.lblSavedGalaxies.TabIndex = 1;
            this.lblSavedGalaxies.Text = "Saved Galaxies:";
            // 
            // btnDeleteGalaxy
            // 
            this.btnDeleteGalaxy.Location = new System.Drawing.Point(335, 265);
            this.btnDeleteGalaxy.Name = "btnDeleteGalaxy";
            this.btnDeleteGalaxy.Size = new System.Drawing.Size(160, 30);
            this.btnDeleteGalaxy.TabIndex = 2;
            this.btnDeleteGalaxy.Text = "Delete";
            this.btnDeleteGalaxy.UseVisualStyleBackColor = true;
            this.btnDeleteGalaxy.Click += new System.EventHandler(this.btnDeleteGalaxy_Click);
            // 
            // btnSetGalaxyAsCurrent
            // 
            this.btnSetGalaxyAsCurrent.Location = new System.Drawing.Point(335, 110);
            this.btnSetGalaxyAsCurrent.Name = "btnSetGalaxyAsCurrent";
            this.btnSetGalaxyAsCurrent.Size = new System.Drawing.Size(160, 30);
            this.btnSetGalaxyAsCurrent.TabIndex = 3;
            this.btnSetGalaxyAsCurrent.Text = "Set Active";
            this.btnSetGalaxyAsCurrent.UseVisualStyleBackColor = true;
            this.btnSetGalaxyAsCurrent.Click += new System.EventHandler(this.btnSetGalaxyAsActive_Click);
            // 
            // btnLoadSelected
            // 
            this.btnLoadSelected.Location = new System.Drawing.Point(335, 200);
            this.btnLoadSelected.Name = "btnLoadSelected";
            this.btnLoadSelected.Size = new System.Drawing.Size(160, 30);
            this.btnLoadSelected.TabIndex = 4;
            this.btnLoadSelected.Text = "Load";
            this.btnLoadSelected.UseVisualStyleBackColor = true;
            this.btnLoadSelected.Click += new System.EventHandler(this.btnLoadSelected_Click);
            // 
            // lblDeletesSelected
            // 
            this.lblDeletesSelected.AutoSize = true;
            this.lblDeletesSelected.Location = new System.Drawing.Point(335, 246);
            this.lblDeletesSelected.Name = "lblDeletesSelected";
            this.lblDeletesSelected.Size = new System.Drawing.Size(160, 30);
            this.lblDeletesSelected.TabIndex = 7;
            this.lblDeletesSelected.Text = "Deletes selected Map folder and files.";
            // 
            // lblLoadsSelected
            // 
            this.lblLoadsSelected.AutoSize = true;
            this.lblLoadsSelected.Location = new System.Drawing.Point(335, 181);
            this.lblLoadsSelected.Name = "lblLoadsSelected";
            this.lblLoadsSelected.Size = new System.Drawing.Size(191, 13);
            this.lblLoadsSelected.TabIndex = 8;
            this.lblLoadsSelected.Text = "Loads selected Map into the Generator";
            // 
            // lblCopiesSel
            // 
            this.lblCopiesSel.AutoSize = true;
            this.lblCopiesSel.Location = new System.Drawing.Point(335, 91);
            this.lblCopiesSel.Name = "lblCopiesSel";
            this.lblCopiesSel.Size = new System.Drawing.Size(135, 13);
            this.lblCopiesSel.TabIndex = 9;
            this.lblCopiesSel.Text = "Copies Selected map to X3";
            // 
            // tbGalaxyName
            // 
            this.tbGalaxyName.Location = new System.Drawing.Point(475, 53);
            this.tbGalaxyName.Name = "tbGalaxyName";
            this.tbGalaxyName.Size = new System.Drawing.Size(140, 20);
            this.tbGalaxyName.TabIndex = 12;
            // 
            // btnRandomName
            // 
            this.btnRandomName.Location = new System.Drawing.Point(625, 48);
            this.btnRandomName.Name = "btnRandomName";
            this.btnRandomName.Size = new System.Drawing.Size(90, 30);
            this.btnRandomName.TabIndex = 14;
            this.btnRandomName.Text = "Random";
            this.btnRandomName.UseVisualStyleBackColor = true;
            this.btnRandomName.Click += new System.EventHandler(this.btnRandomName_Click);
            // 
            // lblSaveGenMapAs
            // 
            this.lblSaveGenMapAs.AutoSize = true;
            this.lblSaveGenMapAs.Location = new System.Drawing.Point(335, 32);
            this.lblSaveGenMapAs.Name = "lblSaveGenMapAs";
            this.lblSaveGenMapAs.Size = new System.Drawing.Size(128, 13);
            this.lblSaveGenMapAs.TabIndex = 16;
            this.lblSaveGenMapAs.Text = "Saves Generator Map as:";
            // 
            // btnSaveNew
            // 
            this.btnSaveNew.Location = new System.Drawing.Point(335, 48);
            this.btnSaveNew.Name = "btnSaveNew";
            this.btnSaveNew.Size = new System.Drawing.Size(130, 30);
            this.btnSaveNew.TabIndex = 15;
            this.btnSaveNew.Text = "Save";
            this.btnSaveNew.UseVisualStyleBackColor = true;
            this.btnSaveNew.Click += new System.EventHandler(this.btnSaveNew_Click);
            // 
            // rtb1
            // 
            this.rtb1.Anchor = ((System.Windows.Forms.AnchorStyles)
                ((((System.Windows.Forms.AnchorStyles.Top |
                    System.Windows.Forms.AnchorStyles.Bottom) |
                    System.Windows.Forms.AnchorStyles.Left) |
                    System.Windows.Forms.AnchorStyles.Right)));
            this.rtb1.Location = new System.Drawing.Point(12, 345);
            this.rtb1.Name = "rtb1";
            // 1.8.6: Was 538, 151
            this.rtb1.Size = new System.Drawing.Size(726, 193);
            this.rtb1.TabIndex = 17;
            this.rtb1.Text = "";
            // 
            // Persist
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 550);
            this.MinimumSize = new System.Drawing.Size(750, 550);
            this.MaximumSize = new System.Drawing.Size(1200, 800);

            //this.ClientSize = new System.Drawing.Size(564, 449);
            this.Controls.Add(this.rtb1);
            this.Controls.Add(this.lblSaveGenMapAs);
            this.Controls.Add(this.btnSaveNew);
            this.Controls.Add(this.btnRandomName);
            this.Controls.Add(this.tbGalaxyName);
            this.Controls.Add(this.lblCopiesSel);
            this.Controls.Add(this.lblLoadsSelected);
            this.Controls.Add(this.lblDeletesSelected);
            this.Controls.Add(this.btnLoadSelected);
            this.Controls.Add(this.btnSetGalaxyAsCurrent);
            this.Controls.Add(this.btnDeleteGalaxy);
            this.Controls.Add(this.lblSavedGalaxies);
            this.Controls.Add(this.lbAvailableGalaxies);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "Persist";
            this.Text = "Save/Load";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox lbAvailableGalaxies;
        private System.Windows.Forms.Label lblSavedGalaxies;
        private System.Windows.Forms.Button btnDeleteGalaxy;
        private System.Windows.Forms.Button btnSetGalaxyAsCurrent;
        private System.Windows.Forms.Button btnLoadSelected;
        private System.Windows.Forms.Label lblDeletesSelected;
        private System.Windows.Forms.Label lblLoadsSelected;
        private System.Windows.Forms.Label lblCopiesSel;
        private System.Windows.Forms.TextBox tbGalaxyName;
        private System.Windows.Forms.Button btnRandomName;
        private System.Windows.Forms.Label lblSaveGenMapAs;
        private System.Windows.Forms.Button btnSaveNew;
        private System.Windows.Forms.RichTextBox rtb1;
    }
}