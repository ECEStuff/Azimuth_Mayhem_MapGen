// Orchestrates map generation, interaction and preview. Generation settings are captured separately from display toggles.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Forms.VisualStyles;

namespace Azimuth
{
    //public delegate void Message(string msg);
    public delegate void SettingsUpdated();


    public partial class Form1 : Form
    {
        Generator m_Generator = new Generator();
        private string m_CurSaveGameFolder = string.Empty;
        private string m_CurX3RootFolder = string.Empty;
        private string m_CurX3MusicFolder = string.Empty;
        private Random m_Rand = new Random();
        private List<SystemListItem> m_MasterSystemList = new List<SystemListItem>();
        int m_ShowTerran = 0;
        int m_ShowStats = 0;
        int m_ShowNames = 0;
        // Map search is a UI-only selection, not a change to galaxy data.
        private readonly List<X3Sector> m_FindMatches = new List<X3Sector>();
        private readonly Dictionary<Rectangle, Point> m_MapCells = new Dictionary<Rectangle, Point>();
        private string m_FindQuery = string.Empty;
        private int m_FindPosition = -1;
        private X3Sector m_SelectedSearchSector;

        // All of the relevant labels in the relations group box that control relations have a tag.
        private List<Label> m_RelationsLabels;

        private X3Sector m_HoveredSector;

        private int configuredXenonPercent = GalaxySettings.MinXenonPercent;

        public Form1()
        {
            InitializeComponent();
            voidSlider.ValueChanged += UpdateVoidPreview;
            xenonSlider.ValueChanged += UpdateXenonPreview;
            rbStandardStart.CheckedChanged += GalaxyStartChanged;
            rbPeacefulStart.CheckedChanged += GalaxyStartChanged;
            rbChaoticStart.CheckedChanged += GalaxyStartChanged;
            foreach (var option in new[] { rbSmallGalaxy, rbMediumGalaxy, rbLargeGalaxy, rbSquareMap, rbRectMap })
                option.CheckedChanged += UpdateVoidPreview;
            pictureBox1.MouseClick += MapCellMouseClick;
            toolStrip1.Renderer = new ActiveToolStripRenderer();

            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;


            pictureBox1.MouseLeave += (sender, e) => SetHoveredSector(null);
            pictureBox1.MouseDown += (sender, e) => SetHoveredSector(null);

            lblWarning.Overflow = ToolStripItemOverflow.Never;
            toolStrip1.SizeChanged += (sender, e) => ResizeValidityLabel();
            ResizeValidityLabel();
            lblWarning.Text = "";
        }


        // The search box is owned by Form1.Designer.cs.
        // Match the familiar browser Find behavior: literal, live search;
        // Enter/Shift+Enter to navigate and Escape to clear.
        private void txtFindSector_TextChanged(object sender, EventArgs e)
        {
            UpdateSectorSearch();
        }

        private void btnFindNext_Click(object sender, EventArgs e)
        {
            FindNextSector();
        }

        private void btnExportPng_Click(object sender, EventArgs e)
        {
            ExportGalaxyPng();
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (txtFindSector != null && txtFindSector.Enabled)
            {
                if (keyData == (Keys.Control | Keys.F))
                {
                    txtFindSector.TextBox.Focus();
                    txtFindSector.TextBox.SelectAll();
                    return true;
                }
                if (keyData == Keys.F3)
                {
                    FindNextSector();
                    return true;
                }
                if (keyData == (Keys.Shift | Keys.F3))
                {
                    FindPreviousSector();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SectorSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (e.Shift)
                    FindPreviousSector();
                else
                    FindNextSector();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                ClearSectorSearch();
                pictureBox1.Focus();
            }
        }

        private void ResetSearchResults()
        {
            m_FindMatches.Clear();
            m_FindQuery = string.Empty;
            m_FindPosition = -1;
            m_SelectedSearchSector = null;
            lblFindStatus.Text = string.Empty;
            lblFindStatus.ToolTipText = string.Empty;
            pictureBox1.Invalidate();
        }

        private void ClearSectorSearch()
        {
            txtFindSector.Text = string.Empty;
            ResetSearchResults();
        }

        private void UpdateSectorSearch(bool force = false)
        {
            // Do not Trim(), TrimStart(), collapse whitespace or alter case.
            // "Argon " must match "Argon Prime", not "ArgonPrime".
            string query = txtFindSector.Text;
            if (!force && string.Equals(query, m_FindQuery, StringComparison.Ordinal))
                return;

            m_FindQuery = query;
            m_FindMatches.Clear();
            m_FindPosition = -1;
            m_SelectedSearchSector = null;
            lblFindStatus.Text = string.Empty;
            lblFindStatus.ToolTipText = string.Empty;

            if (query.Length != 0)
            {
                m_FindMatches.AddRange(X3Galaxy.Instance.Sectors
                    .Where(sector => !string.IsNullOrEmpty(sector.SectorName)
                        && sector.SectorName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(sector => sector.y)
                    .ThenBy(sector => sector.x));

                if (m_FindMatches.Count > 0)
                {
                    m_FindPosition = 0;
                    SetActiveSearchMatch();
                }
                else
                {
                    lblFindStatus.Text = "0 / 0";
                    lblFindStatus.ToolTipText = Localize("{Find.NoMatches}", "No matching sectors");
                }
            }

            pictureBox1.Invalidate();
        }

        private void FindNextSector()
        {
            MoveSearchSelection(1);
        }

        private void FindPreviousSector()
        {
            MoveSearchSelection(-1);
        }

        private void MoveSearchSelection(int direction)
        {
            if (m_FindMatches.Count == 0)
            {
                if (txtFindSector.Text.Length == 0)
                    txtFindSector.TextBox.Focus();
                return;
            }

            m_FindPosition = (m_FindPosition + direction + m_FindMatches.Count) % m_FindMatches.Count;
            SetActiveSearchMatch();
            pictureBox1.Invalidate();
        }

        private void SetActiveSearchMatch()
        {
            m_SelectedSearchSector = m_FindMatches[m_FindPosition];
            lblFindStatus.Text = string.Format("{0} / {1}", m_FindPosition + 1, m_FindMatches.Count);
            lblFindStatus.ToolTipText = m_SelectedSearchSector.SectorName;
        }

        // Render to a separate bitmap so export dimensions cannot change the on-screen click rectangles.
        private void ExportGalaxyPng()
        {
            if (X3Galaxy.Instance.Sectors.Count == 0)
                return;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = X3Utils.Ui("Form1.ExportGalaxyMap");
                dialog.Filter = X3Utils.Ui("Form1.PNGImagesPngPng");
                dialog.DefaultExt = "png";
                dialog.AddExtension = true;
                dialog.OverwritePrompt = true;
                dialog.FileName = "Azimuth_Galaxy.png";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    // Independent of window size: each half-grid step receives 68 pixels.
                    // Export rendering never alters on-screen sector hit-test rectangles.
                    const int cellPixels = 68;
                    int width = checked((X3Galaxy.Instance.MaxX * 2 - 1) * cellPixels + 96);
                    int height = checked((X3Galaxy.Instance.MaxY * 2 - 1) * cellPixels + 96);
                    using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(pictureBox1.BackColor);
                        RenderGalaxy(graphics, bitmap.Size, false, false);
                        bitmap.Save(dialog.FileName, ImageFormat.Png);
                    }
                    Message(X3Utils.Ui("Form1.ExportedGalaxyMap.Message", dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, X3Utils.Ui("Form1.PNGExportFailed"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Refresh();
            ResizeValidityLabel();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            m_Generator.OnMessage += Message;
            m_CurX3MusicFolder = X3Utils.GetSetting("X3Music").Trim();

            // Remember the folder, but do not automatically select Custom Music.
            // Custom Music is available only when a valid folder has been configured.
            rbCustom.Enabled = Directory.Exists(m_CurX3MusicFolder);

            if (m_CurSaveGameFolder == string.Empty)
            {
                m_CurSaveGameFolder = X3Utils.GetSetting("X3Save");
#if DEBUG
                lblX3SaveGameFolder.Text = "Done";
#else
                lblX3SaveGameFolder.Text = string.IsNullOrWhiteSpace(m_CurSaveGameFolder) ? X3Utils.Ui("Form1.SetFolder") : m_CurSaveGameFolder;
#endif
            }

            if (m_CurX3RootFolder == string.Empty)
            {
                m_CurX3RootFolder = X3Utils.GetSetting("X3Root");
#if (DEBUG)
                m_CurX3RootFolder = @"C:\Mayhem 3\";
                X3Utils.SaveSetting("X3Root", m_CurX3RootFolder);
#endif

                lblX3RootFolder.Text = string.IsNullOrWhiteSpace(m_CurX3RootFolder) ? X3Utils.Ui("Form1.SetFolder") : m_CurX3RootFolder;
            }

            UpdateActiveGalaxyName();

            // Add all the relations labels to a collection we use later.
            m_RelationsLabels = gbRelations.Controls.OfType<Label>().ToList().FindAll(item => item.Tag != null && item.Tag.ToString() != "");

            TestRequiredFoldersSet();

#if (!DEBUG)
            string firstRun = X3Utils.GetSetting("FirstRun");
            if (firstRun == "")
            {
                // Throw up warning dialog to make sure they are aware their current save games are not being saved anywhere.
                FirstRun p = new FirstRun();
                p.ShowDialog();
                X3Utils.SaveSetting("FirstRun", "Done");
            }
#endif

            // Reduce form size to screen size for smaller displays.
            Screen screen = Screen.FromControl(this);
            if (Width > screen.Bounds.Width) Width = screen.Bounds.Width;
            if (Height > screen.Bounds.Height) Height = screen.Bounds.Height;

            m_ShowTerran = GetShowSetting("ShowTerran");
            m_ShowStats = GetShowSetting("ShowStats");
            X3Utils.EnsureSetting("ShowNames", "0");
            m_ShowNames = GetShowSetting("ShowNames");
            btnShowTerran.Checked = m_ShowTerran == 1;
            btnShowSectorStats.Checked = m_ShowStats == 1;
            btnShowSectorNames.Checked = m_ShowNames == 1;

            string ver = X3Utils.GetVersion();
            this.Text = X3Utils.Ui("Form1.AzimuthVByHerobettyBasedOnZMap", ver);

            SetupLocalization();

            // Apply the initial Show Race Relations checkbox state.
            chkShowRelations_CheckedChanged(chkShowRelations, EventArgs.Empty);

            int configured;

            if (int.TryParse(X3Utils.GetSetting("EmptySpacePercent"), out configured) && configured >= 15 && configured <= 50)
                voidSlider.Value = configured;
            X3Utils.EnsureSetting("XenonPercent", "");
            if (int.TryParse(X3Utils.GetSetting("XenonPercent"), out configured))
                configuredXenonPercent = GalaxySettings.ResolveXenonPercent(configured, GalaxySettings.MinXenonPercent);
            xenonSlider.Value = configuredXenonPercent;
            UpdateXenonPreview(this, EventArgs.Empty);
            UpdateVoidPreview(this, EventArgs.Empty);
            UpdateRelationsGrid();
            EnableButtons();
        }

        private void UpdateXenonPreview(object sender, EventArgs e)
        {
            xenonSlider.Enabled = !rbPeacefulStart.Checked && !rbChaoticStart.Checked;
            xenonLabel.Text = rbPeacefulStart.Checked ? X3Utils.Ui("Form1.XenonOneSector") :
                X3Utils.Ui("Form1.XenonPercent", rbChaoticStart.Checked ? 50 : xenonSlider.Value);
        }

        private void UpdateVoidPreview(object sender, EventArgs e)
        {
            var preset = new GalaxySettings {
                MapType = rbSquareMap.Checked ? 0 : 1,
                GalaxySize = rbSmallGalaxy.Checked ? 0 : rbMediumGalaxy.Checked ? 1 : 2
            };
            Size grid = preset.PresetGridSize();
            int cells = grid.Width * grid.Height;
            int target = preset.SectorTarget(cells, voidSlider.Value);
            voidLabel.Text = string.Format(X3Utils.GetLocalizedText("{VoidPercentLabel}") ?? "Void {0}%",voidSlider.Value);
            string title = X3Utils.GetLocalizedText("gbGalaxySize") ?? "Galaxy Size";
            gbGalaxySize.Text = string.Format(X3Utils.GetLocalizedText("{GalaxySizePreview}") ?? "{0} — Preview: {1} sectors, {2} empty cells", title, target, cells - target);
        }

        private void SetupLocalization()
        {
            X3Utils.SetControlChildrenText(this);

            toolTip1.SetToolTip(btnGenerateSystems, X3Utils.GetLocalizedText("{Tips.BtnGenerateSystems}"));
            toolTip1.SetToolTip(btnGenerateGates, X3Utils.GetLocalizedText("{Tips.BtnGenerateGates}"));
            toolTip1.SetToolTip(btnGenerateStats, X3Utils.GetLocalizedText("{Tips.BtnGenerateStats}"));
            toolTip1.SetToolTip(lblMayhemRoot, X3Utils.GetLocalizedText("{Tips.BtnX3Root}"));
            toolTip1.SetToolTip(lblMayhemSave, X3Utils.GetLocalizedText("{Tips.BtnX3Save}"));
            toolTip1.SetToolTip(cbClusteredVoids, X3Utils.GetLocalizedText("{Tips.CbClusteredVoids}"));
            toolTip1.SetToolTip(cbClusteredXenon, X3Utils.GetLocalizedText("{Tips.CbClusteredXenon}"));
            string xenonTip = X3Utils.Ui("Form1.XenonPercentTip");
            toolTip1.SetToolTip(xenonSlider, xenonTip);
            toolTip1.SetToolTip(xenonLabel, xenonTip);
            toolTip1.SetToolTip(voidSlider, X3Utils.GetLocalizedText("{Tips.VoidSlider}") ?? "Target percentage of empty cells. The sector count stays within the selected galaxy size limits.");
            toolTip1.SetToolTip(cbLimitedEnclaves, X3Utils.GetLocalizedText("{Tips.CbLimitedEnclaves}"));
            toolTip1.SetToolTip(rbExtraStats, X3Utils.Ui("ExtraStats.Tip"));
            toolTip1.SetToolTip(rbSaboteurMode, X3Utils.GetLocalizedText("{Tips.CbSaboteurMode}"));
            toolTip1.SetToolTip(cbUseParticles, X3Utils.GetLocalizedText("{Tips.CbParticles}"));
            toolTip1.SetToolTip(tbFog, X3Utils.GetLocalizedText("{Tips.SlFog}"));
            toolTip1.SetToolTip(gbExpansion, X3Utils.GetLocalizedText("{Tips.GbExpansion}"));
            foreach (var option in gbExpansion.Controls.OfType<RadioButton>())
                toolTip1.SetToolTip(option, X3Utils.GetLocalizedText("{Tips.GbExpansion}"));
            toolTip1.SetToolTip(gbSystemSpread, X3Utils.GetLocalizedText("{Tips.GbSystemSpread}"));
            toolTip1.SetToolTip(gbGalaxySize, X3Utils.GetLocalizedText("{Tips.GbGalaxySize}"));
            toolTip1.SetToolTip(gbGateDensity, X3Utils.GetLocalizedText("{Tips.GbGateDensity}"));
            toolTip1.SetToolTip(gbMapType, X3Utils.GetLocalizedText("{Tips.GbMapType}"));
            toolTip1.SetToolTip(gbMusic, X3Utils.GetLocalizedText("{Tips.GbMusic}"));
            toolTip1.SetToolTip(gbRelations, X3Utils.GetLocalizedText("{Tips.GbRelations}"));
            toolTip1.SetToolTip(rbChaoticStart, X3Utils.GetLocalizedText("{Tips.cbChaotic}"));
            toolTip1.SetToolTip(rbPeacefulStart, X3Utils.GetLocalizedText("{Tips.cbPeaceful}"));

            ToolStripItem[] toolbarItems =
            {
                btnShowTerran, btnShowSectorStats, btnShowSectorNames,
                btnLaunchWeaponsEditor, btnConfig, edit9972, edit9973,
                txtFindSector, btnFindNext, btnExportPng
            };

            foreach (ToolStripItem item in toolbarItems)
            {
                string tip = X3Utils.GetLocalizedText("{Tips." + item.Name + "}");
                if (string.IsNullOrWhiteSpace(tip))
                    continue;

                item.AutoToolTip = false;
                item.ToolTipText = tip;
            }

        }

        private void UpdateActiveGalaxyName()
        {
            if (m_CurX3RootFolder != string.Empty)
            {
                lblCurGalaxyName.Text = X3Utils.GetSettingsMap(m_CurX3RootFolder + "GalaxySettings.json");
            }
            else
            {
                lblCurGalaxyName.Text = "";
            }
        }

        private void TestRequiredFoldersSet()
        {
            // If no root or save folders are set yet, disable the buttons.
            if (m_CurSaveGameFolder == "" || m_CurX3RootFolder == "")
            {
                btnGenerateSystems.Enabled = btnSaveGalaxy.Enabled = false;
            }
            else
            {
                btnGenerateSystems.Enabled = btnSaveGalaxy.Enabled = true;
            }
        }



        private void Message(string msg)
        {
            if (rtb1.InvokeRequired)
            {
                rtb1.BeginInvoke(new Action<string>(Message), msg);
                return;
            }
            rtb1.AppendText(msg + "\n");
            rtb1.SelectionStart = rtb1.TextLength;
            rtb1.SelectionLength = 0;
            rtb1.ScrollToCaret();
        }

        internal static bool ConfirmDiscard(IWin32Window owner, string action)
        {
            return !X3Galaxy.Instance.HasUnsavedChanges ||
                MessageBox.Show(owner, X3Utils.Ui("Form1.ThisGalaxyHasUnsavedChangesContinueAnd", action),
                    X3Utils.Ui("Form1.UnsavedGalaxy"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel && !ConfirmDiscard(this, X3Utils.Ui("Form1.CloseAzimuth")))
                e.Cancel = true;
        }

        private void btnGenerateGates_Click(object sender, EventArgs e)
        {
            rtb1.Clear();
            GenerateGates();
        }

        private void GenerateGates()
        {
            var settings = X3Galaxy.Instance.GalaxyCreationSettings;
            bool previousLimited = settings.LimitedEnclaves;
            var previousDensity = settings.BorderGateDensity;
            settings.LimitedEnclaves = cbLimitedEnclaves.Checked;
            settings.BorderGateDensity = rbLowGateDensity.Checked ? EGateDensity.Low : EGateDensity.High;
            try
            {
                m_Generator.GenerateGates();
                X3Galaxy.Instance.HasUnsavedChanges = true;
            }
            catch (Exception ex)
            {
                settings.LimitedEnclaves = previousLimited;
                settings.BorderGateDensity = previousDensity;
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            pictureBox1.Refresh();
            CheckMapValidity();
        }

        private List<string> LoadSectorMusicPool()
        {
            IEnumerable<string> files;
            if (rbBasicMusic.Checked || rbLitcubeMusic.Checked)
            {
                string list = rbBasicMusic.Checked ? "custom_musics.txt" : "custom_musics_lu.txt";
                files = File.ReadAllLines(Path.Combine(m_CurX3RootFolder, "mayhem_data", list));
            }
            else
            {
                string folder = rbCustom.Checked ? m_CurX3MusicFolder : Path.Combine(m_CurX3RootFolder, "soundtrack");
                files = Directory.GetFiles(folder, "*.mp3").Select(path => Path.GetFileNameWithoutExtension(path));
                if (rbCustom.Checked)
                    files = files.Select(X3Utils.Transmute);
            }

            // X3 identifies a track by number. Keep its original filename spelling for output.
            var tracks = files.Select(file => file.Trim()).Where(X3Utils.IsSectorMusicTrack)
                .GroupBy(file => int.Parse(file)).Select(group => group.First()).ToList();
            if (tracks.Count == 0)
                throw new InvalidDataException(X3Utils.Ui("Form1.NoEligibleSectorTracksInTheSelected"));
            if (rbCustom.Checked && tracks.Count < 10)
                throw new InvalidDataException(X3Utils.Ui("Form1.CustomMusicRequiresAtLeastDistinctEligible"));
            return tracks;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            List<string> musicFiles;
            List<string> backgroundFiles;
            List<List<string>> m_SunsAndPlanets = new List<List<string>>();
            List<List<string>> m_Asteroids = new List<List<string>>();
            int x, y;
            lblWarning.Text = "";

            try
            {
                musicFiles = LoadSectorMusicPool();
                backgroundFiles = new List<string>(File.ReadLines(m_CurX3RootFolder + "\\mayhem_data\\custom_backgrounds.txt"));
            }
            catch(Exception a2)
            {
                MessageBox.Show(a2.Message);
                return;
            }

          
            // Load Suns, Planets and Asteroid Templates and system name list(s).
            try
            {
                string templatefolder = m_CurX3RootFolder + "\\mayhem_data\\extracted\\"; 
                LoadTemplate(m_SunsAndPlanets, templatefolder + "sunsandplanets.txt");
                LoadTemplate(m_Asteroids, templatefolder + "asteroids.txt");
                LoadMasterSystemsLists(m_CurX3RootFolder + "\\mayhem_data\\");
            }
            catch (Exception a3)
            {
                MessageBox.Show(a3.Message);
                return;
            }

            // Check for unsaved manual edits before generating the galaxy.
            var galaxy = X3Galaxy.Instance;
            if (galaxy.HasUnsavedManualEdits &&
                MessageBox.Show(this,
                    X3Utils.Ui("Form1.GenerateANewGalaxyAndDiscardYour"),
                    X3Utils.Ui("Form1.ReplaceGalaxy"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            var previousSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<GalaxySettings>(
                Newtonsoft.Json.JsonConvert.SerializeObject(galaxy.GalaxyCreationSettings));
            bool previousDirty = galaxy.HasUnsavedChanges;
            X3Galaxy.Instance.HasUnsavedChanges = true;
            X3Galaxy.Instance.GalaxyCreationSettings.UseParticles = cbUseParticles.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ClusteredVoids = cbClusteredVoids.Checked;
            galaxy.GalaxyCreationSettings.XenonPercent = xenonSlider.Value;
            galaxy.GalaxyCreationSettings.VoidPercent = voidSlider.Value;
            galaxy.GalaxyCreationSettings.ShatteredGalaxy = voidSlider.Value >= 35;
            X3Galaxy.Instance.GalaxyCreationSettings.ClusteredXenons = cbClusteredXenon.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ArgonSpread = (int)udArgon.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.BoronSpread = (int)udBoron.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.SplitSpread = (int)udSplit.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.ParanidSpread = (int)udParanid.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.TeladiSpread = (int)udTeladi.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.TerranSpread = (int)udTerran.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.PeacefulStart = rbPeacefulStart.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ChaoticStart = rbChaoticStart.Checked;

            if (rbOrigin.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Origin;
            if (rbEarly.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Early;
            if (rbAverage.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Average;
            if (rbAdvanced.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Advanced;

            if (rbBasicMusic.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 0;
            if (rbLitcubeMusic.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 1;
            if (rbAllMusic.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 2;
            if (rbCustom.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 3;

            X3Galaxy.Instance.GalaxyCreationSettings.MapType = rbSquareMap.Checked ? 0 : 1;

            galaxy.GalaxyCreationSettings.GalaxySize = rbSmallGalaxy.Checked ? 0 : rbMediumGalaxy.Checked ? 1 : 2;
            Size dimensions = galaxy.GalaxyCreationSettings.PresetGridSize();
            x = dimensions.Width;
            y = dimensions.Height;

            galaxy.GalaxyCreationSettings.LimitedEnclaves = cbLimitedEnclaves.Checked;
            galaxy.GalaxyCreationSettings.BorderGateDensity = rbLowGateDensity.Checked ? EGateDensity.Low : EGateDensity.High;
            var pending = new X3Galaxy(x, y);
            pending.GalaxyCreationSettings = galaxy.GalaxyCreationSettings;
            pending.GalaxyCreationSettings.ExtraSystemStats = rbExtraStats.Checked;
            pending.GalaxyCreationSettings.SaboteurMode = rbSaboteurMode.Checked;
            pending.GalaxyCreationSettings.StartSectors = new GalaxySettings().StartSectors;
            // The visible galaxy is never used as the worker's scratch space.
            galaxy.GalaxyCreationSettings = previousSettings;
            galaxy.HasUnsavedChanges = previousDirty;
            var messages = new List<string>();
            using (var progress = new GenerationProgressDialog((token, report) =>
            {
                var worker = new Generator(pending) { Cancellation = token };
                worker.OnMessage += message => { messages.Add(message); report(message); };
                worker.GenerateGalaxy(x, y, musicFiles, backgroundFiles, m_SunsAndPlanets, m_Asteroids);
                token.ThrowIfCancellationRequested();
                pending.AssignSystemNamesAndSoundToGeneratedSystems(m_MasterSystemList);
                worker.GenerateSystemStats();
                token.ThrowIfCancellationRequested();
            }))
            {
                var result = progress.ShowDialog(this);
                foreach (string message in messages) Message(message);
                if (result != DialogResult.OK)
                {
                    Message(result == DialogResult.Cancel ? X3Utils.Ui("Generator.Cancelled") : X3Utils.Ui("Generator.PreviousRetained"));
                    if (progress.Failure != null) MessageBox.Show(this, progress.Failure.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    EnableButtons(); CheckMapValidity();
                    return;
                }
            }
            galaxy.Sectors.Clear(); galaxy.Sectors.AddRange(pending.Sectors);
            galaxy.GalaxyCreationSettings = pending.GalaxyCreationSettings;
            galaxy.SetSize(x, y);
            galaxy.HasUnsavedChanges = true;
            ClearSectorSearch();

            pictureBox1.Refresh();
            CheckMapValidity();
            EnableButtons();
            galaxy.HasUnsavedManualEdits = false;
        }

        // Helper function to retrieve a localized string from the dictionary, or fall back to a default value.
        private static string Localize(string key, string fallback, params object[] values)
        {
            string text = X3Utils.GetLocalizedText(key) ?? fallback;
            return values.Length == 0 ? text : string.Format(text, values);
        }

        // Validate both Renegades plot eligibility and actual gate-network integrity.
        private void CheckMapValidity()
        {
            if (X3Galaxy.Instance.Sectors.Count == 0)
            {
                lblWarning.Text = "";
                lblWarning.ToolTipText = "";
                return;
            }

            GateNetworkReport network = X3Galaxy.Instance.AnalyzeGateNetwork();
            List<string> problems = new List<string>();

            if (!X3Galaxy.HasUnknownEnclave())
                problems.Add(Localize("{Map.NoEnclave}", "No Unknown Enclave (Renegades plot)"));

            if (network.GatelessSectors > 0)
                problems.Add(Localize("{Map.Gateless}", "Gateless sectors: {0}",
                    network.GatelessSectors));

            if (network.OutsideLargestNetwork > 0)
                problems.Add(Localize("{Map.OutsideNetwork}", "Sectors outside the main network: {0}",
                    network.OutsideLargestNetwork));

            if (network.MissingDestinations > 0)
                problems.Add(Localize("{Map.MissingDestinations}", "Gates with missing destinations: {0}",
                    network.MissingDestinations));

            if (network.MissingReturnGates > 0)
                problems.Add(Localize("{Map.MissingReturns}", "Gates without return connections: {0}",
                    network.MissingReturnGates));

            if (X3Galaxy.Instance.GalaxyCreationSettings.TerritorySeeds != null)
            {
                string structural = new GalaxyEditRules(X3Galaxy.Instance).CurrentError();
                if (structural != null) problems.Add(X3Utils.Ui(structural));
            }

            if (problems.Count > 0)
            {
                lblWarning.Text = Localize("{Map.Invalid}", "INVALID: {0}", string.Join("; ", problems));
                lblWarning.ForeColor = Color.DarkRed;
            }
            else
            {
                lblWarning.Text = Localize("{Map.Valid}", "VALID: Unknown Enclave present; all sectors connected.");
                lblWarning.ForeColor = Color.DarkBlue;
            }

            // The full summary remains accessible if a narrow window clips the label.
            lblWarning.ToolTipText = lblWarning.Text;
            ResizeValidityLabel();
        }

        private void ResizeValidityLabel()
        {
            int occupiedWidth = toolStrip1.Padding.Horizontal
                + lblWarning.Margin.Horizontal + 16;

            foreach (ToolStripItem item in toolStrip1.Items)
            {
                if (item == lblWarning || !item.Available)
                    continue;

                occupiedWidth +=
                    item.GetPreferredSize(Size.Empty).Width
                    + item.Margin.Horizontal;
            }

            int availableWidth = Math.Max(
                0, toolStrip1.ClientSize.Width - occupiedWidth);

            lblWarning.Width = availableWidth;
        }


        private void UpdateSectorCount()
        {
            int count = X3Galaxy.Instance.Sectors.Count;
            int total = X3Galaxy.Instance.MaxX * X3Galaxy.Instance.MaxY;

            lblSectorCount.ToolTipText = count > 0
                ? Localize("{Tips.SectorCount}",
                    "Double-click a void to add a sector, or right-click a cell to add or remove one. Allowed: {0}–{1}.",
                    X3Galaxy.Instance.MinimumSectorCount, X3Galaxy.Instance.MaximumSectorCount)
                : string.Empty;

            lblSectorCount.Text = total > 0
                ? Localize("{Map.SectorCount}", "Sectors: {0} / {1}", count, total)
                : Localize("{Map.NoSectors}", "Sectors: 0");

            ResizeValidityLabel();
        }

        /// <summary>
        /// Retrieves a list of \mayhem_data\sector_names_stream1/2.txt 
        /// Loads them into a master system list.
        /// 
        /// These are Sector Names, followed by a Location and Length of spoken sound for that system.
        /// </summary>
        /// <param name="rootdata"></param>
        private void LoadMasterSystemsLists(string rootdata)
        {
            m_MasterSystemList.Clear();
            string stream1 = rootdata + "sector_names_stream1.txt";
            string stream2 = rootdata + "sector_names_stream2.txt";
            List<string> s1 = File.ReadAllLines(stream1).ToList();
            List<string> s2 = File.ReadAllLines(stream2).ToList();

            s1.RemoveAll(item => item.StartsWith("//"));    // 226 names in stream 1
            s2.RemoveAll(item => item.StartsWith("//"));    // 48 names in stream 2

            // Populate the master System List from our 2 templates.
            foreach (string val in s1)
            {
                string[] splt = val.Split('|');
                m_MasterSystemList.Add(new SystemListItem
                {
                    StreamID = 1,
                    Name = splt[0],
                    StreamLocation = int.Parse(splt[1]),
                    StreamLength = int.Parse(splt[2])
                });
            }
            foreach (string val in s2)
            {
                string[] splt = val.Split('|');
                m_MasterSystemList.Add(new SystemListItem
                {
                    StreamID = 2,
                    Name = splt[0],
                    StreamLocation = int.Parse(splt[1]),
                    StreamLength = int.Parse(splt[2])
                });
            }

            m_MasterSystemList.Shuffle();
        }



        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            RenderGalaxy(e.Graphics, pictureBox1.ClientSize, true, true);
            DrawSectorTooltip(e.Graphics);
        }

        private void RenderGalaxy(Graphics g, Size canvasSize, bool updateHitTestRegions, bool fitToWindow)
        {
            if (updateHitTestRegions)
                m_MapCells.Clear();
            if (X3Galaxy.Instance.Sectors.Count == 0 || canvasSize.Width <= 0 || canvasSize.Height <= 0)
                return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.SystemDefault;

            int maxx = X3Galaxy.Instance.MaxX * 2 - 1;
            int maxy = X3Galaxy.Instance.MaxY * 2 - 1;
            if (maxx <= 0 || maxy <= 0)
                return;

            float horizontalScale = 1f;
            float verticalScale = 1f;
            if (fitToWindow)
            {
                // Preserve the existing on-screen aspect behavior. The export canvas
                // already follows the galaxy's grid proportions and needs no skew.
                if (maxx < maxy)
                    horizontalScale = (float)maxx / maxy;
                else if (maxx > maxy)
                    verticalScale = (float)maxy / maxx;
            }

            int labelMarginX = 0;
            int labelMarginY = 0;
            if (m_ShowNames == 1)
            {
                int baseCellWidth = (int)((canvasSize.Width - 8) * horizontalScale) / maxx;
                int baseCellHeight = (int)((canvasSize.Height - 8) * verticalScale) / maxy;
                labelMarginX = (GetSectorLabelWidth(baseCellWidth) - baseCellWidth) / 2 + 2;
                using (Font labelFont = new Font("Tahoma", GetSectorLabelFontSize(baseCellHeight)))
                    labelMarginY = (int)Math.Ceiling(g.MeasureString("Mg", labelFont).Height) + 4;
            }

            int totalWidth = (int)((canvasSize.Width - 8 - labelMarginX * 2) * horizontalScale);
            int totalHeight = (int)((canvasSize.Height - 8 - labelMarginY) * verticalScale);
            if (totalWidth <= 0 || totalHeight <= 0)
                return;

            int cellWidth = totalWidth / maxx;
            int cellHeight = totalHeight / maxy;
            if (cellWidth <= 0 || cellHeight <= 0)
                return;

            int leftMargin = 4 + (m_ShowNames == 1
                ? labelMarginX + (totalWidth - cellWidth * maxx) / 2
                : 0);
            int topMargin = 4 + (m_ShowNames == 1
                ? labelMarginY + (totalHeight - cellHeight * maxy) / 2
                : 0);

            for (int x = 0; x < maxx; x += 2)
                for (int y = 0; y < maxy; y += 2)
                    DrawSector(g, x, y, cellWidth, cellHeight,
                        leftMargin, topMargin, updateHitTestRegions);

            DrawGateLines(g, cellWidth, cellHeight, leftMargin, topMargin);
            if (m_ShowNames == 1)
                DrawSectorNames(g, cellWidth, cellHeight, leftMargin, topMargin);

            // Search selection is UI state, never included in a saved image.
            if (updateHitTestRegions)
                DrawSearchHighlight(g, cellWidth, cellHeight, leftMargin, topMargin);
        }

        private void DrawSearchHighlight(Graphics g, int cellWidth, int cellHeight, int leftMargin, int topMargin)
        {
            if (m_FindMatches.Count == 0)
                return;

            // Mark every match discreetly, but distinguish the active result.
            // These outlines appear only on the interactive map, never in PNG exports.
            using (Pen matchPen = new Pen(Color.FromArgb(180, 240, 205, 95), 1.5f))
            {
                matchPen.DashStyle = DashStyle.Dot;
                foreach (X3Sector sector in m_FindMatches)
                {
                    if (!X3Galaxy.Instance.Sectors.Contains(sector))
                        continue;

                    Rectangle tile = new Rectangle(
                        sector.x * 2 * cellWidth + leftMargin,
                        sector.y * 2 * cellHeight + topMargin,
                        cellWidth, cellHeight);
                    g.DrawRectangle(matchPen, Rectangle.Inflate(tile, 2, 2));
                }
            }

            if (m_SelectedSearchSector == null ||
                !X3Galaxy.Instance.Sectors.Contains(m_SelectedSearchSector))
                return;

            Rectangle activeTile = new Rectangle(
                m_SelectedSearchSector.x * 2 * cellWidth + leftMargin,
                m_SelectedSearchSector.y * 2 * cellHeight + topMargin,
                cellWidth, cellHeight);

            using (Pen outer = new Pen(Color.Black, 4f))
            using (Pen inner = new Pen(Color.Gold, 2f))
            {
                outer.DashStyle = DashStyle.Dash;
                inner.DashStyle = DashStyle.Dash;
                Rectangle frame = Rectangle.Inflate(activeTile, 4, 4);
                g.DrawRectangle(outer, frame);
                g.DrawRectangle(inner, frame);
            }
        }

        private static float GetMapFontSize(int cellHeight)
        {
            // Two stacked attribute lines must share the available cell height.
            // Scale continuously instead of changing fonts at fixed thresholds.
            return Math.Max(5f, Math.Min(10.5f, (cellHeight - 2f) / 3f));
        }

        private static float GetSectorLabelFontSize(int cellHeight)
        {
            return Math.Min(11f, GetMapFontSize(cellHeight) + 0.5f);
        }

        private static int GetSectorLabelWidth(int cellWidth)
        {
            // Leave a small visual gap between labels in adjacent sector columns.
            return Math.Max(cellWidth, cellWidth * 2 - 6);
        }

        private void DrawSectorNames(Graphics g, int elementwidth, int elementheight,
            int leftMargin, int topMargin)
        {
            using (Font font = new Font(
                "Tahoma", GetSectorLabelFontSize(elementheight)))
            using (SolidBrush brush = new SolidBrush(Color.White))
            using (StringFormat format =
                new StringFormat(StringFormatFlags.NoWrap))
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                int labelHeight =
                    (int)Math.Ceiling(g.MeasureString("Mg", font).Height) + 2;

                int labelWidth = GetSectorLabelWidth(elementwidth);

                foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
                {
                    if (string.IsNullOrWhiteSpace(sector.SectorName))
                        continue;

                    // Calculate from current render geometry; exported images must not
                    // depend on the interactive PictureBox's cached hit-test bounds.
                    Rectangle tile = new Rectangle(
                        sector.x * 2 * elementwidth + leftMargin,
                        sector.y * 2 * elementheight + topMargin,
                        elementwidth, elementheight);

                    RectangleF label = new RectangleF(
                        tile.Left + (tile.Width - labelWidth) / 2f,
                        tile.Top - labelHeight - 2,
                        labelWidth,
                        labelHeight);

                    g.DrawString(
                        sector.SectorName, font, brush, label, format);
                }
            }
        }

        private void DrawSector(Graphics g, int x, int y, int elementwidth, int elementheight, int leftMargin, int topMargin, bool updateHitTestRegions)
        {
            SolidBrush backgroundBrush = null;
            int xCoordinate = x / 2;
            int yCoordinate = y / 2;

            Font ft = new Font("Tahoma", GetMapFontSize(elementheight));

            int textHeight = (int)g.MeasureString("T", ft).Height;
            int leftSide = (x * elementwidth) + leftMargin;
            int topSide = (y * elementheight) + topMargin;

            if (updateHitTestRegions)
                m_MapCells.Add(new Rectangle(leftSide, topSide, elementwidth, elementheight), new Point(xCoordinate, yCoordinate));

            // If a sector exists at these coordinates in the galaxy, draw it.
            X3Sector sector = X3Galaxy.Instance.Sectors.Find(item => item.x == xCoordinate && item.y == yCoordinate);
            if (sector != null)
            {
                ERace race = (ERace)sector.r;

#if (GLOBTESTING)
                // For testing only.
                brush = new SolidBrush(sector.GlobColor);
#else

                switch (race)
                {
                    case ERace.None:
                        break;
                    case ERace.Argon:
                        //backgroundBrush = new SolidBrush(Color.FromArgb(0, 0, 240));    // 'slightly' darker than Color.Blue.
                        backgroundBrush = new SolidBrush(Color.FromArgb(73, 89, 255));
                        break;
                    case ERace.Boron:
                        //backgroundBrush = new SolidBrush(Color.LightGreen);
                        backgroundBrush = new SolidBrush(Color.FromArgb(49, 209, 90));
                        break;
                    case ERace.Paranid:
                        //backgroundBrush = new SolidBrush(Color.BurlyWood);
                        backgroundBrush = new SolidBrush(Color.FromArgb(225, 162, 84));
                        break;
                    case ERace.Split:
                        //backgroundBrush = new SolidBrush(Color.MediumPurple);
                        backgroundBrush = new SolidBrush(Color.FromArgb(168, 85, 253));
                        break;
                    case ERace.Teladi:
                        //backgroundBrush = new SolidBrush(Color.Yellow);
                        backgroundBrush = new SolidBrush(Color.FromArgb(210, 216, 68));
                        break;
                    case ERace.Terran:
                        backgroundBrush = new SolidBrush(Color.Aquamarine);
                        break;
                    case ERace.Xenon:
                        backgroundBrush = new SolidBrush(Color.Brown);
                        break;
                    case ERace.Unknown:
                        backgroundBrush = new SolidBrush(Color.Silver);
                        break;
                }
#endif

                if (backgroundBrush != null)
                {
                    g.FillRectangle(backgroundBrush, leftSide, topSide, elementwidth, elementheight);

                    // Only the interactive map updates click/hover rectangles.
                    if (updateHitTestRegions)
                        sector.ScreenBox = new Rectangle(leftSide, topSide, elementwidth, elementheight);


                    bool isStartingSector = false;
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

                        int racestartingx = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 0];
                        int racestartingy = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 1];
                        if (racestartingx == xCoordinate && racestartingy == yCoordinate)
                        {
                            // This is a starting sector for the race.
                            isStartingSector = true;
                        }
                    }

                    // Now draw system stats

                    // Get proper color for best contrast.
                    float backgroundbrightness = backgroundBrush.Color.GetBrightness();
                    Color fontColor = backgroundbrightness > 0.48 ? Color.Black : Color.White;
                    using (SolidBrush textBrush = new SolidBrush(fontColor))
                    {
                        if (m_ShowStats == 1)
                        {
                            string mpr = sector.SectorCreationData.Manpower.ToString() + (isStartingSector ? " *" : "");
                            g.DrawString(mpr,
                                ft,
                                textBrush,
                                new PointF(leftSide, topSide));

                            g.DrawString(sector.SectorCreationData.Support.ToString(),
                                ft,
                                textBrush,
                                new PointF(leftSide, topSide + textHeight));
                        }

                        // Finder highlighting takes priority over Terran Memory.
                        bool searchHighlightVisible = updateHitTestRegions && m_FindMatches.Contains(sector);

                        if (m_ShowTerran == 1 && sector.SectorCreationData.Terrans == 1 && !searchHighlightVisible)
                        {
                            using (Pen borderPen = new Pen(Color.White, 2))
                            {
                                g.DrawRectangle(borderPen, leftSide - 2, topSide - 2, elementwidth + 4, elementheight + 4);
                            }
                        }
                    }

                    backgroundBrush.Dispose();
                }
            }

            ft.Dispose();
        }


        private void btnRootFolder_Click(object sender, EventArgs e)
        {
            // Choose a different mayhem 3 root folder.
            // Ensure AP.EXE is there.
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(folderBrowserDialog1.SelectedPath + "\\X3AP.exe"))
                {
                    m_CurX3RootFolder = folderBrowserDialog1.SelectedPath + "\\";
                    lblX3RootFolder.Text = string.IsNullOrWhiteSpace(m_CurX3RootFolder) ? X3Utils.Ui("Form1.SetFolder") : m_CurX3RootFolder;

                    // SAVE IT TO SETTINGS.
                    X3Utils.SaveSetting("X3Root", lblX3RootFolder.Text);
                    TestRequiredFoldersSet();
                }
                else
                {
                    string msgg = X3Utils.GetLocalizedText("{InvalidPath}");
                    MessageBox.Show(msgg);
                }
            }
        }

        private void btnX3SaveGameFolder_Click(object sender, EventArgs e)
        {
            // Prefer the configured save folder, otherwise use the default X3AP location.
            folderBrowserDialog1.SelectedPath = Directory.Exists(m_CurSaveGameFolder)
                ? m_CurSaveGameFolder
                : X3Utils.GetDefaultSaveGameFolder();

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!folderBrowserDialog1.SelectedPath.EndsWith("save"))
                {
                    string msgg = X3Utils.GetLocalizedText("{InvalidSaveFolder}");
                    MessageBox.Show(msgg);
                    return;
                }
                m_CurSaveGameFolder = folderBrowserDialog1.SelectedPath + "\\";

                // Strip out user name because I'm doing video's.
#if DEBUG
                lblX3SaveGameFolder.Text = "Done";
#else
                lblX3SaveGameFolder.Text = string.IsNullOrWhiteSpace(m_CurSaveGameFolder) ? X3Utils.Ui("Form1.SetFolder") : m_CurSaveGameFolder;
#endif

                X3Utils.SaveSetting("X3Save", m_CurSaveGameFolder);
                TestRequiredFoldersSet();
            }
        }

        private void DrawGateLines(Graphics g, int elementwidth, int elementheight,
            int leftMargin, int topMargin)
        {
            int linelength, startx = 0, starty = 0, endx = 0, endy = 0;
            Pen p = Pens.DarkGray;

            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                // For each gate in this system
                foreach(X3Gate gate in sector.Gates)
                {
                    p = Pens.DarkGray;
                    if (gate.IsSpecial) p = Pens.Red;

                    // type of gate.
                    switch (gate.s)
                    {
                        case 0: // North (top)
                            linelength = elementheight * (((sector.y - gate.gy) * 2) - 1) - 1;
                            starty = (sector.y * elementheight * 2) - 1 + topMargin;
                            endy = starty - linelength;
                            startx = (sector.x * elementwidth * 2) + elementwidth / 2 + leftMargin;
                            if (gate.IsSpecial) startx += 2;
                            endx = startx;
                            g.DrawLine(p, startx, starty, endx, endy);
                            break;
                        case 1: // South (bottom)
                            // no need.
                            break;
                        case 2: // West (left)
                            linelength = elementwidth * (((sector.x - gate.gx) * 2) - 1) - 1;
                            startx = (sector.x * elementwidth * 2) - 1 + leftMargin;
                            endx = startx - linelength + 1;
                            starty = (sector.y * elementheight * 2) + elementheight / 2 + topMargin;
                            if (gate.IsSpecial) starty += 2;
                            endy = starty;
                            g.DrawLine(p, startx, starty, endx, endy);
                            break;
                        case 3: // East (right)
                            // no need.
                            break;
                    }
                }
            }
        }

        private void btnSaveGalaxy_Click(object sender, EventArgs e)
        {
            // A second editor must not interleave saves, deletes, or rollback operations.
            bool ownsDialog;
            using (var gate = new System.Threading.Mutex(true, @"Local\AzimuthGalaxyPersistence", out ownsDialog))
            {
                if (!ownsDialog)
                {
                    MessageBox.Show(X3Utils.Ui("Form1.AnotherAzimuthWindowAlreadyHasSaveLoad"));
                    return;
                }
                try
                {
                    using (var p = new Persist())
                    {
                        p.OnSettingsUpdated += UpdateUIFromGalaxySettings;
                        p.Initialize();
                        p.ShowDialog(this);
                        if (p.GalaxyWasLoaded)
                        {
                            ClearSectorSearch();
                            UpdateUIFromGalaxySettings();
                            pictureBox1.Refresh();
                            CheckMapValidity();
                        }
                        else
                        {
                            UpdateActiveGalaxyName();
                        }
                    }
                }
                finally
                {
                    gate.ReleaseMutex();
                }
            }
        }

        // Changes visibility only; the saved relations are never altered.
        private void chkShowRelations_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel1.SuspendLayout();
            try
            {
                // Restore logical coordinates before moving controls in a scrolled panel.
                splitContainer1.Panel1.AutoScrollPosition = Point.Empty;
                gbRelations.Visible = chkShowRelations.Checked;
                rtb1.Top = chkShowRelations.Checked
                    ? gbRelations.Bottom + 8
                    : chkShowRelations.Bottom + 8;
                rtb1.Height = 135;
            }
            finally
            {
                splitContainer1.Panel1.ResumeLayout(true);
            }
        }

        private void UpdateGeneratedModifierSummary()
        {
            var galaxy = X3Galaxy.Instance;
            string key = "Form1.MapModifier.Empty";
            if (galaxy.Sectors.Count > 0)
            {
                var settings = galaxy.GalaxyCreationSettings;
                key = settings.SaboteurMode ? "Form1.MapModifier.Saboteur"
                    : settings.ExtraSystemStats ? "Form1.MapModifier.Extra"
                    : "Form1.MapModifier.Normal";
            }
            lblGeneratedModifier.Text = X3Utils.Ui(key);
            toolTip1.SetToolTip(lblGeneratedModifier, lblGeneratedModifier.Text);
        }

        private void EnableButtons()
        {
            UpdateGeneratedModifierSummary();
            UpdateSectorCount();
            bool hasSectors = X3Galaxy.Instance.Sectors.Count > 0;
            txtFindSector.Enabled = hasSectors;
            btnFindNext.Enabled = hasSectors;
            btnExportPng.Enabled = hasSectors;

            if (X3Galaxy.Instance.Sectors.Count == 0)
            {
                btnGenerateGates.Enabled = false;
                btnGenerateStats.Enabled = false;
                return;
            }
            else
            {
                btnGenerateGates.Enabled = true;
                btnGenerateStats.Enabled = true;
            }

        }
        /// <summary>
        /// Notification from Persist class that we have loaded a map and it's GalaxySettings.json file into our X3Galaxy.Instance.GalaxyCreationSettings object.  
        /// </summary>
        private void UpdateUIFromGalaxySettings()
        {
            EnableButtons();
            UpdateActiveGalaxyName();

            // Update our UI settings here with the galaxy settings stored by the persist stuff.. when a galaxy is loaded, it's settings are loaded.
            udArgon.Value = X3Galaxy.Instance.GalaxyCreationSettings.ArgonSpread;
            udBoron.Value = X3Galaxy.Instance.GalaxyCreationSettings.BoronSpread;
            udParanid.Value = X3Galaxy.Instance.GalaxyCreationSettings.ParanidSpread;
            udSplit.Value = X3Galaxy.Instance.GalaxyCreationSettings.SplitSpread;
            udTeladi.Value = X3Galaxy.Instance.GalaxyCreationSettings.TeladiSpread;
            udTerran.Value = X3Galaxy.Instance.GalaxyCreationSettings.TerranSpread;
            switch (X3Galaxy.Instance.GalaxyCreationSettings.MapType)
            {
                case 0:
                    rbSquareMap.Checked = true;
                    break;
                case 1:
                    rbRectMap.Checked = true;
                    break;
            }
            switch(X3Galaxy.Instance.GalaxyCreationSettings.BorderGateDensity)
            {
                case EGateDensity.Low:
                    rbLowGateDensity.Checked = true;
                    break;
                case EGateDensity.High:
                    rbHighGateDensity.Checked = true;
                    break;
            }
            cbClusteredVoids.Checked = X3Galaxy.Instance.GalaxyCreationSettings.ClusteredVoids;
            var startSettings = X3Galaxy.Instance.GalaxyCreationSettings;
            if (startSettings.PeacefulStart) 
                rbPeacefulStart.Checked = true;
            else if (startSettings.ChaoticStart) 
                rbChaoticStart.Checked = true;
            else rbStandardStart.Checked = true;
            cbClusteredXenon.Checked = rbChaoticStart.Checked || (rbStandardStart.Checked && startSettings.ClusteredXenons);
            if (startSettings.SaboteurMode) rbSaboteurMode.Checked = true;
            else if (startSettings.ExtraSystemStats) rbExtraStats.Checked = true;
            else rbNoModifier.Checked = true;
            tbFog.Value = X3Galaxy.Instance.GalaxyCreationSettings.FogLevelPercent / 25;
            switch(X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion)
            {
                case EGalaxyExpansion.Origin:
                    rbOrigin.Checked = true;
                    break;
                case EGalaxyExpansion.Early:
                    rbEarly.Checked = true;
                    break;
                case EGalaxyExpansion.Average:
                    rbAverage.Checked = true;
                    break;
                case EGalaxyExpansion.Advanced:
                    rbAdvanced.Checked = true;
                    break;
            }
            int galaxySize = X3Galaxy.Instance.GalaxyCreationSettings.GalaxySize;
            switch(galaxySize)
            {
                case 0:
                    rbSmallGalaxy.Checked = true;
                    break;
                case 1:
                    rbMediumGalaxy.Checked = true;
                    break;
                case 2:
                    rbLargeGalaxy.Checked = true;
                    break;
            }

            cbLimitedEnclaves.Checked = X3Galaxy.Instance.GalaxyCreationSettings.LimitedEnclaves;
            cbUseParticles.Checked = X3Galaxy.Instance.GalaxyCreationSettings.UseParticles;
            switch(X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection)
            {
                case 0:
                    rbBasicMusic.Checked = true;
                    break;
                case 1:
                    rbLitcubeMusic.Checked = true;
                    break;
                case 2:
                    rbAllMusic.Checked = true;
                    break;
                case 3:
                    rbCustom.Checked = true;
                    break;
            }
            xenonSlider.Value = GalaxySettings.ResolveXenonPercent(
                X3Galaxy.Instance.GalaxyCreationSettings.XenonPercent, configuredXenonPercent);
            voidSlider.Value = X3Galaxy.Instance.GalaxyCreationSettings.EffectiveVoidPercent;

            UpdateRelationsGrid();
            pictureBox1.Refresh();
        }

        /// <summary>
        /// Provided the X3 Root folder is correct, load up the text \mayhem_data\extracted\sunsandplanets.txt, which contains
        /// a shit ton (239) of templated suns and planets that mayhem 3 used.  These are arranged as groups of 1 sun and 1-x planets followed by a blank line.
        /// </summary>
        private bool LoadTemplate(List<List<string>> template, string filename)
        {
            List<string> newentry = null;


            List<string> sourcedata = File.ReadAllLines(filename).ToList();

            foreach (string src in sourcedata)
            {
                // If blank line, create a new entry in m_SunsAndPlanets.
                // If non blank, add the line to the new entry
                if (src == "")
                {
                    if (newentry != null)
                    {
                        template.Add(newentry);
                    }
                    newentry = new List<string>();
                }
                else
                {
                    if (newentry == null) throw new Exception(X3Utils.Ui("Form1.InvalidLineIn", filename));
                    newentry.Add(src);
                }
            }
            return true;
        }

        private void RelationsLabels_Click(object sender, EventArgs e)
        {
            // Look at the Tag on the label to see who's being swapped around.
            Label lbl = sender as Label;

            string[] races = lbl.Tag.ToString().Split('|');
            string source = races[0];
            string target = races[1];

            // Get enumeration value of source and target.  These are 1 greater than the index where we store them in Relations.
            ERace sourceRace, destinationRace;
            if (!Enum.TryParse(source, out sourceRace)) throw new Exception(X3Utils.Ui("Form1.UnableToParseTag", lbl.Tag.ToString()));
            if (!Enum.TryParse(target, out destinationRace)) throw new Exception(X3Utils.Ui("Form1.UnableToParseTag", lbl.Tag.ToString()));

            // create 0-based index into Relations array
            int srcIndex = (int)sourceRace - 1;
            int dstIndex = (int)destinationRace - 1;

            // Terrans.  6th element of 6x6 relations array.
            if (sourceRace == ERace.Terran)
            {
                srcIndex = 5;
            }
            if (destinationRace == ERace.Terran)
            {
                dstIndex = 5;
            }

            // Get current relationship
            int curRelatinship = X3Galaxy.Instance.GalaxyCreationSettings.Relations[srcIndex, dstIndex];

            // Set next state of relationship (dynamic = 0, perm alliance = 1, perm war = -1)
            if (++curRelatinship > 1) curRelatinship = -1;
            X3Galaxy.Instance.GalaxyCreationSettings.Relations[srcIndex, dstIndex] = curRelatinship;
            X3Galaxy.Instance.GalaxyCreationSettings.Relations[dstIndex, srcIndex] = curRelatinship;

            if (X3Galaxy.Instance.Sectors.Count > 0)
            {
                X3Galaxy.Instance.HasUnsavedChanges = true;
                X3Galaxy.Instance.HasUnsavedManualEdits = true;
            }

            // Update Relations Display
            UpdateRelationsGrid();
        }

        private void UpdateRelationsGrid()
        {
            string friend = X3Utils.GetLocalizedText("{Friend}");
            string foe = X3Utils.GetLocalizedText("{Foe}");
            string dynamic = X3Utils.GetLocalizedText("{Dynamic}");
            string race1, race2;

            for (int x=0; x<6; x++)
            {
                for (int y=0; y<6; y++)
                {
                    if (x == y) continue;       // Don't try to set relations between the same race.

                    int iRelation = X3Galaxy.Instance.GalaxyCreationSettings.Relations[x, y];

                    // Find the label having the proper tag related to this relation.
                    if (x == 5)
                    {
                        race1 = ERace.Terran.ToString();
                    }
                    else
                    {
                        race1 = ((ERace)x + 1).ToString();
                    }

                    if (y == 5)
                    {
                        race2 = ERace.Terran.ToString();
                    }
                    else
                    {
                        race2 = ((ERace)y + 1).ToString();
                    }

                    // Find label with the right tag.
                    foreach (Label lbl in m_RelationsLabels)
                    {
                        if (lbl.Tag.ToString() == race1 + "|" + race2)
                        {
                            switch(iRelation)
                            {
                                case 0:
                                    lbl.Text = dynamic;
                                    lbl.ForeColor = Color.Black;
                                    break;
                                case 1:
                                    lbl.Text = friend;
                                    lbl.ForeColor = Color.DarkBlue;
                                    break;
                                case -1:
                                    lbl.Text = foe;
                                    lbl.ForeColor = Color.DarkRed;
                                    break;
                                default:
                                    throw new Exception(X3Utils.Ui("Form1.InvalidRelationValue", iRelation));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// They want to regenerate stats.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenerateStats_Click(object sender, EventArgs e)
        {
            X3Galaxy.Instance.HasUnsavedChanges = true;
            m_Generator.GenerateSystemStats();
            pictureBox1.Refresh();
        }

        private void tbFog_Scroll(object sender, EventArgs e)
        {
            int fog = tbFog.Value * 25;
            var galaxy = X3Galaxy.Instance;
            if (galaxy.Sectors.Count > 0 && galaxy.GalaxyCreationSettings.FogLevelPercent != fog)
            {
                galaxy.HasUnsavedChanges = true;
                galaxy.HasUnsavedManualEdits = true;
            }
                
            galaxy.GalaxyCreationSettings.FogLevelPercent = fog;
        }


        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            SetHoveredSector(e.Button == MouseButtons.None ? GetSectorAtLocation(e.Location) : null);
        }

        private void SetHoveredSector(X3Sector sector)
        {
            if (sector == m_HoveredSector) return;
            m_HoveredSector = sector;
            pictureBox1.Invalidate();
        }

        private void DrawSectorTooltip(Graphics graphics)
        {
            var sector = m_HoveredSector;
            if (sector == null || !X3Galaxy.Instance.Sectors.Contains(sector)) 
                return;

            Rectangle tile = sector.ScreenBox;
            if (!tile.Contains(pictureBox1.PointToClient(Cursor.Position))) 
                return;

            string text = sector.SectorName;
            if (m_ShowStats == 1)
            {
                var stats = sector.SectorCreationData;
                text += "\n" + Localize("{Tips.SectorStats}",
                    "Population: {0}\nStation Support: {1}\nResearch Rate: {2}%",
                    stats.Manpower, stats.Support, stats.Research);
            }

            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            Font font = SystemFonts.StatusFont;
            Size size = TextRenderer.MeasureText(graphics, text, font, Size.Empty, flags);
            int pad = Math.Max(3, (int)Math.Ceiling(graphics.DpiX / 24f));
            size.Width += 2 * pad;
            size.Height += 2 * pad;
            Rectangle canvas = pictureBox1.ClientRectangle;
            if (size.Width > canvas.Width || size.Height > canvas.Height) 
                return;
            int x = Math.Min(Math.Max(0, tile.Left), canvas.Width - size.Width);
            int y = Math.Min(Math.Max(0, tile.Top), canvas.Height - size.Height);
            if (tile.Right + pad + size.Width <= canvas.Right) x = tile.Right + pad;
            else if (tile.Left >= size.Width + pad) x = tile.Left - pad - size.Width;
            else if (tile.Bottom + pad + size.Height <= canvas.Bottom) y = tile.Bottom + pad;
            else if (tile.Top >= size.Height + pad) y = tile.Top - pad - size.Height;
            else return;

            // Paint on the map so the hint can never intercept a click on any sector.
            var box = new Rectangle(x, y, size.Width, size.Height);
            graphics.FillRectangle(SystemBrushes.Info, box);
            ControlPaint.DrawBorder(graphics, box, SystemColors.InfoText, ButtonBorderStyle.Solid);
            box.Inflate(-pad, -pad);
            TextRenderer.DrawText(graphics, text, font, box, SystemColors.InfoText, flags);
        }

        private void btnShowTerran_Click(object sender, EventArgs e)
        {
            m_ShowTerran = GetShowSetting("ShowTerran");
            m_ShowTerran = m_ShowTerran == 0 ? 1 : 0;   // toggle it.

            btnShowTerran.Checked = m_ShowTerran == 1;
            X3Utils.SaveSetting("ShowTerran", m_ShowTerran.ToString());
            pictureBox1.Refresh();
        }

        private void btnShowSectorStats_Click(object sender, EventArgs e)
        {
            m_ShowStats = GetShowSetting("ShowStats");
            m_ShowStats = m_ShowStats == 0 ? 1 : 0;   // toggle it.

            btnShowSectorStats.Checked = m_ShowStats == 1;
            X3Utils.SaveSetting("ShowStats", m_ShowStats.ToString());
            pictureBox1.Refresh();
        }

        private void btnShowSectorNames_Click(object sender, EventArgs e)
        {
            m_ShowNames = m_ShowNames == 0 ? 1 : 0;
            btnShowSectorNames.Checked = m_ShowNames == 1;
            X3Utils.SaveSetting("ShowNames", m_ShowNames.ToString());
            pictureBox1.Refresh();
        }

        private int GetShowSetting(string settingName)
        {
            string set = X3Utils.GetSetting(settingName);
            if (set == "")
            {
                return 0;
            }
            return int.Parse(set);
        }

        private X3Sector GetSectorAtLocation(Point p)
        {
            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                if (sector.ScreenBox.Contains(p))
                {
                    return sector;
                }
            }
            return null;
        }

        private void edit9972_Click(object sender, EventArgs e)
        {
            OpenConfigEditor("9972");
        }

        private void edit9973_Click(object sender, EventArgs e)
        {
            OpenConfigEditor("9973");
        }

        private void OpenConfigEditor(string page)
        {
            string language = X3Utils.GetLocalizedText("language_id_file");
            string path = Path.Combine(m_CurX3RootFolder, "addon", "t", page + "-L0" + language + ".xml");
            try
            {
                using (var editor = new GameConfigEditor(path, page))
                    editor.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, X3Utils.Ui("Form1.CouldNotOpenConfiguration"));
            }
        }

        private void MapCellMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            foreach (var cell in m_MapCells)
            {
                if (!cell.Key.Contains(e.Location)) continue;
                EditMapCell(cell.Value);
                return;
            }
        }

        private void EditMapCell(Point cell)
        {
            var galaxy = X3Galaxy.Instance;
            var rules = new GalaxyEditRules(galaxy);
            var sector = galaxy.GetSectorAt(cell.X, cell.Y);
            bool adding = sector == null;
            if (adding)
            {
                var choices = rules.AdditionChoices(cell.X, cell.Y);
                if (choices.Count == 0)
                {
                    string reason = !rules.CountAllowed(galaxy.Sectors.Count + 1) ? "Edit.Count" : rules.CurrentError() ?? "Edit.NoAddition";
                    MessageBox.Show(this, X3Utils.Ui(reason), X3Utils.Ui("Edit.Invalid")); return;
                }
                using (var chooser = new SectorOwnerDialog(choices.Keys))
                {
                    if (chooser.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        sector = CreateMapSector(cell);
                        sector.MarkSectorRace((ERace)chooser.SelectedOwner);
                        // Assets are ready; apply the validated proposal as one operation.
                        galaxy.Sectors.Add(sector);
                        foreach (int neighbor in choices[chooser.SelectedOwner]) sector.AddGates(rules.Sectors[neighbor]);
                        string reason = new GalaxyEditRules(galaxy).CurrentError();
                        if (reason != null) throw new InvalidOperationException(X3Utils.Ui(reason));
                    }
                    catch (Exception ex)
                    {
                        if (sector != null)
                        {
                            foreach (var other in galaxy.Sectors) other.Gates.RemoveAll(g => g.gx == cell.X && g.gy == cell.Y);
                            galaxy.Sectors.Remove(sector);
                        }
                        MessageBox.Show(this, ex.Message, X3Utils.Ui("Form1.CouldNotAddSector")); return;
                    }
                }
            }
            else
            {
                string reason = rules.RemovalError(sector);
                if (reason != null) { MessageBox.Show(this, X3Utils.Ui(reason), X3Utils.Ui("Edit.Invalid")); return; }
                if (MessageBox.Show(this, X3Utils.Ui("Form1.RemoveItsGatesAndAnyPreferredStart", sector.SectorName),
                    X3Utils.Ui("Form1.EditGalaxy"), MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                foreach (var other in galaxy.Sectors) other.Gates.RemoveAll(g => g.gx == cell.X && g.gy == cell.Y);
                var starts = galaxy.GalaxyCreationSettings.StartSectors;
                for (int i = 0; i < starts.GetLength(0); i++)
                    if (starts[i, 0] == cell.X && starts[i, 1] == cell.Y) { starts[i, 0] = -1; starts[i, 1] = -1; }
                galaxy.Sectors.Remove(sector);
            }
            galaxy.HasUnsavedManualEdits = true;
            galaxy.HasUnsavedChanges = true;
            UpdateSectorSearch(force: true); UpdateSectorCount(); CheckMapValidity(); pictureBox1.Refresh();
            if (adding) Message(X3Utils.Ui("Form1.SectorAddedDoubleClickItToEdit"));
        }

        // Allocate the entire sector before adding it to the map; a missing name or template leaves the old map intact.
        private X3Sector CreateMapSector(Point cell)
        {
            var galaxy = X3Galaxy.Instance;
            string data = Path.Combine(m_CurX3RootFolder, "mayhem_data");
            LoadMasterSystemsLists(data + Path.DirectorySeparatorChar);
            var name = m_MasterSystemList.FirstOrDefault(candidate => !galaxy.Sectors.Any(existingSector => existingSector.SectorName == candidate.Name));
            if (name == null)
            {
                throw new InvalidDataException(X3Utils.Ui("Form1.NoUnusedSectorNameAndVoiceEntry"));
            }

            var tracks = LoadSectorMusicPool();
            var counts = tracks.ToDictionary(track => int.Parse(track), track => 0);
            foreach (var existing in galaxy.Sectors)
            {
                int id;
                if (int.TryParse(existing.m_MusicFileName, out id) && counts.ContainsKey(id))
                    counts[id]++;
            }
            int leastUsed = counts.Values.Min();
            var candidates = tracks.Where(track => counts[int.Parse(track)] == leastUsed).ToList();
            var skies = new List<List<string>>();
            var rocks = new List<List<string>>();
            LoadTemplate(skies, Path.Combine(data, "extracted", "sunsandplanets.txt"));
            LoadTemplate(rocks, Path.Combine(data, "extracted", "asteroids.txt"));
            var backgrounds = File.ReadAllLines(Path.Combine(data, "custom_backgrounds.txt"));
            var sector = new X3Sector(cell.X, cell.Y, candidates[m_Rand.Next(candidates.Count)],
                int.Parse(backgrounds[m_Rand.Next(backgrounds.Length)]), skies[m_Rand.Next(skies.Count)], rocks[m_Rand.Next(rocks.Count)]);
            if (sector.Sun == null) throw new InvalidDataException(X3Utils.Ui("Form1.TheSelectedSectorTemplateHasNoSun"));
            sector.MarkSectorRace(ERace.Unknown);
            sector.SectorID = $"102{cell.Y + 1:00}{cell.X + 1:00}";
            sector.SectorName = name.Name;
            sector.StreamID = name.StreamID;
            sector.StreamLocation = name.StreamLocation;
            sector.StreamLength = name.StreamLength;

            // Sample economic attributes from this galaxy so custom stat ranges carry over.
            var sample = galaxy.Sectors[m_Rand.Next(galaxy.Sectors.Count)].SectorCreationData;
            sector.SectorCreationData.Manpower = sample.Manpower;
            sector.SectorCreationData.Support = sample.Support;
            sector.SectorCreationData.Research = sample.Research;
            sector.SectorCreationData.Xenons = sample.Xenons;
            sector.SectorCreationData.Terrans = sample.Terrans;
            sector.SectorCreationData.Production = sample.Production;
            sector.SectorCreationData.Consumption = sample.Consumption;
            return sector;
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            foreach (var cell in m_MapCells)
            {
                if (!cell.Key.Contains(e.Location)) continue;
                var sector = X3Galaxy.Instance.GetSectorAt(cell.Value.X, cell.Value.Y);
                if (sector == null)
                {
                    EditMapCell(cell.Value);
                    return;
                }
                using (var editor = new SectorEdit())
                {
                    editor.Initialize(sector);
                    editor.StartPosition = FormStartPosition.CenterScreen;
                    editor.ShowDialog(this);
                }
                UpdateSectorSearch(force: true);
                pictureBox1.Refresh();
                CheckMapValidity();
                return;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Point p = pictureBox1.PointToClient(Cursor.Position);
            X3Sector sector = GetSectorAtLocation(p);
            if (sector == null)
            {

            }
        }

        private void GalaxyStartChanged(object sender, EventArgs e)
        {
            if (!((RadioButton)sender).Checked) return;
            cbClusteredXenon.Enabled = rbStandardStart.Checked;
            if (!rbStandardStart.Checked)
                cbClusteredXenon.Checked = rbChaoticStart.Checked;
            UpdateXenonPreview(sender, e);
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, X3Utils.Ui("About.Body", X3Utils.GetVersion()),
                X3Utils.Ui("btnAbout"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void miReleaseNotes_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(X3Utils.HelpFile("ReleaseNotes"));
        }

        private void btnHelpMain_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(X3Utils.HelpFile());
        }

        private void btnLaunchWeaponsEditor_Click(object sender, EventArgs e)
        {
            WeaponsEditor we = new WeaponsEditor();
            we.MayhemFolder = m_CurX3RootFolder + "addon\\types\\";
            we.ShowDialog();
        }

        // Select a custom music folder. If selection failed, keep the existing selection.
        private void btnCustomMusic_Click(object sender, EventArgs e)
        {
            if (m_CurX3RootFolder == string.Empty)
            {
                MessageBox.Show(X3Utils.Ui("Form1.YouHaveToSetYourMayhemRoot"));
                return;
            }
            CustomMusic cm = new CustomMusic(m_CurX3MusicFolder, m_CurX3RootFolder);
            DialogResult dr = cm.ShowDialog();
            if (dr == DialogResult.OK)
            {
                X3Utils.SaveSetting("X3Music", cm.SelectedFolder);
                m_CurX3MusicFolder = cm.SelectedFolder;

                rbCustom.Enabled = true;
                rbCustom.Checked = true;
                Message(X3Utils.Ui("Form1.CustomMusicConfiguredSuccessfully"));
            }
        }
    }
}
