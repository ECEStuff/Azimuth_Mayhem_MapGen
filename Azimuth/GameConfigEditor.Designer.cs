using System.Drawing;
using System.Windows.Forms;

namespace Azimuth
{
    partial class GameConfigEditor
    {
        private ConfigSettingsGrid grid;
        private ToolStrip bar;
        private ToolStripComboBox sections;
        private ToolStripLabel filterLabel;
        private ToolStripTextBox search;
        private ToolStripButton loadPreset;
        private ToolStripButton savePreset;
        private ToolStripButton restoreSelected;
        private ToolStripButton restoreFile;
        private ToolStripButton saveClose;
        private Panel panel;
        private Label detailTitle;
        private Label detailMeta;
        private TextBox details;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameConfigEditor));
            this.grid = new Azimuth.ConfigSettingsGrid();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Section = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Title = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Default = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Status = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bar = new System.Windows.Forms.ToolStrip();
            this.sections = new System.Windows.Forms.ToolStripComboBox();
            this.filterLabel = new System.Windows.Forms.ToolStripLabel();
            this.search = new System.Windows.Forms.ToolStripTextBox();
            this.loadPreset = new System.Windows.Forms.ToolStripButton();
            this.savePreset = new System.Windows.Forms.ToolStripButton();
            this.restoreSelected = new System.Windows.Forms.ToolStripButton();
            this.restoreFile = new System.Windows.Forms.ToolStripButton();
            this.saveClose = new System.Windows.Forms.ToolStripButton();
            this.panel = new System.Windows.Forms.Panel();
            this.details = new System.Windows.Forms.TextBox();
            this.detailMeta = new System.Windows.Forms.Label();
            this.detailTitle = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.bar.SuspendLayout();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Title,
            this.Value,
            this.Default,
            this.Section,
            this.Status,
            this.ID});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(0, 25);
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.Size = new System.Drawing.Size(900, 375);
            this.grid.TabIndex = 0;
            // 
            // ID
            // 
            this.ID.HeaderText = "ID";
            this.ID.Name = "ID";
            this.ID.ReadOnly = true;
            this.ID.Visible = false;
            this.ID.Width = 65;
            // 
            // Value
            // 
            this.Value.HeaderText = "Current";
            this.Value.Name = "Value";
            this.Value.Width = 78;
            // 
            // Section
            // 
            this.Section.HeaderText = "Section";
            this.Section.Name = "Section";
            this.Section.ReadOnly = true;
            this.Section.Width = 145;
            // 
            // Title
            // 
            this.Title.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Title.HeaderText = "Setting";
            this.Title.Name = "Title";
            this.Title.ReadOnly = true;
            // 
            // Default
            // 
            this.Default.HeaderText = "Default";
            this.Default.Name = "Default";
            this.Default.ReadOnly = true;
            this.Default.Width = 78;
            // 
            // Status
            // 
            this.Status.HeaderText = "Status";
            this.Status.Name = "Status";
            this.Status.ReadOnly = true;
            this.Status.Width = 160;
            // 
            // bar
            // 
            this.bar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.bar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sections,
            this.filterLabel,
            this.search,
            this.loadPreset,
            this.savePreset,
            this.restoreSelected,
            this.restoreFile,
            this.saveClose});
            this.bar.Location = new System.Drawing.Point(0, 0);
            this.bar.Name = "bar";
            this.bar.Size = new System.Drawing.Size(900, 25);
            this.bar.TabIndex = 2;
            // 
            // sections
            // 
            this.sections.AutoSize = false;
            this.sections.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sections.Items.AddRange(new object[] {
            "All sections"});
            this.sections.Name = "sections";
            this.sections.Size = new System.Drawing.Size(155, 21);
            // 
            // filterLabel
            // 
            this.filterLabel.Name = "filterLabel";
            this.filterLabel.Size = new System.Drawing.Size(33, 22);
            this.filterLabel.Text = "Filter";
            // 
            // search
            // 
            this.search.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.search.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.search.Name = "search";
            this.search.Size = new System.Drawing.Size(100, 25);
            // 
            // loadPreset
            // 
            this.loadPreset.Name = "loadPreset";
            this.loadPreset.Size = new System.Drawing.Size(72, 22);
            this.loadPreset.Text = "Load preset";
            // 
            // savePreset
            // 
            this.savePreset.Name = "savePreset";
            this.savePreset.Size = new System.Drawing.Size(70, 22);
            this.savePreset.Text = "Save preset";
            // 
            // restoreSelected
            // 
            this.restoreSelected.Name = "restoreSelected";
            this.restoreSelected.Size = new System.Drawing.Size(96, 22);
            this.restoreSelected.Text = "Restore selected";
            // 
            // restoreFile
            // 
            this.restoreFile.Name = "restoreFile";
            this.restoreFile.Size = new System.Drawing.Size(114, 22);
            this.restoreFile.Text = "Restore file defaults";
            // 
            // saveClose
            // 
            this.saveClose.Name = "saveClose";
            this.saveClose.Size = new System.Drawing.Size(88, 22);
            this.saveClose.Text = "Save and close";
            // 
            // panel
            // 
            this.panel.BackColor = System.Drawing.SystemColors.Window;
            this.panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel.Controls.Add(this.details);
            this.panel.Controls.Add(this.detailMeta);
            this.panel.Controls.Add(this.detailTitle);
            this.panel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel.Location = new System.Drawing.Point(0, 400);
            this.panel.Name = "panel";
            this.panel.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this.panel.Size = new System.Drawing.Size(900, 140);
            this.panel.TabIndex = 1;
            // 
            // details
            // 
            this.details.BackColor = System.Drawing.SystemColors.Window;
            this.details.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.details.Dock = System.Windows.Forms.DockStyle.Fill;
            this.details.Location = new System.Drawing.Point(10, 54);
            this.details.Multiline = true;
            this.details.Name = "details";
            this.details.ReadOnly = true;
            this.details.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.details.Size = new System.Drawing.Size(878, 76);
            this.details.TabIndex = 0;
            this.details.Text = "Station attack behavior for ships without main batteries.";
            // 
            // detailMeta
            // 
            this.detailMeta.AutoEllipsis = true;
            this.detailMeta.Dock = System.Windows.Forms.DockStyle.Top;
            this.detailMeta.ForeColor = System.Drawing.SystemColors.GrayText;
            this.detailMeta.Location = new System.Drawing.Point(10, 32);
            this.detailMeta.Name = "detailMeta";
            this.detailMeta.Size = new System.Drawing.Size(878, 22);
            this.detailMeta.TabIndex = 1;
            this.detailMeta.Text = "Combat  ·  9972 / 224";
            // 
            // detailTitle
            // 
            this.detailTitle.AutoEllipsis = true;
            this.detailTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.detailTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.detailTitle.Location = new System.Drawing.Point(10, 8);
            this.detailTitle.Name = "detailTitle";
            this.detailTitle.Size = new System.Drawing.Size(878, 24);
            this.detailTitle.TabIndex = 2;
            this.detailTitle.Text = "Station attack behavior";
            // 
            // GameConfigEditor
            // 
            this.ClientSize = new System.Drawing.Size(900, 540);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.bar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(650, 360);
            this.Name = "GameConfigEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Azimuth configuration";
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.bar.ResumeLayout(false);
            this.bar.PerformLayout();
            this.panel.ResumeLayout(false);
            this.panel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private DataGridViewTextBoxColumn Title;
        private DataGridViewTextBoxColumn Value;
        private DataGridViewTextBoxColumn Default;
        private DataGridViewTextBoxColumn Section;
        private DataGridViewTextBoxColumn Status;
        private DataGridViewTextBoxColumn ID;
    }
}
