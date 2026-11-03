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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Persist));
            this.lbAvailableGalaxies = new System.Windows.Forms.ListBox();
            this.lblSavedGalaxies = new System.Windows.Forms.Label();
            this.btnDeleteGalaxy = new System.Windows.Forms.Button();
            this.btnSetGalaxyAsCurrent = new System.Windows.Forms.Button();
            this.btnLoadSelected = new System.Windows.Forms.Button();
            this.tbGalaxyName = new System.Windows.Forms.TextBox();
            this.btnRandomName = new System.Windows.Forms.Button();
            this.lblSaveGenMapAs = new System.Windows.Forms.Label();
            this.btnSaveNew = new System.Windows.Forms.Button();
            this.rtb1 = new System.Windows.Forms.RichTextBox();
            this.btnIdentifyCurrentSaves = new System.Windows.Forms.Button();
            this.tipSaveLoad = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // lbAvailableGalaxies
            // 
            this.lbAvailableGalaxies.FormattingEnabled = true;
            this.lbAvailableGalaxies.Location = new System.Drawing.Point(12, 30);
            this.lbAvailableGalaxies.Name = "lbAvailableGalaxies";
            this.lbAvailableGalaxies.Size = new System.Drawing.Size(300, 290);
            this.lbAvailableGalaxies.TabIndex = 0;
            this.lbAvailableGalaxies.SelectedValueChanged += new System.EventHandler(this.lbAvailableGalaxies_SelectedValueChanged);
            // 
            // lblSavedGalaxies
            // 
            this.lblSavedGalaxies.AutoSize = true;
            this.lblSavedGalaxies.Location = new System.Drawing.Point(12, 12);
            this.lblSavedGalaxies.Name = "lblSavedGalaxies";
            this.lblSavedGalaxies.Size = new System.Drawing.Size(84, 13);
            this.lblSavedGalaxies.TabIndex = 1;
            this.lblSavedGalaxies.Text = "Saved Galaxies:";
            // 
            // btnDeleteGalaxy
            // 
            this.btnDeleteGalaxy.Location = new System.Drawing.Point(335, 150);
            this.btnDeleteGalaxy.Name = "btnDeleteGalaxy";
            this.btnDeleteGalaxy.Size = new System.Drawing.Size(160, 30);
            this.btnDeleteGalaxy.TabIndex = 2;
            this.btnDeleteGalaxy.Text = "Delete";
            this.tipSaveLoad.SetToolTip(this.btnDeleteGalaxy, "Delete the selected galaxy’s saved map folder and its files.");
            this.btnDeleteGalaxy.UseVisualStyleBackColor = true;
            this.btnDeleteGalaxy.Click += new System.EventHandler(this.btnDeleteGalaxy_Click);
            // 
            // btnSetGalaxyAsCurrent
            // 
            this.btnSetGalaxyAsCurrent.Location = new System.Drawing.Point(507, 110);
            this.btnSetGalaxyAsCurrent.Name = "btnSetGalaxyAsCurrent";
            this.btnSetGalaxyAsCurrent.Size = new System.Drawing.Size(160, 30);
            this.btnSetGalaxyAsCurrent.TabIndex = 3;
            this.btnSetGalaxyAsCurrent.Text = "Set Active";
            this.tipSaveLoad.SetToolTip(this.btnSetGalaxyAsCurrent, "Switch X3 to the selected galaxy and use its save slots.");
            this.btnSetGalaxyAsCurrent.UseVisualStyleBackColor = true;
            this.btnSetGalaxyAsCurrent.Click += new System.EventHandler(this.btnSetGalaxyAsActive_Click);
            // 
            // btnLoadSelected
            // 
            this.btnLoadSelected.Location = new System.Drawing.Point(335, 110);
            this.btnLoadSelected.Name = "btnLoadSelected";
            this.btnLoadSelected.Size = new System.Drawing.Size(160, 30);
            this.btnLoadSelected.TabIndex = 4;
            this.btnLoadSelected.Text = " Load in Editor";
            this.tipSaveLoad.SetToolTip(this.btnLoadSelected, "Open the selected galaxy in the editor. Your active X3 galaxy stays the same.");
            this.btnLoadSelected.UseVisualStyleBackColor = true;
            this.btnLoadSelected.Click += new System.EventHandler(this.btnLoadSelected_Click);
            // 
            // tbGalaxyName
            // 
            this.tbGalaxyName.Location = new System.Drawing.Point(335, 35);
            this.tbGalaxyName.Name = "tbGalaxyName";
            this.tbGalaxyName.Size = new System.Drawing.Size(160, 20);
            this.tbGalaxyName.TabIndex = 12;
            // 
            // btnRandomName
            // 
            this.btnRandomName.Location = new System.Drawing.Point(507, 30);
            this.btnRandomName.Name = "btnRandomName";
            this.btnRandomName.Size = new System.Drawing.Size(100, 30);
            this.btnRandomName.TabIndex = 14;
            this.btnRandomName.Text = "Random Name";
            this.tipSaveLoad.SetToolTip(this.btnRandomName, "Fill in a random galaxy name. Click Save when you’re ready.");
            this.btnRandomName.UseVisualStyleBackColor = true;
            this.btnRandomName.Click += new System.EventHandler(this.btnRandomName_Click);
            // 
            // lblSaveGenMapAs
            // 
            this.lblSaveGenMapAs.AutoSize = true;
            this.lblSaveGenMapAs.Location = new System.Drawing.Point(335, 17);
            this.lblSaveGenMapAs.Name = "lblSaveGenMapAs";
            this.lblSaveGenMapAs.Size = new System.Drawing.Size(70, 13);
            this.lblSaveGenMapAs.TabIndex = 16;
            this.lblSaveGenMapAs.Text = "Galaxy Name";
            // 
            // btnSaveNew
            // 
            this.btnSaveNew.Location = new System.Drawing.Point(619, 30);
            this.btnSaveNew.Name = "btnSaveNew";
            this.btnSaveNew.Size = new System.Drawing.Size(105, 30);
            this.btnSaveNew.TabIndex = 15;
            this.btnSaveNew.Text = "Save";
            this.tipSaveLoad.SetToolTip(this.btnSaveNew, "Save the map currently open in the editor under the name entered here.");
            this.btnSaveNew.UseVisualStyleBackColor = true;
            this.btnSaveNew.Click += new System.EventHandler(this.btnSaveNew_Click);
            // 
            // rtb1
            // 
            this.rtb1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtb1.Location = new System.Drawing.Point(12, 345);
            this.rtb1.Name = "rtb1";
            this.rtb1.Size = new System.Drawing.Size(726, 193);
            this.rtb1.TabIndex = 17;
            this.rtb1.Text = "";
            // 
            // btnIdentifyCurrentSaves
            // 
            this.btnIdentifyCurrentSaves.Location = new System.Drawing.Point(507, 150);
            this.btnIdentifyCurrentSaves.Name = "btnIdentifyCurrentSaves";
            this.btnIdentifyCurrentSaves.Size = new System.Drawing.Size(160, 30);
            this.btnIdentifyCurrentSaves.TabIndex = 18;
            this.btnIdentifyCurrentSaves.Text = "Identify Current Saves...";
            this.tipSaveLoad.SetToolTip(this.btnIdentifyCurrentSaves, "Select the galaxy your current X3 saves came from, then click here to record it a" +
        "s their owner.");
            this.btnIdentifyCurrentSaves.UseVisualStyleBackColor = true;
            this.btnIdentifyCurrentSaves.Click += new System.EventHandler(this.IdentifyCurrentSaves);
            // 
            // Persist
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 550);
            this.Controls.Add(this.btnIdentifyCurrentSaves);
            this.Controls.Add(this.rtb1);
            this.Controls.Add(this.lblSaveGenMapAs);
            this.Controls.Add(this.btnSaveNew);
            this.Controls.Add(this.btnRandomName);
            this.Controls.Add(this.tbGalaxyName);
            this.Controls.Add(this.btnLoadSelected);
            this.Controls.Add(this.btnSetGalaxyAsCurrent);
            this.Controls.Add(this.btnDeleteGalaxy);
            this.Controls.Add(this.lblSavedGalaxies);
            this.Controls.Add(this.lbAvailableGalaxies);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(1200, 800);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(750, 550);
            this.Name = "Persist";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
        private System.Windows.Forms.TextBox tbGalaxyName;
        private System.Windows.Forms.Button btnRandomName;
        private System.Windows.Forms.Label lblSaveGenMapAs;
        private System.Windows.Forms.Button btnSaveNew;
        private System.Windows.Forms.RichTextBox rtb1;
        private System.Windows.Forms.Button btnIdentifyCurrentSaves;
        private System.Windows.Forms.ToolTip tipSaveLoad;
    }
}