using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Azimuth
{
    internal sealed class GameConfigEditor : Form
    {
        private readonly GameConfigFile file;
        private readonly DataGridView grid = new DataGridView();
        private readonly GameConfigFile defaults;
        private readonly TextBox details = new TextBox {
            Dock = DockStyle.Bottom, Height = 105, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, BackColor = SystemColors.Window
        };
        private readonly ToolStripButton restoreSelected = new ToolStripButton("Restore selected");
        private bool saved;

        public GameConfigEditor(string path, string page)
        {
            file = new GameConfigFile(path, page);
            defaults = new GameConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigDefaults", page + "-L044.xml"), page);
            Icon = new Icon(typeof(GameConfigEditor), "Config.ico");
            Text = "Azimuth configuration — " + path;
            ClientSize = new Size(900, 540);
            MinimumSize = new Size(650, 360);
            StartPosition = FormStartPosition.CenterParent;
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.Columns.Add("ID", "ID");
            grid.Columns.Add("Value", "Current");
            grid.Columns.Add("Section", "Section");
            grid.Columns.Add("Title", "Setting");
            grid.Columns.Add("Default", "Default");
            grid.Columns.Add("Status", "Status");
            grid.Columns["Title"].DisplayIndex = 0;
            grid.Columns["Value"].DisplayIndex = 1;
            grid.Columns["Default"].DisplayIndex = 2;
            grid.Columns["ID"].Visible = false;
            grid.Columns["Default"].ReadOnly = grid.Columns["Status"].ReadOnly = true;
            grid.Columns["Value"].Width = grid.Columns["Default"].Width = 78;
            grid.Columns["Status"].Width = 160;
            grid.MultiSelect = false;
            var baseline = defaults.Entries.ToDictionary(GameConfigFile.Id, entry => entry.Value);
            string catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigDefaults", "SettingNames.json");
            var titles = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,string>>>(File.ReadAllText(catalogPath))[page];
            grid.Columns[0].Width = 65;
            grid.Columns[2].Width = 145;
            grid.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[0].ReadOnly = grid.Columns[2].ReadOnly = grid.Columns[3].ReadOnly = true;
            var sections = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Width = 155 };
            sections.Items.Add("All sections");
            sections.SelectedIndex = 0;
            foreach (var entry in file.Entries)
            {
                string description = GameConfigFile.Description(entry);
                string id = GameConfigFile.Id(entry);
                string section = file.Sections[id];
                if (string.IsNullOrEmpty(section)) defaults.Sections.TryGetValue(id, out section);
                if (string.IsNullOrEmpty(section)) section = "General";
                string heading = section;
                section = section.Split(':')[0].Trim();
                if (!sections.Items.Contains(section)) sections.Items.Add(section);
                string title, defaultValue;
                if (!titles.TryGetValue(id, out title)) title = description.Length > 0 ? description : "Setting " + id;
                bool knownDefault = baseline.TryGetValue(id, out defaultValue);
                var row = grid.Rows[grid.Rows.Add(id, entry.Value, section, title, knownDefault ? defaultValue : "—")];
                row.Cells["Default"].Tag = defaultValue;
                row.Cells["Section"].Tag = heading;
                row.Cells["Section"].ToolTipText = heading;
                row.Tag = entry;
                row.Cells[3].ToolTipText = description;
                UpdateStatus(row);
            }
            var bar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
            bar.Items.Add(sections);
            bar.Items.Add("Filter");
            var search = new ToolStripTextBox();
            bar.Items.Add(search);
            EventHandler filter = (s, e) =>
            {
                grid.EndEdit();
                grid.CurrentCell = null;
                foreach (DataGridViewRow row in grid.Rows)
                    row.Visible = (sections.SelectedIndex == 0 || Equals(row.Cells[2].Value, sections.SelectedItem)) && new[] { Convert.ToString(row.Cells[0].Value), Convert.ToString(row.Cells[2].Value), Convert.ToString(row.Cells[3].Value), row.Cells[3].ToolTipText }.Any(term => term
                        .IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            };
            sections.SelectedIndexChanged += filter;
            search.TextChanged += filter;
            bar.Items.Add("Load preset", null, (s, e) => Run(Import));
            bar.Items.Add("Save preset", null, (s, e) => Run(Export));
            restoreSelected.Click += (s, e) =>
            {
                var row = grid.CurrentRow;
                if (row == null || row.Cells["Default"].Tag == null) return;
                grid.EndEdit();
                row.Cells["Value"].Value = row.Cells["Default"].Tag;
            };
            bar.Items.Add(restoreSelected);
            bar.Items.Add("Restore file defaults", null, (s, e) => Run(() =>
            {
                if (MessageBox.Show(this, "Restore all matching settings in " + file.Page +
                    ", including hidden rows? Changes take effect when you save.", "Restore file defaults",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    ApplyValues(defaults.Entries.ToDictionary(GameConfigFile.Id, entry => entry.Value));
            }));
            bar.Items.Add("Save and close", null, (s, e) => Run(() =>
            {
                var values = Values();
                if (IsDirty()) file.Save(values);
                saved = true;
                Close();
            }));
            Controls.Add(grid);
            Controls.Add(details);
            Controls.Add(bar);
            grid.SelectionChanged += (s, e) => UpdateDetails();
            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == grid.Columns["Value"].Index)
                {
                    UpdateStatus(grid.Rows[e.RowIndex]);
                    UpdateDetails();
                }
            };
            UpdateDetails();
            FormClosing += (s, e) =>
            {
                grid.EndEdit();
                if (!saved && IsDirty())
                    e.Cancel = MessageBox.Show(this, "Discard unsaved configuration changes?", "Close editor",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes;
            };
        }

        private static void UpdateStatus(DataGridViewRow row)
        {
            string value = Convert.ToString(row.Cells["Value"].Value);
            string baseline = row.Cells["Default"].Tag as string;
            bool edited = value != ((XElement)row.Tag).Value;
            string comparison = baseline == null ? "No reference" : value == baseline ? "Default" : "Customized";
            row.Cells["Status"].Value = edited ? comparison + " · Unsaved" : comparison;
        }

        private void UpdateDetails()
        {
            var row = grid.CurrentRow;
            restoreSelected.Enabled = row != null && row.Cells["Default"].Tag != null;
            if (row == null)
            {
                details.Clear();
                return;
            }
            string heading = Convert.ToString(row.Cells["Section"].Tag);
            details.Text = string.Join(Environment.NewLine, new[] {
                row.Cells["Title"].Value + "  (" + file.Page + " / " + row.Cells["ID"].Value + ")",
                heading, row.Cells["Title"].ToolTipText
            });
        }

        private bool IsDirty() => grid.Rows.Cast<DataGridViewRow>().Any(row =>
            Convert.ToString(row.Cells[1].Value) != ((XElement)row.Tag).Value);

        private Dictionary<string, string> Values()
        {
            grid.EndEdit();
            var values = new Dictionary<string, string>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                var entry = (XElement)row.Tag;
                string value = Convert.ToString(row.Cells[1].Value);
                file.Validate(entry, value);
                values.Add(GameConfigFile.Id(entry), value);
            }
            return values;
        }

        private void Export()
        {
            using (var dialog = new SaveFileDialog { Filter = "Azimuth preset|*.json", FileName = file.Page + "-preset.json" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var preset = new Dictionary<string, Dictionary<string, string>> { { file.Page, Values() } };
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(preset, Formatting.Indented));
            }
        }

        private void Import()
        {
            using (var dialog = new OpenFileDialog { Title = "Load Azimuth preset", Filter = "Azimuth preset|*.json" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var preset = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(dialog.FileName));
                Dictionary<string, string> values;
                if (preset == null || !preset.TryGetValue(file.Page, out values) || values == null)
                    throw new InvalidDataException("This preset does not contain page " + file.Page);
                ApplyValues(values);
            }
        }

        private void ApplyValues(Dictionary<string, string> values)
        {
            var matching = grid.Rows.Cast<DataGridViewRow>().Where(row => values.ContainsKey((string)row.Cells[0].Value)).ToList();
            if (matching.Count == 0) throw new InvalidDataException("No matching setting IDs.");
            foreach (var row in matching)
                file.Validate((XElement)row.Tag, values[(string)row.Cells[0].Value]);
            grid.EndEdit();
            foreach (var row in matching)
                row.Cells[1].Value = values[(string)row.Cells[0].Value];
        }

        private void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Configuration editor"); }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameConfigEditor));
            this.SuspendLayout();
            // 
            // GameConfigEditor
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GameConfigEditor";
            this.ResumeLayout(false);

        }
    }
}
