// Edits one installed configuration page at a time. Each row retains its original XML entry and reference default.

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
    internal sealed partial class GameConfigEditor : Form
    {
        private readonly GameConfigFile file;
        private readonly GameConfigFile defaults;
        private bool saved;

        public GameConfigEditor()
        {
            InitializeComponent();
        }

        public GameConfigEditor(string path, string page) : this()
        {
            file = new GameConfigFile(path, page);
            defaults = new GameConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigDefaults", page + "-L044.xml"), page);
            Text = X3Utils.Ui("GameConfigEditor.AzimuthConfiguration.Message", path);
            X3Utils.SetControlChildrenText(this);
            sections.Items[0] = X3Utils.Ui("Config.AllSections");
            foreach (DataGridViewColumn column in grid.Columns)
                column.HeaderText = X3Utils.Ui("Config.Column." + column.Name);
            sections.SelectedIndex = 0;
            var baseline = defaults.Entries.ToDictionary(GameConfigFile.Id, entry => entry.Value);
            string catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigDefaults", "SettingNames.json");
            var titles = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,string>>>(File.ReadAllText(catalogPath))[page];
            foreach (var entry in file.Entries)
            {
                string description = GameConfigFile.Description(entry).Replace("manoeuvre", "maneuver");
                string id = GameConfigFile.Id(entry);
                string section = file.Sections[id];
                if (string.IsNullOrEmpty(section)) defaults.Sections.TryGetValue(id, out section);
                if (string.IsNullOrEmpty(section)) section = X3Utils.Ui("GameConfigEditor.General");
                string heading = section;
                section = section.Split(':')[0].Trim();
                if (!sections.Items.Contains(section)) sections.Items.Add(section);
                string title, defaultValue;
                title = X3Utils.GetLocalizedText("Config.Setting." + page + "." + id);
                if (title == null && !titles.TryGetValue(id, out title))
                    title = description.Length > 0 ? description : X3Utils.Ui("GameConfigEditor.Setting.Message", id);
                bool knownDefault = baseline.TryGetValue(id, out defaultValue);
                var row = grid.AddSettingRow(id, entry.Value, section, title, knownDefault ? defaultValue : "—");
                row.Cells["Default"].Tag = defaultValue;
                row.Cells["Section"].Tag = heading;
                row.Cells["Section"].ToolTipText = heading;
                row.Tag = entry;
                row.Cells["Title"].ToolTipText = description;
                UpdateStatus(row);
            }
            EventHandler filter = (s, e) =>
            {
                grid.EndEdit();
                grid.CurrentCell = null;
                foreach (DataGridViewRow row in grid.Rows)
                    row.Visible = (sections.SelectedIndex == 0 || Equals(row.Cells["Section"].Value, sections.SelectedItem)) && new[] { Convert.ToString(row.Cells["ID"].Value), Convert.ToString(row.Cells["Section"].Value), Convert.ToString(row.Cells["Title"].Value), row.Cells["Title"].ToolTipText }.Any(term => term
                        .IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            };
            sections.SelectedIndexChanged += filter;
            search.TextChanged += filter;
            loadPreset.Click += (s, e) => Run(Import);
            savePreset.Click += (s, e) => Run(Export);
            restoreSelected.Click += (s, e) =>
            {
                var row = grid.CurrentRow;
                if (row == null || row.Cells["Default"].Tag == null) return;
                grid.EndEdit();
                row.Cells["Value"].Value = row.Cells["Default"].Tag;
            };
            restoreFile.Click += (s, e) => Run(() =>
            {
                if (MessageBox.Show(this, X3Utils.Ui("GameConfigEditor.RestoreAllMatchingSettingsIn.Message", file.Page), X3Utils.Ui("GameConfigEditor.RestoreFileDefaults"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    ApplyValues(defaults.Entries.ToDictionary(GameConfigFile.Id, entry => entry.Value));
            });
            saveClose.Click += (s, e) => Run(() =>
            {
                var values = Values();
                if (IsDirty()) file.Save(values);
                saved = true;
                Close();
            });
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
                    e.Cancel = MessageBox.Show(this, X3Utils.Ui("GameConfigEditor.DiscardUnsavedConfigurationChanges"), X3Utils.Ui("GameConfigEditor.CloseEditor"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes;
            };
        }

        // Compare against both baselines: the reference default controls Customized; the installed XML controls Unsaved.
        private static void UpdateStatus(DataGridViewRow row)
        {
            string value = Convert.ToString(row.Cells["Value"].Value);
            string baseline = row.Cells["Default"].Tag as string;
            bool edited = value != ((XElement)row.Tag).Value;
            string comparison = baseline == null ? X3Utils.Ui("GameConfigEditor.NoReference") : value == baseline ? X3Utils.Ui("GameConfigEditor.Default") : X3Utils.Ui("GameConfigEditor.Customized");
            row.Cells["Status"].Value = edited ? comparison + X3Utils.Ui("GameConfigEditor.Unsaved") : comparison;
        }

        private void UpdateDetails()
        {
            var row = grid.CurrentRow;
            restoreSelected.Enabled = row != null && row.Cells["Default"].Tag != null;
            if (row == null)
            {
                detailTitle.Text = detailMeta.Text = string.Empty;
                details.Clear();
                return;
            }
            string heading = Convert.ToString(row.Cells["Section"].Tag);
            detailTitle.Text = Convert.ToString(row.Cells["Title"].Value);
            detailMeta.Text = row.Cells["Section"].Value + "  ·  " + file.Page + " / " + row.Cells["ID"].Value;
            int separator = heading.IndexOf(':');
            details.Text = row.Cells["Title"].ToolTipText;
            if (separator >= 0)
                details.AppendText(Environment.NewLine + Environment.NewLine + heading.Substring(separator + 1).Trim());
            details.Select(0, 0);
            details.ScrollToCaret();
        }

        private bool IsDirty() => grid.Rows.Cast<DataGridViewRow>().Any(row =>
            Convert.ToString(row.Cells["Value"].Value) != ((XElement)row.Tag).Value);

        private Dictionary<string, string> Values()
        {
            grid.EndEdit();
            var values = new Dictionary<string, string>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                var entry = (XElement)row.Tag;
                string value = Convert.ToString(row.Cells["Value"].Value);
                file.Validate(entry, value);
                values.Add(GameConfigFile.Id(entry), value);
            }
            return values;
        }

        private void Export()
        {
            using (var dialog = new SaveFileDialog { Filter = X3Utils.Ui("GameConfigEditor.AzimuthPresetJson"), FileName = file.Page + "-preset.json" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var preset = new Dictionary<string, Dictionary<string, string>> { { file.Page, Values() } };
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(preset, Formatting.Indented));
            }
        }

        private void Import()
        {
            using (var dialog = new OpenFileDialog { Title = X3Utils.Ui("GameConfigEditor.LoadAzimuthPreset"), Filter = X3Utils.Ui("GameConfigEditor.AzimuthPresetJson") })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var preset = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(dialog.FileName));
                Dictionary<string, string> values;
                if (preset == null || !preset.TryGetValue(file.Page, out values) || values == null)
                    throw new InvalidDataException(X3Utils.Ui("GameConfigEditor.ThisPresetDoesNotContainPage.Message", file.Page));
                ApplyValues(values);
            }
        }

        // Validate every matching setting before changing any row, including settings hidden by the active filter.
        private void ApplyValues(Dictionary<string, string> values)
        {
            var matching = grid.Rows.Cast<DataGridViewRow>().Where(row => values.ContainsKey((string)row.Cells["ID"].Value)).ToList();
            if (matching.Count == 0) throw new InvalidDataException(X3Utils.Ui("GameConfigEditor.NoMatchingSettingIDs"));
            foreach (var row in matching)
                file.Validate((XElement)row.Tag, values[(string)row.Cells["ID"].Value]);
            grid.EndEdit();
            foreach (var row in matching)
                row.Cells["Value"].Value = values[(string)row.Cells["ID"].Value];
        }

        private void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, X3Utils.Ui("GameConfigEditor.ConfigurationEditor")); }
        }
    }
}
