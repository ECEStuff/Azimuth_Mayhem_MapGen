namespace Azimuth
{
    partial class SectorEdit
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
            this.lbsSecId = new System.Windows.Forms.Label();
            this.lblDejOwner = new System.Windows.Forms.Label();
            this.lblCompany = new System.Windows.Forms.Label();
            this.lblCons = new System.Windows.Forms.Label();
            this.lblProd = new System.Windows.Forms.Label();
            this.lblManpower = new System.Windows.Forms.Label();
            this.lblSupport = new System.Windows.Forms.Label();
            this.lblResearch = new System.Windows.Forms.Label();
            this.lblTerranMem = new System.Windows.Forms.Label();
            this.lblAbbySigs = new System.Windows.Forms.Label();
            this.lblSectorId = new System.Windows.Forms.Label();
            this.cbDejureOwner = new System.Windows.Forms.ComboBox();
            this.nudAbnormalSignals = new System.Windows.Forms.NumericUpDown();
            this.cbTerranMemory = new System.Windows.Forms.CheckBox();
            this.nudResearch = new System.Windows.Forms.NumericUpDown();
            this.nudSupport = new System.Windows.Forms.NumericUpDown();
            this.nudManpower = new System.Windows.Forms.NumericUpDown();
            this.cbProduction = new System.Windows.Forms.ComboBox();
            this.cbConsumption = new System.Windows.Forms.ComboBox();
            this.cbCompany = new System.Windows.Forms.ComboBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.cbOwner = new System.Windows.Forms.ComboBox();
            this.lblOwner = new System.Windows.Forms.Label();
            this.nudSystemSize = new System.Windows.Forms.NumericUpDown();
            this.lblSysSize = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnChangeMusicFile = new System.Windows.Forms.Button();
            this.lblMusicFile = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbSunLevel = new System.Windows.Forms.ComboBox();
            this.lblPlanets = new System.Windows.Forms.Label();
            this.lblPlanetCount = new System.Windows.Forms.Label();
            this.lblSunLevel = new System.Windows.Forms.Label();
            this.btnGateSouth = new System.Windows.Forms.Button();
            this.btnGateWest = new System.Windows.Forms.Button();
            this.btnGateNorth = new System.Windows.Forms.Button();
            this.btnGateEast = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.cbMaelstrom = new System.Windows.Forms.CheckBox();
            this.lblMaelstromStart = new System.Windows.Forms.Label();
            this.lblStartSec = new System.Windows.Forms.Label();
            this.cbStartSector = new System.Windows.Forms.CheckBox();
            this.lblSystemName = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tipSectorEdit = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.nudAbnormalSignals)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudResearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSupport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudManpower)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSystemSize)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbsSecId
            // 
            this.lbsSecId.AutoSize = true;
            this.lbsSecId.Location = new System.Drawing.Point(15, 14);
            this.lbsSecId.Name = "lbsSecId";
            this.lbsSecId.Size = new System.Drawing.Size(55, 13);
            this.lbsSecId.TabIndex = 2;
            this.lbsSecId.Text = "Sector ID:";
            // 
            // lblDejOwner
            // 
            this.lblDejOwner.AutoSize = true;
            this.lblDejOwner.Location = new System.Drawing.Point(238, 74);
            this.lblDejOwner.Name = "lblDejOwner";
            this.lblDejOwner.Size = new System.Drawing.Size(75, 13);
            this.lblDejOwner.TabIndex = 3;
            this.lblDejOwner.Text = "Dejure Owner:";
            // 
            // lblCompany
            // 
            this.lblCompany.AutoSize = true;
            this.lblCompany.Location = new System.Drawing.Point(238, 153);
            this.lblCompany.Name = "lblCompany";
            this.lblCompany.Size = new System.Drawing.Size(54, 13);
            this.lblCompany.TabIndex = 4;
            this.lblCompany.Text = "Company:";
            // 
            // lblCons
            // 
            this.lblCons.AutoSize = true;
            this.lblCons.Location = new System.Drawing.Point(238, 127);
            this.lblCons.Name = "lblCons";
            this.lblCons.Size = new System.Drawing.Size(71, 13);
            this.lblCons.TabIndex = 5;
            this.lblCons.Text = "Consumption:";
            // 
            // lblProd
            // 
            this.lblProd.AutoSize = true;
            this.lblProd.Location = new System.Drawing.Point(238, 100);
            this.lblProd.Name = "lblProd";
            this.lblProd.Size = new System.Drawing.Size(61, 13);
            this.lblProd.TabIndex = 6;
            this.lblProd.Text = "Production:";
            // 
            // lblManpower
            // 
            this.lblManpower.AutoSize = true;
            this.lblManpower.Location = new System.Drawing.Point(15, 49);
            this.lblManpower.Name = "lblManpower";
            this.lblManpower.Size = new System.Drawing.Size(60, 13);
            this.lblManpower.TabIndex = 7;
            this.lblManpower.Text = "Manpower:";
            // 
            // lblSupport
            // 
            this.lblSupport.AutoSize = true;
            this.lblSupport.Location = new System.Drawing.Point(15, 74);
            this.lblSupport.Name = "lblSupport";
            this.lblSupport.Size = new System.Drawing.Size(47, 13);
            this.lblSupport.TabIndex = 8;
            this.lblSupport.Text = "Support:";
            // 
            // lblResearch
            // 
            this.lblResearch.AutoSize = true;
            this.lblResearch.Location = new System.Drawing.Point(15, 100);
            this.lblResearch.Name = "lblResearch";
            this.lblResearch.Size = new System.Drawing.Size(56, 13);
            this.lblResearch.TabIndex = 9;
            this.lblResearch.Text = "Research:";
            // 
            // lblTerranMem
            // 
            this.lblTerranMem.AutoSize = true;
            this.lblTerranMem.Location = new System.Drawing.Point(15, 182);
            this.lblTerranMem.Name = "lblTerranMem";
            this.lblTerranMem.Size = new System.Drawing.Size(81, 13);
            this.lblTerranMem.TabIndex = 10;
            this.lblTerranMem.Text = "Terran Memory:";
            // 
            // lblAbbySigs
            // 
            this.lblAbbySigs.AutoSize = true;
            this.lblAbbySigs.Location = new System.Drawing.Point(15, 127);
            this.lblAbbySigs.Name = "lblAbbySigs";
            this.lblAbbySigs.Size = new System.Drawing.Size(91, 13);
            this.lblAbbySigs.TabIndex = 11;
            this.lblAbbySigs.Text = "Abnormal Signals:";
            // 
            // lblSectorId
            // 
            this.lblSectorId.AutoSize = true;
            this.lblSectorId.Location = new System.Drawing.Point(76, 14);
            this.lblSectorId.Name = "lblSectorId";
            this.lblSectorId.Size = new System.Drawing.Size(66, 13);
            this.lblSectorId.TabIndex = 14;
            this.lblSectorId.Text = "sectoridlabel";
            // 
            // cbDejureOwner
            // 
            this.cbDejureOwner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDejureOwner.FormattingEnabled = true;
            this.cbDejureOwner.Location = new System.Drawing.Point(346, 72);
            this.cbDejureOwner.Name = "cbDejureOwner";
            this.cbDejureOwner.Size = new System.Drawing.Size(70, 21);
            this.cbDejureOwner.TabIndex = 24;
            // 
            // nudAbnormalSignals
            // 
            this.nudAbnormalSignals.Location = new System.Drawing.Point(146, 124);
            this.nudAbnormalSignals.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.nudAbnormalSignals.Name = "nudAbnormalSignals";
            this.nudAbnormalSignals.Size = new System.Drawing.Size(53, 20);
            this.nudAbnormalSignals.TabIndex = 25;
            // 
            // cbTerranMemory
            // 
            this.cbTerranMemory.AutoSize = true;
            this.cbTerranMemory.Location = new System.Drawing.Point(146, 182);
            this.cbTerranMemory.Name = "cbTerranMemory";
            this.cbTerranMemory.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cbTerranMemory.Size = new System.Drawing.Size(15, 14);
            this.cbTerranMemory.TabIndex = 26;
            this.cbTerranMemory.UseVisualStyleBackColor = true;
            // 
            // nudResearch
            // 
            this.nudResearch.Location = new System.Drawing.Point(146, 98);
            this.nudResearch.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.nudResearch.Name = "nudResearch";
            this.nudResearch.Size = new System.Drawing.Size(53, 20);
            this.nudResearch.TabIndex = 27;
            this.nudResearch.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // nudSupport
            // 
            this.nudSupport.Location = new System.Drawing.Point(146, 72);
            this.nudSupport.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.nudSupport.Minimum = new decimal(new int[] {
            6,
            0,
            0,
            0});
            this.nudSupport.Name = "nudSupport";
            this.nudSupport.Size = new System.Drawing.Size(53, 20);
            this.nudSupport.TabIndex = 28;
            this.nudSupport.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // nudManpower
            // 
            this.nudManpower.Location = new System.Drawing.Point(146, 46);
            this.nudManpower.Maximum = new decimal(new int[] {
            130,
            0,
            0,
            0});
            this.nudManpower.Minimum = new decimal(new int[] {
            48,
            0,
            0,
            0});
            this.nudManpower.Name = "nudManpower";
            this.nudManpower.Size = new System.Drawing.Size(53, 20);
            this.nudManpower.TabIndex = 29;
            this.nudManpower.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // cbProduction
            // 
            this.cbProduction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProduction.FormattingEnabled = true;
            this.cbProduction.Location = new System.Drawing.Point(346, 99);
            this.cbProduction.Name = "cbProduction";
            this.cbProduction.Size = new System.Drawing.Size(70, 21);
            this.cbProduction.TabIndex = 30;
            // 
            // cbConsumption
            // 
            this.cbConsumption.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConsumption.FormattingEnabled = true;
            this.cbConsumption.Location = new System.Drawing.Point(346, 126);
            this.cbConsumption.Name = "cbConsumption";
            this.cbConsumption.Size = new System.Drawing.Size(70, 21);
            this.cbConsumption.TabIndex = 31;
            // 
            // cbCompany
            // 
            this.cbCompany.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCompany.FormattingEnabled = true;
            this.cbCompany.Location = new System.Drawing.Point(346, 152);
            this.cbCompany.Name = "cbCompany";
            this.cbCompany.Size = new System.Drawing.Size(70, 21);
            this.cbCompany.TabIndex = 32;
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(346, 274);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 33;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // cbOwner
            // 
            this.cbOwner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbOwner.FormattingEnabled = true;
            this.cbOwner.Location = new System.Drawing.Point(346, 45);
            this.cbOwner.Name = "cbOwner";
            this.cbOwner.Size = new System.Drawing.Size(70, 21);
            this.cbOwner.TabIndex = 36;
            this.cbOwner.SelectedIndexChanged += new System.EventHandler(this.cbOwner_SelectedIndexChanged);
            // 
            // lblOwner
            // 
            this.lblOwner.AutoSize = true;
            this.lblOwner.Location = new System.Drawing.Point(238, 49);
            this.lblOwner.Name = "lblOwner";
            this.lblOwner.Size = new System.Drawing.Size(41, 13);
            this.lblOwner.TabIndex = 35;
            this.lblOwner.Text = "Owner:";
            // 
            // nudSystemSize
            // 
            this.nudSystemSize.Location = new System.Drawing.Point(146, 150);
            this.nudSystemSize.Maximum = new decimal(new int[] {
            27500000,
            0,
            0,
            0});
            this.nudSystemSize.Minimum = new decimal(new int[] {
            18500000,
            0,
            0,
            0});
            this.nudSystemSize.Name = "nudSystemSize";
            this.nudSystemSize.Size = new System.Drawing.Size(73, 20);
            this.nudSystemSize.TabIndex = 38;
            this.nudSystemSize.Value = new decimal(new int[] {
            18500000,
            0,
            0,
            0});
            // 
            // lblSysSize
            // 
            this.lblSysSize.AutoSize = true;
            this.lblSysSize.Location = new System.Drawing.Point(15, 153);
            this.lblSysSize.Name = "lblSysSize";
            this.lblSysSize.Size = new System.Drawing.Size(67, 13);
            this.lblSysSize.TabIndex = 37;
            this.lblSysSize.Text = "System Size:";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.btnChangeMusicFile);
            this.panel1.Controls.Add(this.lblMusicFile);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.cbSunLevel);
            this.panel1.Controls.Add(this.lblPlanets);
            this.panel1.Controls.Add(this.lblPlanetCount);
            this.panel1.Controls.Add(this.lblSunLevel);
            this.panel1.Controls.Add(this.btnGateSouth);
            this.panel1.Controls.Add(this.btnGateWest);
            this.panel1.Controls.Add(this.btnGateNorth);
            this.panel1.Controls.Add(this.btnGateEast);
            this.panel1.Controls.Add(this.checkBox1);
            this.panel1.Controls.Add(this.btnClose);
            this.panel1.Controls.Add(this.cbMaelstrom);
            this.panel1.Controls.Add(this.lblMaelstromStart);
            this.panel1.Controls.Add(this.lblStartSec);
            this.panel1.Controls.Add(this.cbStartSector);
            this.panel1.Controls.Add(this.lbsSecId);
            this.panel1.Controls.Add(this.nudSystemSize);
            this.panel1.Controls.Add(this.lblDejOwner);
            this.panel1.Controls.Add(this.lblSysSize);
            this.panel1.Controls.Add(this.lblCompany);
            this.panel1.Controls.Add(this.cbOwner);
            this.panel1.Controls.Add(this.lblCons);
            this.panel1.Controls.Add(this.lblOwner);
            this.panel1.Controls.Add(this.lblProd);
            this.panel1.Controls.Add(this.lblManpower);
            this.panel1.Controls.Add(this.btnOk);
            this.panel1.Controls.Add(this.lblSupport);
            this.panel1.Controls.Add(this.cbCompany);
            this.panel1.Controls.Add(this.lblResearch);
            this.panel1.Controls.Add(this.cbConsumption);
            this.panel1.Controls.Add(this.lblTerranMem);
            this.panel1.Controls.Add(this.cbProduction);
            this.panel1.Controls.Add(this.lblAbbySigs);
            this.panel1.Controls.Add(this.nudManpower);
            this.panel1.Controls.Add(this.lblSectorId);
            this.panel1.Controls.Add(this.nudSupport);
            this.panel1.Controls.Add(this.cbDejureOwner);
            this.panel1.Controls.Add(this.nudResearch);
            this.panel1.Controls.Add(this.nudAbnormalSignals);
            this.panel1.Controls.Add(this.cbTerranMemory);
            this.panel1.Location = new System.Drawing.Point(12, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(447, 310);
            this.panel1.TabIndex = 39;
            // 
            // btnChangeMusicFile
            // 
            this.btnChangeMusicFile.Location = new System.Drawing.Point(383, 9);
            this.btnChangeMusicFile.Name = "btnChangeMusicFile";
            this.btnChangeMusicFile.Size = new System.Drawing.Size(33, 23);
            this.btnChangeMusicFile.TabIndex = 55;
            this.btnChangeMusicFile.Text = "<--";
            this.btnChangeMusicFile.UseVisualStyleBackColor = true;
            this.btnChangeMusicFile.Click += new System.EventHandler(this.btnChangeMusicFile_Click);
            // 
            // lblMusicFile
            // 
            this.lblMusicFile.AutoSize = true;
            this.lblMusicFile.Location = new System.Drawing.Point(289, 14);
            this.lblMusicFile.Name = "lblMusicFile";
            this.lblMusicFile.Size = new System.Drawing.Size(78, 13);
            this.lblMusicFile.TabIndex = 54;
            this.lblMusicFile.Text = "00000000.mp3";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(226, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 53;
            this.label1.Text = "Music File:";
            // 
            // cbSunLevel
            // 
            this.cbSunLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSunLevel.FormattingEnabled = true;
            this.cbSunLevel.Location = new System.Drawing.Point(346, 179);
            this.cbSunLevel.Name = "cbSunLevel";
            this.cbSunLevel.Size = new System.Drawing.Size(70, 21);
            this.cbSunLevel.TabIndex = 52;
            // 
            // lblPlanets
            // 
            this.lblPlanets.AutoSize = true;
            this.lblPlanets.Location = new System.Drawing.Point(15, 205);
            this.lblPlanets.Name = "lblPlanets";
            this.lblPlanets.Size = new System.Drawing.Size(42, 13);
            this.lblPlanets.TabIndex = 56;
            this.lblPlanets.Text = "Planets";
            // 
            // lblPlanetCount
            // 
            this.lblPlanetCount.AutoSize = true;
            this.lblPlanetCount.Location = new System.Drawing.Point(146, 205);
            this.lblPlanetCount.Name = "lblPlanetCount";
            this.lblPlanetCount.Size = new System.Drawing.Size(13, 13);
            this.lblPlanetCount.TabIndex = 57;
            this.lblPlanetCount.Text = "0";
            // 
            // lblSunLevel
            // 
            this.lblSunLevel.AutoSize = true;
            this.lblSunLevel.Location = new System.Drawing.Point(238, 182);
            this.lblSunLevel.Name = "lblSunLevel";
            this.lblSunLevel.Size = new System.Drawing.Size(58, 13);
            this.lblSunLevel.TabIndex = 51;
            this.lblSunLevel.Text = "Sun Level:";
            // 
            // btnGateSouth
            // 
            this.btnGateSouth.BackColor = System.Drawing.Color.Maroon;
            this.btnGateSouth.Location = new System.Drawing.Point(95, 265);
            this.btnGateSouth.Margin = new System.Windows.Forms.Padding(0);
            this.btnGateSouth.Name = "btnGateSouth";
            this.btnGateSouth.Size = new System.Drawing.Size(10, 25);
            this.btnGateSouth.TabIndex = 50;
            this.btnGateSouth.Tag = "South";
            this.btnGateSouth.UseVisualStyleBackColor = false;
            this.btnGateSouth.Click += new System.EventHandler(this.btnGate_Click);
            // 
            // btnGateWest
            // 
            this.btnGateWest.BackColor = System.Drawing.Color.Maroon;
            this.btnGateWest.Location = new System.Drawing.Point(62, 253);
            this.btnGateWest.Margin = new System.Windows.Forms.Padding(0);
            this.btnGateWest.Name = "btnGateWest";
            this.btnGateWest.Size = new System.Drawing.Size(30, 10);
            this.btnGateWest.TabIndex = 49;
            this.btnGateWest.Tag = "West";
            this.btnGateWest.UseVisualStyleBackColor = false;
            this.btnGateWest.Click += new System.EventHandler(this.btnGate_Click);
            // 
            // btnGateNorth
            // 
            this.btnGateNorth.BackColor = System.Drawing.Color.Maroon;
            this.btnGateNorth.Location = new System.Drawing.Point(95, 224);
            this.btnGateNorth.Margin = new System.Windows.Forms.Padding(0);
            this.btnGateNorth.Name = "btnGateNorth";
            this.btnGateNorth.Size = new System.Drawing.Size(10, 25);
            this.btnGateNorth.TabIndex = 48;
            this.btnGateNorth.Tag = "North";
            this.btnGateNorth.UseVisualStyleBackColor = false;
            this.btnGateNorth.Click += new System.EventHandler(this.btnGate_Click);
            // 
            // btnGateEast
            // 
            this.btnGateEast.BackColor = System.Drawing.Color.Maroon;
            this.btnGateEast.Location = new System.Drawing.Point(108, 253);
            this.btnGateEast.Margin = new System.Windows.Forms.Padding(0);
            this.btnGateEast.Name = "btnGateEast";
            this.btnGateEast.Size = new System.Drawing.Size(30, 10);
            this.btnGateEast.TabIndex = 45;
            this.btnGateEast.Tag = "East";
            this.btnGateEast.UseVisualStyleBackColor = false;
            this.btnGateEast.Click += new System.EventHandler(this.btnGate_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Enabled = false;
            this.checkBox1.Location = new System.Drawing.Point(93, 251);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(15, 14);
            this.checkBox1.TabIndex = 44;
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(255, 274);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 43;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // cbMaelstrom
            // 
            this.cbMaelstrom.AutoSize = true;
            this.cbMaelstrom.Location = new System.Drawing.Point(401, 239);
            this.cbMaelstrom.Name = "cbMaelstrom";
            this.cbMaelstrom.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cbMaelstrom.Size = new System.Drawing.Size(15, 14);
            this.cbMaelstrom.TabIndex = 42;
            this.cbMaelstrom.UseVisualStyleBackColor = true;
            // 
            // lblMaelstromStart
            // 
            this.lblMaelstromStart.AutoSize = true;
            this.lblMaelstromStart.Location = new System.Drawing.Point(238, 240);
            this.lblMaelstromStart.Name = "lblMaelstromStart";
            this.lblMaelstromStart.Size = new System.Drawing.Size(129, 13);
            this.lblMaelstromStart.TabIndex = 41;
            this.lblMaelstromStart.Text = "Maelstrom Portal Location";
            // 
            // lblStartSec
            // 
            this.lblStartSec.AutoSize = true;
            this.lblStartSec.Location = new System.Drawing.Point(238, 214);
            this.lblStartSec.Name = "lblStartSec";
            this.lblStartSec.Size = new System.Drawing.Size(97, 13);
            this.lblStartSec.TabIndex = 40;
            this.lblStartSec.Text = "Game Start Sector:";
            // 
            // cbStartSector
            // 
            this.cbStartSector.AutoSize = true;
            this.cbStartSector.Location = new System.Drawing.Point(401, 213);
            this.cbStartSector.Name = "cbStartSector";
            this.cbStartSector.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cbStartSector.Size = new System.Drawing.Size(15, 14);
            this.cbStartSector.TabIndex = 39;
            this.cbStartSector.UseVisualStyleBackColor = true;
            // 
            // lblSystemName
            // 
            this.lblSystemName.Font = new System.Drawing.Font("Georgia", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSystemName.Location = new System.Drawing.Point(118, 5);
            this.lblSystemName.Name = "lblSystemName";
            this.lblSystemName.Size = new System.Drawing.Size(239, 13);
            this.lblSystemName.TabIndex = 39;
            this.lblSystemName.Text = "Owner:";
            this.lblSystemName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // SectorEdit
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.AntiqueWhite;
            this.ClientSize = new System.Drawing.Size(471, 346);
            this.Controls.Add(this.lblSystemName);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SectorEdit";
            this.Text = "Sector Edit -";
            ((System.ComponentModel.ISupportInitialize)(this.nudAbnormalSignals)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudResearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSupport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudManpower)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSystemSize)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label lbsSecId;
        private System.Windows.Forms.Label lblDejOwner;
        private System.Windows.Forms.Label lblCompany;
        private System.Windows.Forms.Label lblCons;
        private System.Windows.Forms.Label lblProd;
        private System.Windows.Forms.Label lblManpower;
        private System.Windows.Forms.Label lblSupport;
        private System.Windows.Forms.Label lblResearch;
        private System.Windows.Forms.Label lblTerranMem;
        private System.Windows.Forms.Label lblAbbySigs;
        private System.Windows.Forms.Label lblSectorId;
        private System.Windows.Forms.ComboBox cbDejureOwner;
        private System.Windows.Forms.NumericUpDown nudAbnormalSignals;
        private System.Windows.Forms.CheckBox cbTerranMemory;
        private System.Windows.Forms.NumericUpDown nudResearch;
        private System.Windows.Forms.NumericUpDown nudSupport;
        private System.Windows.Forms.NumericUpDown nudManpower;
        private System.Windows.Forms.ComboBox cbProduction;
        private System.Windows.Forms.ComboBox cbConsumption;
        private System.Windows.Forms.ComboBox cbCompany;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ComboBox cbOwner;
        private System.Windows.Forms.Label lblOwner;
        private System.Windows.Forms.NumericUpDown nudSystemSize;
        private System.Windows.Forms.Label lblSysSize;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblSystemName;
        private System.Windows.Forms.Label lblStartSec;
        private System.Windows.Forms.CheckBox cbStartSector;
        private System.Windows.Forms.CheckBox cbMaelstrom;
        private System.Windows.Forms.Label lblMaelstromStart;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnGateEast;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Button btnGateSouth;
        private System.Windows.Forms.Button btnGateWest;
        private System.Windows.Forms.Button btnGateNorth;
        private System.Windows.Forms.Label lblPlanets;
        private System.Windows.Forms.Label lblPlanetCount;
        private System.Windows.Forms.Label lblSunLevel;
        private System.Windows.Forms.ComboBox cbSunLevel;
        private System.Windows.Forms.Button btnChangeMusicFile;
        private System.Windows.Forms.Label lblMusicFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ToolTip tipSectorEdit;
    }
}