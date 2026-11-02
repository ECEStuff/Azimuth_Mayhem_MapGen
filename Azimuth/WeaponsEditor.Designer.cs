
namespace Azimuth
{
    partial class WeaponsEditor
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
            this.gbAction = new System.Windows.Forms.GroupBox();
            this.rbRaiseShipStatsForRace = new System.Windows.Forms.RadioButton();
            this.rbLowerShipStatsForRace = new System.Windows.Forms.RadioButton();
            this.rbAllShipStatsForRace = new System.Windows.Forms.RadioButton();
            this.rbAllShipStats = new System.Windows.Forms.RadioButton();
            this.rbRandomizeAllWeapons = new System.Windows.Forms.RadioButton();
            this.btnDoIt = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.cbRace = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnRestore = new System.Windows.Forms.Button();
            this.lblNote = new System.Windows.Forms.Label();
            this.btnHelpMain = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbTwentypercent = new System.Windows.Forms.RadioButton();
            this.rbThirtypercent = new System.Windows.Forms.RadioButton();
            this.gbAction.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbAction
            // 
            this.gbAction.Controls.Add(this.rbRaiseShipStatsForRace);
            this.gbAction.Controls.Add(this.rbLowerShipStatsForRace);
            this.gbAction.Controls.Add(this.rbAllShipStatsForRace);
            this.gbAction.Controls.Add(this.rbAllShipStats);
            this.gbAction.Controls.Add(this.rbRandomizeAllWeapons);
            this.gbAction.Location = new System.Drawing.Point(20, 21);
            this.gbAction.Name = "gbAction";
            this.gbAction.Size = new System.Drawing.Size(247, 165);
            this.gbAction.TabIndex = 0;
            this.gbAction.TabStop = false;
            this.gbAction.Text = "Action";
            // 
            // rbRaiseShipStatsForRace
            // 
            this.rbRaiseShipStatsForRace.AutoSize = true;
            this.rbRaiseShipStatsForRace.Location = new System.Drawing.Point(21, 134);
            this.rbRaiseShipStatsForRace.Name = "rbRaiseShipStatsForRace";
            this.rbRaiseShipStatsForRace.Size = new System.Drawing.Size(165, 17);
            this.rbRaiseShipStatsForRace.TabIndex = 4;
            this.rbRaiseShipStatsForRace.Text = "Raise all ship stats for a Race";
            this.rbRaiseShipStatsForRace.UseVisualStyleBackColor = true;
            this.rbRaiseShipStatsForRace.Click += new System.EventHandler(this.rbRaiseShipStatsForRace_Click);
            // 
            // rbLowerShipStatsForRace
            // 
            this.rbLowerShipStatsForRace.AutoSize = true;
            this.rbLowerShipStatsForRace.Location = new System.Drawing.Point(21, 111);
            this.rbLowerShipStatsForRace.Name = "rbLowerShipStatsForRace";
            this.rbLowerShipStatsForRace.Size = new System.Drawing.Size(167, 17);
            this.rbLowerShipStatsForRace.TabIndex = 3;
            this.rbLowerShipStatsForRace.Text = "Lower all ship stats for a Race";
            this.rbLowerShipStatsForRace.UseVisualStyleBackColor = true;
            this.rbLowerShipStatsForRace.Click += new System.EventHandler(this.rbLowerShipStatsForRace_Click);
            // 
            // rbAllShipStatsForRace
            // 
            this.rbAllShipStatsForRace.AutoSize = true;
            this.rbAllShipStatsForRace.Location = new System.Drawing.Point(21, 88);
            this.rbAllShipStatsForRace.Name = "rbAllShipStatsForRace";
            this.rbAllShipStatsForRace.Size = new System.Drawing.Size(191, 17);
            this.rbAllShipStatsForRace.TabIndex = 2;
            this.rbAllShipStatsForRace.Text = "Randomize all ship stats for a Race";
            this.rbAllShipStatsForRace.UseVisualStyleBackColor = true;
            this.rbAllShipStatsForRace.Click += new System.EventHandler(this.rbAllShipStatsForRace_Click);
            // 
            // rbAllShipStats
            // 
            this.rbAllShipStats.AutoSize = true;
            this.rbAllShipStats.Location = new System.Drawing.Point(21, 65);
            this.rbAllShipStats.Name = "rbAllShipStats";
            this.rbAllShipStats.Size = new System.Drawing.Size(138, 17);
            this.rbAllShipStats.TabIndex = 1;
            this.rbAllShipStats.Text = "Randomize all ship stats";
            this.rbAllShipStats.UseVisualStyleBackColor = true;
            this.rbAllShipStats.Click += new System.EventHandler(this.rbAllShipStats_Click);
            // 
            // rbRandomizeAllWeapons
            // 
            this.rbRandomizeAllWeapons.AutoSize = true;
            this.rbRandomizeAllWeapons.Checked = true;
            this.rbRandomizeAllWeapons.Location = new System.Drawing.Point(21, 31);
            this.rbRandomizeAllWeapons.Name = "rbRandomizeAllWeapons";
            this.rbRandomizeAllWeapons.Size = new System.Drawing.Size(149, 17);
            this.rbRandomizeAllWeapons.TabIndex = 0;
            this.rbRandomizeAllWeapons.TabStop = true;
            this.rbRandomizeAllWeapons.Text = "Randomize weapons stats";
            this.rbRandomizeAllWeapons.UseVisualStyleBackColor = true;
            this.rbRandomizeAllWeapons.Click += new System.EventHandler(this.rbRandomizeAllWeapons_Click);
            // 
            // btnDoIt
            // 
            this.btnDoIt.Location = new System.Drawing.Point(291, 23);
            this.btnDoIt.Name = "btnDoIt";
            this.btnDoIt.Size = new System.Drawing.Size(118, 23);
            this.btnDoIt.TabIndex = 1;
            this.btnDoIt.Text = "Execute";
            this.btnDoIt.UseVisualStyleBackColor = true;
            this.btnDoIt.Click += new System.EventHandler(this.btnDoIt_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(308, 175);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 2;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // cbRace
            // 
            this.cbRace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRace.FormattingEnabled = true;
            this.cbRace.Items.AddRange(new object[] {
            "Argon",
            "Boron",
            "Paranid",
            "Split",
            "Teladi",
            "Terran",
            "Yaki",
            "Xenon",
            "OCV"});
            this.cbRace.Location = new System.Drawing.Point(59, 192);
            this.cbRace.Name = "cbRace";
            this.cbRace.Size = new System.Drawing.Size(162, 21);
            this.cbRace.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 195);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Race";
            // 
            // btnRestore
            // 
            this.btnRestore.Location = new System.Drawing.Point(291, 49);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(118, 23);
            this.btnRestore.TabIndex = 5;
            this.btnRestore.Text = "Restore Defaults";
            this.btnRestore.UseVisualStyleBackColor = true;
            this.btnRestore.Click += new System.EventHandler(this.btnRestore_Click);
            // 
            // lblNote
            // 
            this.lblNote.Location = new System.Drawing.Point(23, 233);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(415, 116);
            this.lblNote.TabIndex = 6;
            this.lblNote.Text = "The rain in spain falls maily";
            // 
            // btnHelpMain
            // 
            this.btnHelpMain.BackColor = System.Drawing.Color.LightGray;
            this.btnHelpMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHelpMain.ForeColor = System.Drawing.Color.Red;
            this.btnHelpMain.Location = new System.Drawing.Point(291, 132);
            this.btnHelpMain.Name = "btnHelpMain";
            this.btnHelpMain.Size = new System.Drawing.Size(75, 23);
            this.btnHelpMain.TabIndex = 94;
            this.btnHelpMain.Text = "Help";
            this.btnHelpMain.UseVisualStyleBackColor = false;
            this.btnHelpMain.Click += new System.EventHandler(this.btnHelpMain_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbThirtypercent);
            this.groupBox1.Controls.Add(this.rbTwentypercent);
            this.groupBox1.Location = new System.Drawing.Point(273, 96);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(147, 73);
            this.groupBox1.TabIndex = 95;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Maximum Stat Adjustment";
            // 
            // rbTwentypercent
            // 
            this.rbTwentypercent.AutoSize = true;
            this.rbTwentypercent.Checked = true;
            this.rbTwentypercent.Location = new System.Drawing.Point(7, 20);
            this.rbTwentypercent.Name = "rbTwentypercent";
            this.rbTwentypercent.Size = new System.Drawing.Size(62, 17);
            this.rbTwentypercent.TabIndex = 0;
            this.rbTwentypercent.TabStop = true;
            this.rbTwentypercent.Text = "+/- 20%";
            this.rbTwentypercent.UseVisualStyleBackColor = true;
            // 
            // rbThirtypercent
            // 
            this.rbThirtypercent.AutoSize = true;
            this.rbThirtypercent.Location = new System.Drawing.Point(7, 43);
            this.rbThirtypercent.Name = "rbThirtypercent";
            this.rbThirtypercent.Size = new System.Drawing.Size(62, 17);
            this.rbThirtypercent.TabIndex = 1;
            this.rbThirtypercent.Text = "+/- 30%";
            this.rbThirtypercent.UseVisualStyleBackColor = true;
            // 
            // WeaponsEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(456, 365);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnHelpMain);
            this.Controls.Add(this.lblNote);
            this.Controls.Add(this.btnRestore);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbRace);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnDoIt);
            this.Controls.Add(this.gbAction);
            this.Name = "WeaponsEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ship and Weapon Stats Editor";
            this.Load += new System.EventHandler(this.WeaponsEditor_Load);
            this.gbAction.ResumeLayout(false);
            this.gbAction.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbAction;
        private System.Windows.Forms.RadioButton rbAllShipStatsForRace;
        private System.Windows.Forms.RadioButton rbAllShipStats;
        private System.Windows.Forms.RadioButton rbRandomizeAllWeapons;
        private System.Windows.Forms.Button btnDoIt;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.RadioButton rbRaiseShipStatsForRace;
        private System.Windows.Forms.RadioButton rbLowerShipStatsForRace;
        private System.Windows.Forms.ComboBox cbRace;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.Button btnHelpMain;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbThirtypercent;
        private System.Windows.Forms.RadioButton rbTwentypercent;
    }
}