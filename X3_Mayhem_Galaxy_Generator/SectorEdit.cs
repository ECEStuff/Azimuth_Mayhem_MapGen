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

namespace X3_Mayhem_Galaxy_Generator
{
    public partial class SectorEdit : Form
    {
        private X3Sector m_Sector;
        private bool m_StartMaelstrom;
        private bool m_InitialStartSector;

        public SectorEdit()
        {
            InitializeComponent();
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

        public void Initialize(X3Sector sector)
        {
            m_Sector = sector;
            SetupLocalization();

            string title = X3Utils.GetLocalizedText("{SectorEditFormTitle}");

            // Display the title using 0-based coordinates for consistency with in-game sector indexing.
            Text = title + $"  ({sector.x},{sector.y})";
            lblSectorId.Text = sector.SectorID;
            lblSystemName.Text = sector.SectorName;
            lblMusicFile.Text = sector.m_MusicFileName + ".mp3";


            // Set drop down list values.
            cbOwner.SelectedValue = sector.r;

            cbDejureOwner.SelectedValue = sector.SectorCreationData.DejureOwner;
            cbProduction.SelectedValue = sector.SectorCreationData.Production;
            cbConsumption.SelectedValue = sector.SectorCreationData.Consumption;
            cbCompany.SelectedValue = sector.SectorCreationData.Company;
            cbSunLevel.SelectedValue = X3Utils.PlaceDigitValue(0, sector.Sun.s);


            SetNud(nudSystemSize, sector.size);
            SetNud(nudAbnormalSignals, sector.SectorCreationData.Xenons);
            SetNud(nudResearch, sector.SectorCreationData.Research);
            SetNud(nudSupport, sector.SectorCreationData.Support);
            SetNud(nudManpower, sector.SectorCreationData.Manpower);
            cbTerranMemory.Checked = sector.SectorCreationData.Terrans == 1;

            cbMaelstrom.Checked = sector.IsMaelstromPortal;
            cbMaelstrom.Enabled = (m_Sector.r == (int)ERace.Xenon);
            if (X3Galaxy.Instance.GalaxyCreationSettings.PeacefulStart || X3Galaxy.Instance.GalaxyCreationSettings.ChaoticStart)
            {
                cbMaelstrom.Enabled = false;    // Don't let them change this.  It is always true and must never be set to false for this start.
            }
            m_StartMaelstrom = cbMaelstrom.Checked;


            if (X3Utils.RaceIsMain(sector.r))
            {
                // ZMap 1.8.5: Fixed for Xenon Defect.
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

            ColorGates();
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
            
            Dictionary<string, int> racedd = new Dictionary<string, int>
            {
                {X3Utils.GetLocalizedText("{none}"), 0 },
                {X3Utils.GetLocalizedText("{Argon}"), 1 },
                {X3Utils.GetLocalizedText("{Boron}"), 2 },
                {X3Utils.GetLocalizedText("{Split}"), 3 },
                {X3Utils.GetLocalizedText("{Paranid}"), 4 },
                {X3Utils.GetLocalizedText("{Teladi}"), 5 },
                {X3Utils.GetLocalizedText("{Xenon}"), 6 },
                {X3Utils.GetLocalizedText("{Unknown}"), 14 },
                {X3Utils.GetLocalizedText("{Terran}"), 18 }
            };

            Dictionary<string, int> productsdd = new Dictionary<string, int>
            {
                {X3Utils.GetLocalizedText("{Fish}"), 0 },
                {X3Utils.GetLocalizedText("{Fruit}"), 1 },
                {X3Utils.GetLocalizedText("{Meat}"), 2 },
                {X3Utils.GetLocalizedText("{Vegetables}"), 3 },
            };


            Dictionary<string, int> companydd = new Dictionary<string, int>
            {
                {X3Utils.GetLocalizedText("{none}"), -1 },
                {X3Utils.GetLocalizedText("{Markus}"), 0 },
                {X3Utils.GetLocalizedText("{Oceana}"), 1 },
                {X3Utils.GetLocalizedText("{Pontifex}"), 2 },
                {X3Utils.GetLocalizedText("{Thurok}"), 3 },
                {X3Utils.GetLocalizedText("{Salecrest}"), 4 },
                {X3Utils.GetLocalizedText("{Industritech}"), 5 }
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
            cbOwner.DataSource = racedd.Where(item => item.Value != 0).ToList();    // use most of the list, except for the "none" option.

            cbDejureOwner.DisplayMember = "Key";
            cbDejureOwner.ValueMember = "Value";
            cbDejureOwner.DataSource = racedd.Where(item => item.Value != 6 && item.Value != 14).ToList();    // use most of the list, except for the "Unknown and Xenon" options.

            cbProduction.DisplayMember = "Key";
            cbProduction.ValueMember = "Value";
            cbProduction.DataSource = productsdd.ToList();

            cbConsumption.DisplayMember = "Key";
            cbConsumption.ValueMember = "Value";
            cbConsumption.DataSource = productsdd.ToList();

            cbCompany.DisplayMember = "Key";
            cbCompany.ValueMember = "Value";
            cbCompany.DataSource = companydd.ToList();

            cbSunLevel.DisplayMember = "Key";
            cbSunLevel.ValueMember = "Value";
            cbSunLevel.DataSource = sunlevels.ToList();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            int owner = (int)cbOwner.SelectedValue;
            int dejowner = (int)cbDejureOwner.SelectedValue;
            int production = (int)cbProduction.SelectedValue;
            int consumption = (int)cbConsumption.SelectedValue;
            int company = (int)cbCompany.SelectedValue;

            m_Sector.Sun.SetSunLevel((int)cbSunLevel.SelectedValue);

            if (production == consumption)
            {
                string msgg = X3Utils.GetLocalizedText("{ProdConsumNotSame}");
                MessageBox.Show(msgg);
                return;
            }
            // Save the UI values back into the sector.

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
                    // ZMap 1.8.5: Xenon Defect starts use index 6.

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


            Close();
        }


        private void cbOwner_SelectedIndexChanged(object sender, EventArgs e)
        {
            int owner = (int)cbOwner.SelectedValue;

            // If changing this to a non-xenon sector, force the maelstrom option off.
            bool xenonOwner = owner == (int)ERace.Xenon;
            cbMaelstrom.Enabled = xenonOwner;
            if (!xenonOwner) cbMaelstrom.Checked = false;


            // Only allow start sector for player in race controlled space.
            bool ownerisrace = X3Utils.RaceIsMain(owner);
            cbStartSector.Enabled = ownerisrace;
            if (!ownerisrace) cbStartSector.Checked = false;

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

        /// <summary>
        /// Yes, let them orphan systems if that's what they want.
        /// </summary>
        /// <param name="direction"></param>
        /// 
        private void ToggleGate(EGateDirection direction)
        {
            // When removing an existing gate, use its actual destination.
            X3Gate existingGate = m_Sector.Gates.Find(gate => gate.s == (int)direction);
            if (existingGate != null)
            {
                X3Sector linkedSector = X3Galaxy.Instance.GetSectorAt(existingGate.gx, existingGate.gy);

                if (linkedSector != null)
                {
                    m_Sector.RemoveGates(linkedSector);
                }
                else
                {
                    // Allow removal of gates pointing to nonexistent sectors.
                    m_Sector.Gates.Remove(existingGate);
                }

                return;
            }

            // Search for the nearest sector only when adding a new gate.
            X3Sector destination = X3Galaxy.Instance.GetSectorInDirection(m_Sector, direction);
            if (destination == null)
                return;

            // Determine which gate side the destination would use.
            EGateDirection opposite;
            switch (direction)
            {
                case EGateDirection.North:
                    opposite = EGateDirection.South;
                    break;

                case EGateDirection.South:
                    opposite = EGateDirection.North;
                    break;

                case EGateDirection.East:
                    opposite = EGateDirection.West;
                    break;

                case EGateDirection.West:
                    opposite = EGateDirection.East;
                    break;

                default:
                    return;
            }

            // Do not overwrite an occupied gate side.
            X3Gate occupiedSide = destination.Gates.Find(gate => gate.s == (int)opposite && (gate.gx != m_Sector.x || gate.gy != m_Sector.y));
            if (occupiedSide != null)
            {
                MessageBox.Show(
                    $"Cannot add a {direction} gate. The destination sector's {opposite} gate is already occupied.",
                    "Gate Already In Use",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            m_Sector.AddGates(destination);
        }

        private void btnChangeMusicFile_Click(object sender, EventArgs e)
        {
            // Show File Choose Dialog, restricting the file name to the X3/soundtrack folder.
            openFileDialog1.InitialDirectory = X3Utils.GetSetting("X3Root") + "soundtrack";
            openFileDialog1.Filter = "Music Files|*.mp3";
            openFileDialog1.FileName = m_Sector.m_MusicFileName + ".mp3";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string directory = Path.GetDirectoryName(openFileDialog1.FileName);
                if (directory != openFileDialog1.InitialDirectory)
                {
                    MessageBox.Show("Please choose a file from the soundtrack folder.");
                    return;
                }
                string src = Path.GetFileName(openFileDialog1.FileName);
                string srcFile = Path.GetFileNameWithoutExtension(src);

                if (!X3Utils.IsSectorMusicTrack(srcFile))
                {
                    MessageBox.Show(
                        "This file cannot be assigned as sector music. Soundtrack IDs below 15, except 12, are reserved.",
                        "Invalid Sector Music",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                m_Sector.m_MusicFileName = srcFile;
                lblMusicFile.Text = srcFile + ".mp3";
            }
        }
    }

}
