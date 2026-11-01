using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Forms.VisualStyles;

namespace X3_Mayhem_Galaxy_Generator
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
        int m_LeftMargin = 4;
        int m_TopMargin = 4;

        // All of the relevant labels in the relations group box that control relations have a tag.
        private List<Label> m_RelationsLabels;


        public Form1()
        {
            InitializeComponent();
            toolStrip1.Renderer = new ActiveToolStripRenderer();

            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            toolTip1.ShowAlways = true;


            toolTip2.AutoPopDelay = 10;
            toolTip2.InitialDelay = 10;
            toolTip2.ReshowDelay = 10;
            toolTip2.ShowAlways = true;

            lblWarning.Overflow = ToolStripItemOverflow.Never;
            toolStrip1.SizeChanged += (sender, e) => ResizeValidityLabel();
            ResizeValidityLabel();
            lblWarning.Text = "";
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
                if (m_CurSaveGameFolder.Trim() == string.Empty)
                {
                    m_CurSaveGameFolder = "Click HERE to set folder.";
                }
#if DEBUG
                lblX3SaveGameFolder.Text = "Done";
#else
                lblX3SaveGameFolder.Text = m_CurSaveGameFolder;
#endif
            }

            if (m_CurX3RootFolder == string.Empty)
            {
                m_CurX3RootFolder = X3Utils.GetSetting("X3Root");
#if (DEBUG)
                m_CurX3RootFolder = @"C:\Mayhem 3\";
                X3Utils.SaveSetting("X3Root", m_CurX3RootFolder);
#endif
                if (m_CurX3RootFolder.Trim() == string.Empty)
                {
                    m_CurX3RootFolder = "Click HERE to set folder.";
                }

                lblX3RootFolder.Text = m_CurX3RootFolder;
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
            this.Text = $"ZMap Expanded v{ver} - by herobetty; original ZMap by Mordan (Hairless-Ape)";

            SetupLocalization();
            UpdateRelationsGrid();
            UpdateSectorCount();
        }

        private void SetupLocalization()
        {
            if (File.Exists("Localizations.json"))
            {
                string locz = File.ReadAllText("Localizations.json");
                X3Galaxy.Instance.LocalizationDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(locz);
                // Set this form's child controls to the dictionary values, and recurse into all subcontainers as well.
                X3Utils.SetControlChildrenText(this);
            }
            else
                throw new Exception("Localization file not found.");

            toolTip1.SetToolTip(btnGenerateSystems, X3Utils.GetLocalizedText("{Tips.BtnGenerateSystems}"));
            toolTip1.SetToolTip(btnGenerateGates, X3Utils.GetLocalizedText("{Tips.BtnGenerateGates}"));
            toolTip1.SetToolTip(btnGenerateStats, X3Utils.GetLocalizedText("{Tips.BtnGenerateStats}"));
            toolTip1.SetToolTip(lblMayhemRoot, X3Utils.GetLocalizedText("{Tips.BtnX3Root}"));
            toolTip1.SetToolTip(lblMayhemSave, X3Utils.GetLocalizedText("{Tips.BtnX3Save}"));
            toolTip1.SetToolTip(cbClusteredVoids, X3Utils.GetLocalizedText("{Tips.CbClusteredVoids}"));
            toolTip1.SetToolTip(cbClusteredXenon, X3Utils.GetLocalizedText("{Tips.CbClusteredXenon}"));
            toolTip1.SetToolTip(cbShattered, X3Utils.GetLocalizedText("{Tips.CbShattered}"));
            toolTip1.SetToolTip(cbLimitedEnclaves, X3Utils.GetLocalizedText("{Tips.CbLimitedEnclaves}"));
            toolTip1.SetToolTip(cbExtraSystemStats, X3Utils.GetLocalizedText("{Tips.CbExtraSystemStats}"));
            toolTip1.SetToolTip(cbUseParticles, X3Utils.GetLocalizedText("{Tips.CbParticles}"));
            toolTip1.SetToolTip(tbFog, X3Utils.GetLocalizedText("{Tips.SlFog}"));
            toolTip1.SetToolTip(gbExpansion, X3Utils.GetLocalizedText("{Tips.GbExpansion}"));
            toolTip1.SetToolTip(gbSystemSpread, X3Utils.GetLocalizedText("{Tips.GbSystemSpread}"));
            toolTip1.SetToolTip(gbGalaxySize, X3Utils.GetLocalizedText("{Tips.GbGalaxySize}"));
            toolTip1.SetToolTip(gbGateDensity, X3Utils.GetLocalizedText("{Tips.GbGateDensity}"));
            toolTip1.SetToolTip(gbMapType, X3Utils.GetLocalizedText("{Tips.GbMapType}"));
            toolTip1.SetToolTip(gbMusic, X3Utils.GetLocalizedText("{Tips.GbMusic}"));
            toolTip1.SetToolTip(gbRelations, X3Utils.GetLocalizedText("{Tips.GbRelations}"));
            toolTip1.SetToolTip(cbChaoticStart, X3Utils.GetLocalizedText("{Tips.cbChaotic}"));
            toolTip1.SetToolTip(cbPeacefulStart, X3Utils.GetLocalizedText("{Tips.cbPeaceful}"));

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
            rtb1.AppendText(msg + "\n");
        }

        private void btnGenerateGates_Click(object sender, EventArgs e)
        {
            rtb1.Clear();
            GenerateGates();
        }

        private void GenerateGates()
        {
            X3Galaxy.Instance.GalaxyCreationSettings.LimitedEnclaves = cbLimitedEnclaves.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.BorderGateDensity = rbLowGateDensity.Checked ? EGateDensity.Low : EGateDensity.High;
            m_Generator.GenerateGates();
            pictureBox1.Refresh();
            CheckMapValidity();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            List<string> musicFiles = new List<string>();
            List<string> backgroundFiles;
            List<List<string>> m_SunsAndPlanets = new List<List<string>>();
            List<List<string>> m_Asteroids = new List<List<string>>();
            int x, y;
            lblWarning.Text = "";

            // Load up templates for both soundFiles and Backgrounds used by the sytems.
            try
            {
                if (rbBasicMusic.Checked)
                {
                    musicFiles = new List<string>(File.ReadAllLines(m_CurX3RootFolder + "\\mayhem_data\\custom_musics.txt"));
                }
                else if (rbLitcubeMusic.Checked)
                {
                    musicFiles = new List<string>(File.ReadAllLines(m_CurX3RootFolder + "\\mayhem_data\\custom_musics_lu.txt"));
                }
                else if (rbAllMusic.Checked)
                {
                    // User chose the ALL selection, which means we need to read the list of music files from their \soundtrack folder.
                    string path = m_CurX3RootFolder + "\\soundtrack";
                    List<string> files = Directory.GetFiles(path, "*.mp3").ToList();
                    files.ForEach(item => musicFiles.Add(Path.GetFileNameWithoutExtension(item)));
                }
                else
                {
                    // Custom music folder.  Here, we should verify a custom folder is set, and if so, load all the files from it.
                    if (Directory.Exists(m_CurX3MusicFolder))
                    {
                        List<string> files = Directory.GetFiles(m_CurX3MusicFolder, "*.mp3").ToList();
                        if (files.Count < 10)
                        {
                            MessageBox.Show($"The Custom music folder: {m_CurX3MusicFolder} contains insufficient mp3 files.  Halting");
                            return;
                        }

                        Message("Custom Music");

                        // Note, that the 'transmuted' music file name should have already been copied to the Mayhem 3/soundtrack/ folder.
                        // The only thing stored in each sector data is the transmuted numerical base name of the music file, with no extension.
                        foreach (string fname in files)
                        {
                            string fwe = Path.GetFileNameWithoutExtension(fname);
                            string intfname = X3Utils.Transmute(fwe);
                            Message(fname + " (" + intfname + ")");
                            musicFiles.Add(intfname);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"The Custom music folder: {m_CurX3MusicFolder} doesn't exist.  Halting");
                        return;
                    }
                }
            }
            catch(Exception a1)
            {
                MessageBox.Show(a1.Message);
                return;
            }

            // Reserve soundtrack IDs below 15, except the unused X:BTF building theme (12).
            int originalMusicCount = musicFiles.Count;

            musicFiles = musicFiles.Select(file => file.Trim()).Where(X3Utils.IsSectorMusicTrack).ToList();
            int excludedMusicCount = originalMusicCount - musicFiles.Count;
            if (excludedMusicCount > 0)
            {
                Message($"Excluded {excludedMusicCount} invalid or reserved music track(s).");
            }

            if (musicFiles.Count == 0)
            {
                MessageBox.Show(
                    "No usable sector music was found. Music filenames must be numeric, with an ID of 12 or at least 15.",
                    "Invalid Music Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (rbCustom.Checked && musicFiles.Count < 10)
            {
                MessageBox.Show(
                    "Custom Music requires at least 10 usable tracks. Only soundtrack ID 12 and IDs 15 or higher are permitted.",
                    "Insufficient Custom Music",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
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


            //rtb1.Clear();

            X3Galaxy.Instance.GalaxyCreationSettings.UseParticles = cbUseParticles.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ClusteredVoids = cbClusteredVoids.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ShatteredGalaxy = cbShattered.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ClusteredXenons = cbClusteredXenon.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ArgonSpread = (int)udArgon.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.BoronSpread = (int)udBoron.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.SplitSpread = (int)udSplit.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.ParanidSpread = (int)udParanid.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.TeladiSpread = (int)udTeladi.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.TerranSpread = (int)udTerran.Value;
            X3Galaxy.Instance.GalaxyCreationSettings.ExtraSystemStats = cbExtraSystemStats.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.PeacefulStart = cbPeacefulStart.Checked;
            X3Galaxy.Instance.GalaxyCreationSettings.ChaoticStart = cbChaoticStart.Checked;

            if (rbOrigin.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Origin;
            if (rbEarly.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Early;
            if (rbAverage.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Average;
            if (rbAdvanced.Checked) X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion = EGalaxyExpansion.Advanced;

            if (rbBasicMusic.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 0;
            if (rbLitcubeMusic.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 1;
            if (rbAllMusic.Checked) X3Galaxy.Instance.GalaxyCreationSettings.MusicSelection = 2;

            X3Galaxy.Instance.GalaxyCreationSettings.MapType = rbSquareMap.Checked ? 0 : 1;

            if (rbSmallGalaxy.Checked)
            {
                X3Galaxy.Instance.GalaxyCreationSettings.GalaxySize = 0;
                if (rbSquareMap.Checked)
                {
                    x = 10;
                    y = 10;
                }
                else
                {
                    x = 13;
                    y = 7;
                }
            }
            else if (rbMediumGalaxy.Checked)
            {
                X3Galaxy.Instance.GalaxyCreationSettings.GalaxySize = 1;
                if (rbSquareMap.Checked)
                {
                    x = 13;
                    y = 13;
                }
                else
                {
                    x = 18;
                    y = 9;
                }
            }
            else
            {
                X3Galaxy.Instance.GalaxyCreationSettings.GalaxySize = 2;
                if (rbSquareMap.Checked)
                {
                    x = 17;         // NOTE that loaded maps can be odd, like 16x18.
                    y = 17;
                }
                else
                {
                    x = 22;
                    y = 13;
                }
            }

            do
            {
                m_Generator.GenerateGalaxy(x, y, musicFiles, backgroundFiles, m_SunsAndPlanets, m_Asteroids);
            }
            while (X3Galaxy.Instance.Sectors.Count > 256);       // This should never happen, as 17x17 = 289..  minus 15% for voids (38) = 246 max systems..  and we only have 256 names to work with.


            X3Galaxy.Instance.AssignSystemNamesAndSoundToGeneratedSystems(m_MasterSystemList);

            m_Generator.GenerateSystemStats();

            GenerateGates();
            EnableButtons();
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

            X3Galaxy.GateNetworkReport network =
                X3Galaxy.Instance.AnalyzeGateNetwork();
            List<string> problems = new List<string>();

            if (!X3Galaxy.HasUnknownEnclave())
                problems.Add("No Unknown Enclave (Renegades plot)");

            if (network.GatelessSectors > 0)
                problems.Add($"{network.GatelessSectors} gateless sector(s)");

            if (network.OutsideLargestNetwork > 0)
                problems.Add($"{network.OutsideLargestNetwork} outside main network");

            if (network.MissingDestinations > 0)
                problems.Add($"{network.MissingDestinations} gate(s) with missing destinations");

            if (network.MissingReturnGates > 0)
                problems.Add($"{network.MissingReturnGates} gate(s) without return connections");

            if (problems.Count > 0)
            {
                lblWarning.Text = "INVALID: " + string.Join("; ", problems);
                lblWarning.ForeColor = Color.DarkRed;
            }
            else
            {
                lblWarning.Text = "VALID: Unknown Enclave present; all sectors connected.";
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

            lblSectorCount.Text = total > 0
                ? $"Sectors: {count} / {total}"
                : "Sectors: 0";

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

            s1.RemoveAll(item => item.StartsWith("//"));    // 212 names in stream 1
            s2.RemoveAll(item => item.StartsWith("//"));    // 44 names in stream 2

            s1.Shuffle();
            s2.Shuffle();

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
        }



        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            float horz_skew = 1.0f;
            float vert_skew = 1.0f;

            if (X3Galaxy.Instance.Sectors.Count == 0) return;

            // Smooth galaxy map geometry.
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Use Windows' preferred text rendering.
            e.Graphics.TextRenderingHint = TextRenderingHint.SystemDefault;

            int maxx = X3Galaxy.Instance.MaxX * 2 - 1;
            int maxy = X3Galaxy.Instance.MaxY * 2 - 1;

            if (maxx < maxy)        // 16x18 would shrink horizontal
            {
                horz_skew = (float)maxx / (float)maxy;
            }
            else if (maxx > maxy)   // 18x16 would shrink verticle.
            {
                vert_skew = (float)maxy / (float)maxx;
            }

            int nameMarginX = 0;
            int nameMarginY = 0;

            if (m_ShowNames == 1)
            {
                int baseCellWidth = (int)((pictureBox1.Width - 8) * horz_skew) / maxx;
                int baseCellHeight = (int)((pictureBox1.Height - 8) * vert_skew) / maxy;
                nameMarginX = (int)Math.Ceiling(baseCellWidth * 0.45f);

                using (Font labelFont = new Font("Tahoma", GetMapFontSize(baseCellHeight) * 1.1f))
                {
                    nameMarginY = (int)Math.Ceiling(e.Graphics.MeasureString("T", labelFont).Height) + 4;
                }
            }

            int totalwidth = (int)((pictureBox1.Width - 8 - nameMarginX * 2) * horz_skew);
            int totalheight = (int)((pictureBox1.Height - 8 - nameMarginY) * vert_skew);

            if (totalwidth <= 0 || totalheight <= 0)
                return;

            int elementwidth = totalwidth / maxx;
            int elementheight = totalheight / maxy;

            if (elementwidth <= 0 || elementheight <= 0)
                return;

            m_LeftMargin = 4 + (m_ShowNames == 1
                ? nameMarginX + (totalwidth - elementwidth * maxx) / 2
                : 0);

            m_TopMargin = 4 + (m_ShowNames == 1
                ? nameMarginY + (totalheight - elementheight * maxy) / 2
                : 0);

            for (int x = 0; x < maxx; x++)
            {
                for (int y = 0; y < maxy; y++)
                {
                    if (x % 2 == 0 && y % 2 == 0)
                    {
                        DrawSector(e.Graphics, x, y, elementwidth, elementheight);
                    }
                }
            }

            // Draw connections once, after all sector tiles are rendered.
            DrawGateLines(e.Graphics, elementwidth, elementheight);

            // Draw labels last so gate lines cannot obscure them.
            if (m_ShowNames == 1)
                DrawSectorNames(e.Graphics, elementwidth, elementheight);
        }

        private static int GetMapFontSize(int elementheight)
        {
            if (elementheight > 45) return 10;
            if (elementheight > 35) return 9;
            if (elementheight > 30) return 8;
            if (elementheight > 25) return 7;
            return 6;
        }

        private void DrawSectorNames(
            Graphics g, int elementwidth, int elementheight)
        {
            using (Font font = new Font(
                "Tahoma", GetMapFontSize(elementheight) * 1.1f))
            using (SolidBrush brush = new SolidBrush(Color.White))
            using (StringFormat format =
                new StringFormat(StringFormatFlags.NoWrap))
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                int labelHeight =
                    (int)Math.Ceiling(g.MeasureString("T", font).Height) + 2;

                int labelWidth = (int)Math.Ceiling(elementwidth * 1.9f);

                foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
                {
                    if (string.IsNullOrWhiteSpace(sector.SectorName))
                        continue;

                    Rectangle tile = sector.ScreenBox;

                    if (tile.Width <= 0 || tile.Height <= 0)
                        continue;

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

        private void DrawSector(Graphics g, int x, int y, int elementwidth, int elementheight)
        {
            SolidBrush backgroundBrush = null;
            int xCoordinate = x / 2;
            int yCoordinate = y / 2;

            Font ft = new Font("Tahoma", GetMapFontSize(elementheight));

            int textHeight = (int)g.MeasureString("T", ft).Height;
            int leftSide = (x * elementwidth) + m_LeftMargin;
            int topSide = (y * elementheight) + m_TopMargin;

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

                    // Store where we drew this, as we will detect clicks there later.
                    sector.ScreenBox.X = leftSide;
                    sector.ScreenBox.Y = topSide;
                    sector.ScreenBox.Height = elementheight;
                    sector.ScreenBox.Width = elementwidth;


                    bool isStartingSector = false;
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


                        if (m_ShowTerran == 1 && sector.SectorCreationData.Terrans == 1)
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
                    lblX3RootFolder.Text = m_CurX3RootFolder;

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
                lblX3SaveGameFolder.Text = m_CurSaveGameFolder;
#endif

                X3Utils.SaveSetting("X3Save", m_CurSaveGameFolder);
                TestRequiredFoldersSet();
            }
        }

        private void DrawGateLines(Graphics g, int elementwidth, int elementheight)
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
                            starty = (sector.y * elementheight * 2) - 1 + m_TopMargin;
                            endy = starty - linelength;
                            startx = (sector.x * elementwidth * 2) + elementwidth / 2 + m_LeftMargin;
                            if (gate.IsSpecial) startx += 2;
                            endx = startx;
                            g.DrawLine(p, startx, starty, endx, endy);
                            break;
                        case 1: // South (bottom)
                            // no need.
                            break;
                        case 2: // West (left)
                            linelength = elementwidth * (((sector.x - gate.gx) * 2) - 1) - 1;
                            startx = (sector.x * elementwidth * 2) - 1 + m_LeftMargin;
                            endx = startx - linelength + 1;
                            starty = (sector.y * elementheight * 2) + elementheight / 2 + m_TopMargin;
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
            Persist p = new Persist();
            p.OnSettingsUpdated += UpdateUIFromGalaxySettings;
            p.Initialize();
            p.ShowDialog(this);

            if (p.GalaxyWasLoaded)
            {
                UpdateUIFromGalaxySettings();
                UpdateActiveGalaxyName();
                pictureBox1.Refresh();
                CheckMapValidity();
            }
        }

        private void EnableButtons()
        {
            UpdateSectorCount();

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
            cbClusteredXenon.Checked = X3Galaxy.Instance.GalaxyCreationSettings.ClusteredXenons;
            cbExtraSystemStats.Checked = X3Galaxy.Instance.GalaxyCreationSettings.ExtraSystemStats;
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
            }
            cbShattered.Checked = X3Galaxy.Instance.GalaxyCreationSettings.ShatteredGalaxy;

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
                    if (newentry == null) throw new Exception($"Invalid line in {filename}.");
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
            if (!Enum.TryParse(source, out sourceRace)) throw new Exception($"Unable to parse Tag {lbl.Tag.ToString()}");
            if (!Enum.TryParse(target, out destinationRace)) throw new Exception($"Unable to parse Tag {lbl.Tag.ToString()}");

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
                                    throw new Exception($"Invalid relation value {iRelation}");
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
            m_Generator.GenerateSystemStats();
            pictureBox1.Refresh();
        }

        private void tbFog_Scroll(object sender, EventArgs e)
        {
            X3Galaxy.Instance.GalaxyCreationSettings.FogLevelPercent = tbFog.Value * 25;
        }


        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (X3Galaxy.Instance.Sectors.Count == 0) return;

            Point mouseat = pictureBox1.PointToClient(Cursor.Position);   // coordinates relative to the control.
            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                if (sector.ScreenBox.Contains(mouseat))
                {
                    string tooltip = sector.SectorName;
                    if (m_ShowStats == 1)
                    {
                        tooltip +=
                            $"\nPopulation: {sector.SectorCreationData.Manpower}" +
                            $"\nStation Support: {sector.SectorCreationData.Support}" +
                            $"\nResearch Rate: {sector.SectorCreationData.Research}%";
                    }

                    toolTip2.Show(tooltip, pictureBox1, new Point(sector.ScreenBox.X + sector.ScreenBox.Width + 10, sector.ScreenBox.Y - 10));
                    return;
                }
            }
            toolTip2.Hide(pictureBox1);
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

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (X3Galaxy.Instance.Sectors.Count == 0) return;

            //rtb1.Clear();

            // We stored the rectangle where we drew each sector box in the paint method.  Here we can just see if we clicked in one of those rectangles to figure
            // out which sector was clicked.
            X3Sector sector = GetSectorAtLocation(pictureBox1.PointToClient(Cursor.Position));
            if (sector != null)
            {
                SectorEdit se = new SectorEdit();
                se.Initialize(sector);
                se.StartPosition = FormStartPosition.CenterScreen;
                se.ShowDialog();
                pictureBox1.Refresh();
            }

            CheckMapValidity();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Point p = pictureBox1.PointToClient(Cursor.Position);
            X3Sector sector = GetSectorAtLocation(p);
            if (sector == null)
            {

            }
        }

        private void cbPeacefulStart_CheckedChanged(object sender, EventArgs e)
        {
            if (cbPeacefulStart.Checked)
            {
                cbChaoticStart.Checked = false;
                cbClusteredXenon.Checked = false;
            }
        }

        private void cbChaoticStart_CheckedChanged(object sender, EventArgs e)
        {
            cbClusteredXenon.Enabled = !cbChaoticStart.Checked;

            if (cbChaoticStart.Checked)
            {
                cbPeacefulStart.Checked = false;
                cbClusteredXenon.Checked = true;
            }
        }

        private void btnHelpMain_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("HelpMain.txt");
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
                MessageBox.Show("You have to set your Mayhem 3 root folder first.  Look above.");
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
                Message("Custom Music configured successfully.");
            }
        }
    }
}
