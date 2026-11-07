// Edits one existing sector and its gates. Planet count is derived from its scenery objects.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Collections.Specialized.BitVector32;

namespace Azimuth
{
    public partial class SectorEdit : Form
    {
        private X3Sector m_Sector;
        private bool m_StartMaelstrom;
        private bool m_InitialStartSector;
        private bool committed, bindingOwners;
        private bool bindingFoodChoices;
        private KeyValuePair<string, int>[] foodChoices;
        private int? attributeOwner;
        private Dictionary<X3Sector, List<X3Gate>> originalGates;
        private bool originalDirty, originalManual;
        private string originalMusic;

        public SectorEdit()
        {
            InitializeComponent();
            cbProduction.SelectedValueChanged += FoodSelectionChanged;
            cbConsumption.SelectedValueChanged += FoodSelectionChanged;
        }

        /// <summary>
        /// Sets numeric up/down control with a value, but also enforces min/max boundaries.  Cheap hack, but hey it's free.
        /// </summary>
        /// <param name="ctl"></param>
        /// <param name="value"></param>
        private void SetNud(NumericUpDown ctl, int value)
        {
            if (value < ctl.Minimum) value = (int)ctl.Minimum;      // Force boundaries.. You never know what they might have.
            if (value > ctl.Maximum) value = (int)ctl.Maximum;
            ctl.Value = value;
        }

        private static void SetStatRange(NumericUpDown control, int minimum, int maximum)
        {
            control.Minimum = minimum;
            control.Maximum = maximum;
        }

        // Populate the dialog from the selected sector after localizing captions; raw race IDs differ from dropdown positions.
        public void Initialize(X3Sector sector)
        {
            m_Sector = sector;
            var galaxy = X3Galaxy.Instance;
            originalGates = galaxy.Sectors.ToDictionary(s => s, s => s.Gates.ToList());
            originalDirty = galaxy.HasUnsavedChanges; originalManual = galaxy.HasUnsavedManualEdits;
            originalMusic = sector.m_MusicFileName;
            SetupLocalization();

            string title = X3Utils.GetLocalizedText("{SectorEditFormTitle}");

            // Display the title using 0-based coordinates for consistency with in-game sector indexing.
            Text = title + $"  ({sector.x},{sector.y})";
            lblSectorId.Text = sector.SectorID;
            lblSystemName.Text = sector.SectorName;
            lblMusicFile.Text = sector.m_MusicFileName + ".mp3";
            lblPlanetCount.Text = sector.PlanetCount.ToString();


            // Set drop down list values.
            cbOwner.SelectedValue = sector.r;

            RefreshOwnerAttributes(sector.r);
            RefreshFoodChoices(sector.SectorCreationData.Production, sector.SectorCreationData.Consumption);

            cbSunLevel.SelectedValue = X3Utils.PlaceDigitValue(0, sector.Sun.s);


            SetNud(nudSystemSize, sector.size);
            SetNud(nudAbnormalSignals, sector.SectorCreationData.Xenons);
            // The checkbox on the main form configures the next galaxy; the saved mode governs this one.
            var settings = X3Galaxy.Instance.GalaxyCreationSettings;
            if (settings.SaboteurMode)
            {
                SetStatRange(nudResearch, 32, 80);
                SetStatRange(nudSupport, 6, 12);
                SetStatRange(nudManpower, 48, 104);
            }
            else if (settings.ExtraSystemStats)
            {
                // Preserve an imported sector's legacy outlier without unlocking the old caps elsewhere.
                var data = sector.SectorCreationData;
                SetStatRange(nudResearch, Math.Min(48, data.Research), Math.Max(120, data.Research));
                SetStatRange(nudSupport, Math.Min(10, data.Support), Math.Max(18, data.Support));
                SetStatRange(nudManpower, Math.Min(72, data.Manpower), Math.Max(156, data.Manpower));
            }
            else
            {
                SetStatRange(nudResearch, 40, 100);
                SetStatRange(nudSupport, 8, 15);
                SetStatRange(nudManpower, 60, 130);
            }
            SetNud(nudResearch, sector.SectorCreationData.Research);
            SetNud(nudSupport, sector.SectorCreationData.Support);
            SetNud(nudManpower, sector.SectorCreationData.Manpower);
            cbTerranMemory.Checked = sector.SectorCreationData.Terrans == 1;

            cbMaelstrom.Checked = sector.IsMaelstromPortal;
            cbMaelstrom.Enabled = (m_Sector.r == (int)ERace.Xenon);
            m_StartMaelstrom = cbMaelstrom.Checked;


            if (X3Utils.RaceIsMain(sector.r))
            {
                // Azimuth 1.8.5: Fixed for Xenon Defect.
                int xindx;
                if (sector.r == (int)ERace.Terran)
                    xindx = 5;
                else if (sector.r == (int)ERace.Xenon)
                    xindx = 6;
                else
                    xindx = sector.r - 1;

                int x = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 0]; 
                int y = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 1];
                if (sector.x == x && sector.y == y)
                {
                    cbStartSector.Checked = true;       // This is the start sector for this race.
                    m_InitialStartSector = true;        // Track what the state of this setting was upon entry.
                }
            }

            cbMaelstrom.Enabled = (int)cbOwner.SelectedValue == (int)ERace.Xenon;
            if (new GalaxyEditRules(galaxy).IsSeed(sector)) cbDejureOwner.Enabled = false;
            ColorGates();
        }

        private void RefreshOwners()
        {
            var rules = new GalaxyEditRules(X3Galaxy.Instance);
            int previous = cbOwner.SelectedValue is int ? (int)cbOwner.SelectedValue : m_Sector.r;
            var available = new[] { 1, 2, 3, 4, 5, 6, 14, 18 }.Where(i => rules.OwnerError(m_Sector, i) == null).ToList();
            if (!available.Contains(m_Sector.r)) available.Add(m_Sector.r);
            bindingOwners = true;
            cbOwner.DataSource = available.Select(i => new KeyValuePair<string, int>(SectorOwnerDialog.OwnerName(i), i)).ToList();
            cbOwner.SelectedValue = available.Contains(previous) ? previous : m_Sector.r;
            bindingOwners = false;
            cbOwner_SelectedIndexChanged(cbOwner, EventArgs.Empty);
        }

        private void RefreshOwnerAttributes(int owner)
        {
            bool changed = attributeOwner.HasValue && attributeOwner.Value != owner;
            int dejure = cbDejureOwner.SelectedValue is int ? (int)cbDejureOwner.SelectedValue : m_Sector.SectorCreationData.DejureOwner;
            int company = cbCompany.SelectedValue is int ? (int)cbCompany.SelectedValue : m_Sector.SectorCreationData.Company;
            if (changed || owner != m_Sector.r || !SectorOwnershipRules.ValidDejure(owner, dejure)) dejure = 0;
            if (!SectorOwnershipRules.ValidCompany(owner, company)) company = -1;
            var dejures = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(X3Utils.GetLocalizedText("{none}"), 0) };
            if (SectorOwnershipRules.IsFaction(owner)) dejures.Add(new KeyValuePair<string, int>(SectorOwnerDialog.OwnerName(owner), owner));
            cbDejureOwner.DisplayMember = "Key"; cbDejureOwner.ValueMember = "Value";
            cbDejureOwner.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDejureOwner.DataSource = dejures; cbDejureOwner.SelectedValue = dejure;
            // A transfer commits with no de jure owner. It can be assigned on the next edit.
            cbDejureOwner.Enabled = SectorOwnershipRules.IsFaction(owner) && owner == m_Sector.r
                && !new GalaxyEditRules(X3Galaxy.Instance).IsSeed(m_Sector);
            string[] companyKeys = { "{Markus}", "{Oceana}", "{Pontifex}", "{Thurok}", "{Salecrest}", "{Industritech}" };
            var companies = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(X3Utils.GetLocalizedText("{none}"), -1) };
            for (int i = 0; i < companyKeys.Length; i++)
                if (SectorOwnershipRules.ValidCompany(owner, i)) companies.Add(new KeyValuePair<string, int>(X3Utils.GetLocalizedText(companyKeys[i]), i));
            cbCompany.DisplayMember = "Key"; cbCompany.ValueMember = "Value";
            cbCompany.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCompany.DataSource = companies; cbCompany.SelectedValue = company;
            cbCompany.Enabled = companies.Count > 1;
            attributeOwner = owner;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (!committed && originalGates != null)
            {
                foreach (var pair in originalGates) { pair.Key.Gates.Clear(); pair.Key.Gates.AddRange(pair.Value); }
                m_Sector.m_MusicFileName = originalMusic;
                X3Galaxy.Instance.HasUnsavedChanges = originalDirty;
                X3Galaxy.Instance.HasUnsavedManualEdits = originalManual;
            }
            base.OnFormClosed(e);
        }

        private void ColorGates()
        {
            btnGateEast.BackColor = Color.LightGray;
            btnGateNorth.BackColor = Color.LightGray;
            btnGateSouth.BackColor = Color.LightGray;
            btnGateWest.BackColor = Color.LightGray;
            foreach (X3Gate gate in m_Sector.Gates)
            {
                EGateDirection direction = (EGateDirection)gate.s;
                switch (direction)
                {
                    case EGateDirection.East:
                        btnGateEast.BackColor = Color.Maroon;
                        break;
                    case EGateDirection.West:
                        btnGateWest.BackColor = Color.Maroon;
                        break;
                    case EGateDirection.North:
                        btnGateNorth.BackColor = Color.Maroon;
                        break;
                    case EGateDirection.South:
                        btnGateSouth.BackColor = Color.Maroon;
                        break;
                }
            }
        }

        private void SetupLocalization()
        {
            X3Utils.SetControlChildrenText(this);
            string gamestartTip = X3Utils.Ui("SectorEdit.GamestartSectorTip");
            tipSectorEdit.SetToolTip(cbStartSector, gamestartTip);
            tipSectorEdit.SetToolTip(lblStartSec, gamestartTip);

            foodChoices = new[]
            {
                new KeyValuePair<string, int>(X3Utils.GetLocalizedText("{Fish}"), 0),
                new KeyValuePair<string, int>(X3Utils.GetLocalizedText("{Fruit}"), 1),
                new KeyValuePair<string, int>(X3Utils.GetLocalizedText("{Meat}"), 2),
                new KeyValuePair<string, int>(X3Utils.GetLocalizedText("{Vegetables}"), 3)
            };


            Dictionary<string, int> sunlevels = new Dictionary<string, int>
            {
                {"100%", 4 },
                {"150%", 5 },
                {"200%", 6 },
                {"250%", 7 },
                {"300%", 8 }
            };

            cbOwner.DisplayMember = "Key";
            cbOwner.ValueMember = "Value";
            cbOwner.DropDownStyle = ComboBoxStyle.DropDownList;
            RefreshOwners();

            cbDejureOwner.DisplayMember = "Key";
            cbDejureOwner.ValueMember = "Value";


            cbProduction.DisplayMember = "Key";
            cbProduction.ValueMember = "Value";

            cbConsumption.DisplayMember = "Key";
            cbConsumption.ValueMember = "Value";

            cbCompany.DisplayMember = "Key";
            cbCompany.ValueMember = "Value";


            cbSunLevel.DisplayMember = "Key";
            cbSunLevel.ValueMember = "Value";
            cbSunLevel.DataSource = sunlevels.ToList();
        }

        // Filter by stable food IDs, not localized labels or dropdown positions.
        // Rebinding triggers selection events, so suppress those until both lists agree.
        private void FoodSelectionChanged(object sender, EventArgs e)
        {
            if (bindingFoodChoices || foodChoices == null) return;
            RefreshFoodChoices(cbProduction.SelectedValue as int?, cbConsumption.SelectedValue as int?);
        }

        private void RefreshFoodChoices(int? production, int? consumption)
        {
            if (!foodChoices.Any(food => food.Value == production)) production = null;
            if (!foodChoices.Any(food => food.Value == consumption)) consumption = null;
            // For an invalid imported pair, require a consumption choice instead of
            // silently assigning another food. Nothing touches the sector until OK.
            if (production == consumption) consumption = null;
            bindingFoodChoices = true;
            try
            {
                cbProduction.DataSource = foodChoices.Where(food => food.Value != consumption).ToList();
                cbProduction.SelectedIndex = -1;
                if (production.HasValue) cbProduction.SelectedValue = production.Value;

                cbConsumption.DataSource = foodChoices.Where(food => food.Value != production).ToList();
                cbConsumption.SelectedIndex = -1;
                if (consumption.HasValue) cbConsumption.SelectedValue = consumption.Value;
            }
            finally { bindingFoodChoices = false; }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!(cbOwner.SelectedValue is int)) return;
            int owner = (int)cbOwner.SelectedValue;
            string invalidOwner = new GalaxyEditRules(X3Galaxy.Instance).OwnerError(m_Sector, owner);
            if (invalidOwner != null) { MessageBox.Show(this, X3Utils.Ui(invalidOwner), X3Utils.Ui("Edit.Invalid")); return; }
            int dejowner = owner != m_Sector.r ? 0 : (int)cbDejureOwner.SelectedValue;
            if (!(cbProduction.SelectedValue is int) || !(cbConsumption.SelectedValue is int))
            {
                MessageBox.Show(this, X3Utils.Ui("SectorEdit.SelectDifferentFoods"));
                return;
            }
            int production = (int)cbProduction.SelectedValue;
            int consumption = (int)cbConsumption.SelectedValue;
            int company = (int)cbCompany.SelectedValue;
            if (!SectorOwnershipRules.ValidDejure(owner, dejowner) || !SectorOwnershipRules.ValidCompany(owner, company))
            { MessageBox.Show(this, X3Utils.Ui("Edit.InvalidAttributes")); return; }

            if (production == consumption)
            {
                string msgg = X3Utils.GetLocalizedText("{ProdConsumNotSame}");
                MessageBox.Show(msgg);
                return;
            }
            if (m_Sector.r != owner ||
                m_Sector.SectorCreationData.DejureOwner != dejowner ||
                m_Sector.SectorCreationData.Production != production ||
                m_Sector.SectorCreationData.Consumption != consumption ||
                m_Sector.SectorCreationData.Company != company ||
                m_Sector.SectorCreationData.Xenons != (int)nudAbnormalSignals.Value ||
                m_Sector.SectorCreationData.Research != (int)nudResearch.Value ||
                m_Sector.SectorCreationData.Support != (int)nudSupport.Value ||
                m_Sector.SectorCreationData.Manpower != (int)nudManpower.Value ||
                m_Sector.size != (int)nudSystemSize.Value ||
                m_Sector.SectorCreationData.Terrans != (cbTerranMemory.Checked ? 1 : 0) ||
                X3Utils.PlaceDigitValue(0, m_Sector.Sun.s) != (int)cbSunLevel.SelectedValue ||
                (X3Utils.RaceIsMain(owner) && m_InitialStartSector != cbStartSector.Checked) ||
                m_StartMaelstrom != cbMaelstrom.Checked
                )
            {
                X3Galaxy.Instance.HasUnsavedChanges = true;
                X3Galaxy.Instance.HasUnsavedManualEdits = true;
            }

            m_Sector.Sun.SetSunLevel((int)cbSunLevel.SelectedValue);
            // Save the UI values back into the sector.

            if (m_Sector.r != owner)
            {
                var preferred = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors;
                for (int i = 0; i < preferred.GetLength(0); i++)
                    if (preferred[i, 0] == m_Sector.x && preferred[i, 1] == m_Sector.y) { preferred[i, 0] = -1; preferred[i, 1] = -1; }
                m_InitialStartSector = false;
            }
            m_Sector.r = owner;
            m_Sector.SectorCreationData.DejureOwner = dejowner;
            m_Sector.SectorCreationData.Production = production;
            m_Sector.SectorCreationData.Consumption = consumption;
            m_Sector.SectorCreationData.Company = company;
            m_Sector.SectorCreationData.Xenons = (int)nudAbnormalSignals.Value;
            m_Sector.SectorCreationData.Research = (int)nudResearch.Value;
            m_Sector.SectorCreationData.Support = (int)nudSupport.Value;
            m_Sector.SectorCreationData.Manpower = (int)nudManpower.Value;
            m_Sector.size = (int)nudSystemSize.Value;
            m_Sector.SectorCreationData.Terrans = cbTerranMemory.Checked ? 1 : 0;


            // Ignore anyone trying to set non-race startup location.
            if (X3Utils.RaceIsMain(m_Sector.r))
            {
                // Only save over the Creation Setting if they are changing it from false to true, or each time you just look at a sector and click ok, it will
                // continually override the setting to false.
                if (m_InitialStartSector != cbStartSector.Checked)
                {
                    int x = cbStartSector.Checked ? m_Sector.x : -1;
                    int y = cbStartSector.Checked ? m_Sector.y : -1;
                    // for the currently selected owner, set this as their startup location.
                    // Azimuth 1.8.5: Xenon Defect starts use index 6.

                    int xindx;
                    if (m_Sector.r == (int)ERace.Terran)
                        xindx = 5;
                    else if (m_Sector.r == (int)ERace.Xenon)
                        xindx = 6;
                    else
                        xindx = m_Sector.r - 1;

                    X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 0] = x;
                    X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 1] = y;
                }
            }

            if (cbMaelstrom.Checked != m_StartMaelstrom)
            {
                // If new value is on
                if (cbMaelstrom.Checked)
                {
                    X3Galaxy.Instance.Sectors.ForEach(item => item.IsMaelstromPortal = false);
                }
                m_Sector.IsMaelstromPortal = cbMaelstrom.Checked;
            }


            committed = true;
            Close();
        }


        private void cbOwner_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (bindingOwners || !(cbOwner.SelectedValue is int)) return;
            int owner = (int)cbOwner.SelectedValue;
            RefreshOwnerAttributes(owner);

            // If changing this to a non-xenon sector, force the maelstrom option off.
            bool xenonOwner = owner == (int)ERace.Xenon;
            cbMaelstrom.Enabled = (int)cbOwner.SelectedValue == (int)ERace.Xenon;
            if (!xenonOwner) cbMaelstrom.Checked = false;


            // Allow start sector for player only in race controlled space.
            bool ownerisrace = X3Utils.RaceIsMain(owner);
            cbStartSector.Enabled = ownerisrace;
            if (!ownerisrace) 
                cbStartSector.Checked = false;
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnGate_Click(object sender, EventArgs e)
        {
            switch((sender as Button).Tag)
            {
                case "North":
                    ToggleGate(EGateDirection.North);
                    break;
                case "South":
                    ToggleGate(EGateDirection.South);
                    break;
                case "East":
                    ToggleGate(EGateDirection.East);
                    break;
                case "West":
                    ToggleGate(EGateDirection.West);
                    break;
            }
            ColorGates();
        }

        private void ToggleGate(EGateDirection direction)
        {
            var galaxy = X3Galaxy.Instance;
            var rules = new GalaxyEditRules(galaxy);
            var graph = rules.Graph.Select(v => v.ToList()).ToArray();
            int source = rules.Sectors.IndexOf(m_Sector);
            var existing = m_Sector.Gates.Find(g => g.s == (int)direction);
            X3Sector destination = existing == null ? galaxy.GetSectorInDirection(m_Sector, direction) : galaxy.GetSectorAt(existing.gx, existing.gy);
            if (destination == null) { MessageBox.Show(this, X3Utils.Ui("Edit.Gates")); return; }
            int dest = rules.Sectors.IndexOf(destination);
            if (existing != null) { graph[source].Remove(dest); graph[dest].Remove(source); }
            else
            {
                var opposite = GalaxyEditRules.Direction(destination.x, destination.y, m_Sector.x, m_Sector.y);
                if (destination.Gates.Any(g => g.s == (int)opposite))
                { MessageBox.Show(this, X3Utils.Ui("SectorEdit.GateAlreadyInUse")); return; }
                graph[source].Add(dest); graph[dest].Add(source);
            }
            string reason = rules.GraphError(graph.Select(v => v.ToArray()).ToArray());
            if (reason != null) { MessageBox.Show(this, X3Utils.Ui(reason), X3Utils.Ui("Edit.Invalid")); return; }
            if (existing != null) m_Sector.RemoveGates(destination); else m_Sector.AddGates(destination);
            galaxy.HasUnsavedChanges = galaxy.HasUnsavedManualEdits = true;
            RefreshOwners();
        }

        private void btnChangeMusicFile_Click(object sender, EventArgs e)
        {
            // Show File Choose Dialog, restricting the file name to the X3/soundtrack folder.
            openFileDialog1.InitialDirectory = X3Utils.GetSetting("X3Root") + "soundtrack";
            openFileDialog1.Filter = X3Utils.Ui("SectorEdit.MusicFilesMp");
            openFileDialog1.FileName = m_Sector.m_MusicFileName + ".mp3";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string directory = Path.GetDirectoryName(openFileDialog1.FileName);
                if (directory != openFileDialog1.InitialDirectory)
                {
                    MessageBox.Show(X3Utils.Ui("SectorEdit.PleaseChooseAFileFromTheSoundtrack"));
                    return;
                }
                string src = Path.GetFileName(openFileDialog1.FileName);
                string srcFile = Path.GetFileNameWithoutExtension(src);

                if (!X3Utils.IsSectorMusicTrack(srcFile))
                {
                    MessageBox.Show(
                        X3Utils.Ui("SectorEdit.ThisFileCannotBeAssignedAsSector"),
                        X3Utils.Ui("SectorEdit.InvalidSectorMusic"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (m_Sector.m_MusicFileName != srcFile)
                {
                    X3Galaxy.Instance.HasUnsavedChanges = true;
                    X3Galaxy.Instance.HasUnsavedManualEdits = true;
                }
                    
                m_Sector.m_MusicFileName = srcFile;
                lblMusicFile.Text = srcFile + ".mp3";
            }
        }
    }

}
