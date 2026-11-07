// Generates race-wide profiles from shipped defaults, then writes game overrides.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Azimuth
{
    public partial class WeaponsEditor : Form
    {
        private static readonly Random Random = new Random();
        public string MayhemFolder;

        private readonly Dictionary<string, int> races = new Dictionary<string, int> {
            { "Argon", 1 }, { "Boron", 2 }, { "Split", 3 },
            { "Paranid", 4 }, { "Teladi", 5 }, { "Terran", 18 }
        };

        public WeaponsEditor()
        {
            InitializeComponent();
            X3Utils.SetControlChildrenText(this);
            Text = X3Utils.Ui("WeaponsEditor.Title");
            Icon = new System.Drawing.Icon(typeof(WeaponsEditor), "ShipsWeapons.ico");
        }

        private void WeaponsEditor_Load(object sender, EventArgs e)
        {
            cbRace.DisplayMember = "Key";
            cbRace.ValueMember = "Value";
            cbRace.DataSource = races.Select(item => new KeyValuePair<string, int>(
                X3Utils.GetLocalizedText("{" + item.Key + "}") ?? item.Key, item.Value)).ToList();
            UpdateAction();
        }

        private void UpdateAction()
        {
            // These captions show effective limits; the underlying selection still
            // feeds 20/30 into the profile generator, which halves weapon rolls.
            rbTwentypercent.Text = X3Utils.Ui("Randomizer.PercentLimit", rbRandomizeAllWeapons.Checked ? 10 : 20);
            rbThirtypercent.Text = X3Utils.Ui("Randomizer.PercentLimit", rbRandomizeAllWeapons.Checked ? 15 : 30);
            cbRace.Enabled = label1.Enabled = rbAllShipStatsForRace.Checked;
            lblNote.Text = X3Utils.Ui(rbRandomizeAllWeapons.Checked ? "Randomizer.WeaponsNote" :
                rbAllShipStatsForRace.Checked ? "Randomizer.RaceNote" : "Randomizer.ShipsNote");
        }

        private void Action_CheckedChanged(object sender, EventArgs e) { UpdateAction(); }

        private static TypeTable ReadDefaults(string name, int minimumFields)
        {
            // Stable application directory: file dialogs can change the working directory.
            return new TypeTable(name, File.ReadAllLines(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, name)), minimumFields);
        }

        private void btnDoIt_Click(object sender, EventArgs e)
        {
            btnDoIt.Enabled = false;
            try
            {
                int range = rbTwentypercent.Checked ? 20 : 30;
                int count;
                string[] output;
                string name;
                string[] laserOutput = null;
                if (rbRandomizeAllWeapons.Checked)
                {
                    name = "TBullets.txt";
                    output = WeaponRandomizer.Randomize(ReadDefaults(name, 50),
                        ReadDefaults("TLaser.txt", 24), Random, range, out count, out laserOutput);
                }
                else
                {
                    name = "TShips.txt";
                    int selectedRace = rbAllShipStatsForRace.Checked ? (int)cbRace.SelectedValue : 0;
                    string path = Path.Combine(MayhemFolder, name);
                    TypeTable existing = selectedRace != 0 && File.Exists(path)
                        ? new TypeTable(name, File.ReadAllLines(path), 60) : null;
                    output = ShipRandomizer.Randomize(ReadDefaults(name, 60),
                        ShipRandomizer.ShieldCapacities(ReadDefaults("TShields.txt", 22)),
                        Random, range, selectedRace, existing, out count);
                }
                Directory.CreateDirectory(MayhemFolder);
                if (laserOutput != null)
                {
                    TypeOverrideFile.WriteBatch(new[] { Path.Combine(MayhemFolder, name),
                        Path.Combine(MayhemFolder, "TLaser.txt") }, new[] { output, laserOutput });
                    name = "TBullets.txt + TLaser.txt";
                }
                else TypeOverrideFile.Write(Path.Combine(MayhemFolder, name), output);
                MessageBox.Show(this, X3Utils.Ui("Randomizer.Success", count, name), Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, X3Utils.Ui("Randomizer.Failed", ex.Message), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btnDoIt.Enabled = true; }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, X3Utils.Ui("Randomizer.RestoreConfirm"), Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                != DialogResult.Yes) return;
            try
            {
                TypeOverrideFile.RemoveOverrides(MayhemFolder);
                MessageBox.Show(this, X3Utils.Ui("Randomizer.Restored"), Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, X3Utils.Ui("Randomizer.Failed", ex.Message), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e) { Close(); }

        private void btnHelpMain_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(X3Utils.HelpFile());
        }
    }
}
