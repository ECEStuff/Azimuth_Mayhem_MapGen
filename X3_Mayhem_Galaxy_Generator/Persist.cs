using Newtonsoft.Json;
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
using System.Xml;
using static System.Collections.Specialized.BitVector32;

namespace X3_Mayhem_Galaxy_Generator
{
    public partial class Persist : Form
    {
        private List<string> m_SavedGalaxyFolderNames = new List<string>();
        private string m_X3SaveFolder;
        private string m_CurX3RootFolder;
        private const string ActiveString = " (active)";
        public bool GalaxyWasLoaded = false;

        public event SettingsUpdated OnSettingsUpdated;

        public Persist()
        {
            InitializeComponent();
        }
        public void Initialize()
        {
            m_X3SaveFolder = X3Utils.GetSetting("X3Save"); 
            m_CurX3RootFolder = X3Utils.GetSetting("X3Root"); 

            // Determine list of saved galaxies
            LoadSavedGalaxiesList();

            if (X3Galaxy.Instance.Sectors.Count == 0)
            {
                btnSaveNew.Enabled = false;
            }

            SetupLocalization();
        }

        private void SetupLocalization()
        {
            this.Text = X3Utils.GetLocalizedText("{SaveLoadFormTitle}");
            X3Utils.SetControlChildrenText(this);
        }

        private void Message(string msg, bool clear = false)
        {
            if (clear) rtb1.Clear();
            rtb1.AppendText(msg + "\r\n");
        }

        /// <summary>
        /// Loads up 'most' of the data associated with a map.  If the map was created by the older original generator, we will not know the settings used to create that map so 
        /// none of that is loaded.  Also, the Gamestarts.xml is not loaded, so any start location that was set in there is lost.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadSelected_Click(object sender, EventArgs e)
        {
            if (lbAvailableGalaxies.SelectedItem == null) return;       // nothing selected to load.

            string mapToLoad = lbAvailableGalaxies.SelectedItem.ToString().Replace(ActiveString, "");

            Message($"{mapToLoad} map is loading into the Generator for you to review or edit.", true);
            Message("Here are some system statistics for the loaded map.  Close this dialog to see the map.");

            // Loads the entire map.  Gates, Asteroids etc.     There are no Sector Names, ID's or System stats stored in the map file.   (thanks egosoft).
            string universepath = m_CurX3RootFolder + "mayhem_galaxies\\" + mapToLoad + "\\addon\\maps\\x3_universe.xml";
            if (!File.Exists(universepath))
            {
                Message($"File {universepath} does not exist.");
                return;
            }
            X3Galaxy.Instance.LoadFrom_X3_Universe_XML(universepath);


            // If a settings file exists, load it.
            string settingspath = m_CurX3RootFolder + "mayhem_galaxies\\" + mapToLoad + "\\GalaxySettings.json";
            X3Galaxy.Instance.LoadFrom_X3_Universe_Settings(settingspath);

            // Notify the U.I. we updated settings, so it can reflect those new settings.
            OnSettingsUpdated?.Invoke();


            // Loads all the sector names and stats
            string language = X3Utils.GetLocalizedText("language_id_file");
            string path9970 = m_CurX3RootFolder + "mayhem_galaxies\\" + mapToLoad + $"\\addon\\t\\9970-L0{language}.xml";
            if (!File.Exists(path9970))
            {
                Message($"File {path9970} does not exist.");
                return;
            }
            X3Galaxy.Instance.Load9970_XML(path9970);


            // load the 00044 file, which has the system Sound stream pointers and lengths for the systems we just loaded.
            string path00044 = m_CurX3RootFolder + "mayhem_galaxies\\" + mapToLoad + $"\\addon\\mov\\000{language}.xml";
            if (!File.Exists(path00044))
            {
                Message($"File {path00044} does not exist.");
                return;
            }
            X3Galaxy.Instance.Load00044_XML(path00044);

            // Double Check was ok.  Are there any systems we loaded from the Map file that were not assigned a StreamID from the 00044 file..
            // var val = X3Galaxy.Instance.Sectors.FindAll(item => item.StreamID == 0);


            // Here, we can run some stats on the loaded galaxy.
            //<t id="10000101">5</t>            // Dejure Owner:        id = 1000 + SectorLocation      20% chance galaxy wide for an actual owner. some races up to 5%, while others at 1%.  odd.
            //<t id="10010101">0</t>            // smugglers:           id = 1001 + SectorLocation      Not used
            //<t id="10020101">48</t>           // Xenons:              id = 1002 + SectorLocation      Abnormal signals.  0-60, average 23.    30% of these are at 0.
            //<t id="10030101">0</t>            // Terrans:             id = 1003 + "                   20% chance galaxy wide.
            //<t id="10040101">99</t>           // research:            id = 1004 + "                   40-100, average 70
            //<t id="10050101">9</t>            // support              id = 1005 + "                   Number of stations supported in system.  8-15, Average 9.
            //<t id="10060101">107</t>          // manpower             id = 1006 + "                   Population of system (60-130, average 95)
            //<t id="10070101">-1</t>           // cluster              id = 1007 + "                   Not Used.  Set to -1 to duplicate old program.
            //<t id="10080101">3 </t >          // production           id = 1008 + "                   Type of Food produced.  See EFood above for enumeration.
            //<t id="10090101">1</t>            // consumption          id = 1009 + "                   Type of food consumed.  See EFood.
            //<t id="10100101">4</t>            // company              id = 1010 + "                   17-21% chance galaxy wide for a company to exist in system.  1-5% per race + 1 company in unknown space.
            //                                                                                          Change this to be more evenly distributed.
            int secct = X3Galaxy.Instance.Sectors.Count();
            int gatect = X3Galaxy.Instance.Sectors.Sum(sector => sector.Gates.Count);
            string mapdimensions = $"{X3Galaxy.Instance.MaxX}, {X3Galaxy.Instance.MaxY}";
            int dejurect = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner != 0);
            int numdejurenotinsameracespace = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner != item.r && item.SectorCreationData.DejureOwner != 0);
            int argondejurecount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner == 1);
            int borondejurecount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner == 2);
            int splitdejurecount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner == 3);
            int paraniddejurecount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner == 4);
            int teladidejurecount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner == 5);
            int terrandejurecount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.DejureOwner == (int)ERace.Terran);
            int terrancount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Terrans == 1);
            int nocompaniescount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Company == -1);

            int company = (int)ECompany.Markus;
            int race = (int)X3Utils.GetRaceForCompany((ECompany)company);
            int companycount = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Company == company);
            int companyinNonRaceSpace = X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Company == company && item.r != race);
            int? kk = X3Galaxy.Instance.Sectors.Find(item => item.SectorCreationData.Company == company && item.r != race)?.r;

            int avgSectorSize = (int)X3Galaxy.Instance.Sectors.Average(item => item.size);
            // Find average, minimum, and max offset of North gate from system X center?
            int avgGateOffset = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Average(gate => gate.x);
            int minGateOffset = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Min(gate => gate.x);
            int maxGateOffset = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Max(gate => gate.x);
            // Find average distance (gate.z) of North gate in comparison to the system size (size)
            int avgGateDist = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Average(gate => gate.z);
            int minGateDist = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Min(gate => gate.z);
            int maxGateDist = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Max(gate => gate.z);
            // Find offset above/below ecliptic plane.
            int minGateDepth = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Min(gate => gate.y);
            int maxGateDepth = (int)X3Galaxy.Instance.Sectors.SelectMany(sector => sector.Gates).Where(gate => gate.gid == (int)EGateDirection.South).Max(gate => gate.y);


            // some random stats for fun.
            Message("Sector Count: ", secct);
            Message("Gate Count: ", gatect);
            Message("Galaxy Dimensions: " + mapdimensions);

            Message("Average Sector Size: ", avgSectorSize);
            Message("Average Gate Offset: ", avgGateOffset);
            Message("Min Gate Offset: ", minGateOffset);            // +/- 17% of system size.
            Message("Max Gate Offset: ", maxGateOffset);
            Message("Average Gate Distance: ", avgGateDist);
            Message("Min Gate Distance: ", minGateDist);            // 55-125% of system size.
            Message("Max Gate Distance: ", maxGateDist);
            Message("Min Gate Depth: " + minGateDepth);
            Message("Max Gate Depth: " + maxGateDepth);


            Message($"dejure Count ({dejurect * 100/secct})%", dejurect);
            Message($"dejure race found in non-Dejure owned systems", numdejurenotinsameracespace);
            Message($"Argon dejureOwner Count ({argondejurecount * 100/secct})%", argondejurecount);
            Message($"Boron dejureOwner Count ({borondejurecount * 100 / secct})%", borondejurecount);
            Message($"Split dejureOwner Count ({splitdejurecount * 100 / secct})%", splitdejurecount);
            Message($"Paranid dejureOwner Count ({paraniddejurecount * 100 / secct})%", paraniddejurecount);
            Message($"Teladi dejureOwner Count ({teladidejurecount * 100 / secct})%", teladidejurecount);
            Message($"Terran dejureOwner Count ({terrandejurecount * 100 / secct})%", terrandejurecount);
            Message($"Abnormal Signals Min", X3Galaxy.Instance.Sectors.Min(item => item.SectorCreationData.Xenons));
            Message($"Abnormal Signals at 0", X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Xenons == 0));
            Message($"Abnormal Signals Max", X3Galaxy.Instance.Sectors.Max(item => item.SectorCreationData.Xenons));
            Message($"Abnormal Signals Avg", (int)X3Galaxy.Instance.Sectors.Average(item => item.SectorCreationData.Xenons));
            Message($"Abnormal Signals Avg in Xenon space", (int)X3Galaxy.Instance.Sectors.Where(item => item.r == 6).Average(item => item.SectorCreationData.Xenons));
            Message($"Terran Memory Count ({terrancount * 100 / secct})%", terrancount);
            Message($"Research Min", X3Galaxy.Instance.Sectors.Min(item => item.SectorCreationData.Research));
            Message($"Research Max", X3Galaxy.Instance.Sectors.Max(item => item.SectorCreationData.Research));
            Message($"Research Avg", (int)X3Galaxy.Instance.Sectors.Average(item => item.SectorCreationData.Research));
            Message($"Support Min", X3Galaxy.Instance.Sectors.Min(item => item.SectorCreationData.Support));
            Message($"Support at 12", X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Support == 12));
            Message($"Support at 13", X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Support == 13));
            Message($"Support above 13", X3Galaxy.Instance.Sectors.Count(item => item.SectorCreationData.Support > 13));
            Message($"Support Max", X3Galaxy.Instance.Sectors.Max(item => item.SectorCreationData.Support));
            Message($"Support Avg", (int)X3Galaxy.Instance.Sectors.Average(item => item.SectorCreationData.Support));
            Message($"Population Min", X3Galaxy.Instance.Sectors.Min(item => item.SectorCreationData.Manpower));
            Message($"Population Max", X3Galaxy.Instance.Sectors.Max(item => item.SectorCreationData.Manpower));
            Message($"Population Avg", (int)X3Galaxy.Instance.Sectors.Average(item => item.SectorCreationData.Manpower));
            Message($"Company Count {(ECompany)company}", companycount);
            //Message($"{(ECompany)company} Companies not in their race Space (one found in {(ERace)kk})", companyinNonRaceSpace); 

            GalaxyWasLoaded = true;
        }

        private void Message(string msg, int val)
        {
            rtb1.AppendText(msg + $":  {val}\r\n");
        }

        private void LoadSavedGalaxiesList()
        {
            lbAvailableGalaxies.Items.Clear();
            m_SavedGalaxyFolderNames.Clear();

            // Check to see if the galaxies folder exists.
            if (File.Exists(m_CurX3RootFolder + "\\X3AP.exe"))
            {
                if (!Directory.Exists(m_CurX3RootFolder + "\\mayhem_galaxies"))
                {
                    Directory.CreateDirectory(m_CurX3RootFolder + "\\mayhem_galaxies");
                }
            }
            else
            {
                MessageBox.Show($"Warning: The folder you specified for X3 does not appear to be valid.  Can't find X3AP.exe in {m_CurX3RootFolder}");
                return;
            }

            // parse all folders found in the root\mayhem_galaxies folder.
            List<string> directories = Directory.GetDirectories(m_CurX3RootFolder + "\\mayhem_galaxies").ToList();

            foreach (string directory in directories)
            {
                string folder = new DirectoryInfo(directory).Name;
                m_SavedGalaxyFolderNames.Add(folder);


                // The only ACTIVE galaxy is the one referenced in the root folder.
                if (GetActiveMap() == folder)
                {
                    folder += ActiveString;
                }

                lbAvailableGalaxies.Items.Add(folder);
            }
        }

        private string GetActiveMap()
        {
            return X3Utils.GetSettingsMap(m_CurX3RootFolder + "GalaxySettings.json");
        }

        /// <summary>
        /// Saves the active Galaxy into a new folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveNew_Click(object sender, EventArgs e)
        {
            string src, dest, destroot;
            bool isActiveMap = false;

            // 1.77 - just double check that the map has at least one unknown enclave system.
            if (!X3Galaxy.HasUnknownEnclave())
            {
                MessageBox.Show(
                    "This galaxy cannot be saved because it contains no Unknown Enclaves. Please generate another galaxy.", "Invalid Galaxy", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Warning);
                return;
            }

            // ZMap Expanded 2.2 - Check for connectivity.
            X3Galaxy.GateNetworkReport network = X3Galaxy.Instance.AnalyzeGateNetwork();

            if (network.GatelessSectors > 0 || network.OutsideLargestNetwork > 0 || network.MissingDestinations > 0 || network.MissingReturnGates > 0)
            {
                MessageBox.Show(
                    "This galaxy has invalid gate connections. Please resolve the network warnings before saving.", "Invalid Galaxy",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }



            if (tbGalaxyName.Text == string.Empty)
            {
                tbGalaxyName.Text = X3Utils.GetRandomString(3) + "-" + X3Utils.GetRandomNumber(2);
            }

            // Create Galaxy files. Save the Universe!
            destroot = m_CurX3RootFolder + "mayhem_galaxies\\" + tbGalaxyName.Text;         // c:\mayhem 3\mayhem_galaxies\XXX-XX

            // If it's your current galaxy.
            if (tbGalaxyName.Text == GetActiveMap())
            {
                isActiveMap = true;
                string msg = X3Utils.GetLocalizedText("{UpdateActiveMapWarning}");
                string warning = X3Utils.GetLocalizedText("{Warning}");
                if (MessageBox.Show(msg, warning, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            string language = X3Utils.GetLocalizedText("language_id_file");

            X3Galaxy.Instance.GalaxyCreationSettings.GalaxyName = tbGalaxyName.Text;


            Directory.CreateDirectory(destroot);
            Directory.CreateDirectory(destroot + "\\addon");
            Directory.CreateDirectory(destroot + "\\addon\\director");
            Directory.CreateDirectory(destroot + "\\addon\\maps");
            Directory.CreateDirectory(destroot + "\\addon\\mov");
            Directory.CreateDirectory(destroot + "\\addon\\t");
            Directory.CreateDirectory(destroot + "\\addon\\types");
            Directory.CreateDirectory(destroot + "\\objects");
            Directory.CreateDirectory(destroot + "\\objects\\cut");

            // Create /addon/maps/x3_universe.xml
            X3Galaxy.Instance.CreateX3UniverseFile(destroot + "\\addon\\maps\\x3_universe.xml");

            // Create /addon/director/start.xml
            src = m_CurX3RootFolder + "mayhem_data\\template_start.xml";
            dest = destroot + "\\addon\\director\\start.xml";
            try
            {
                File.Copy(src, dest, true);
            }
            catch (Exception a1)
            {
                MessageBox.Show($"Error copying start.xml file: {a1.Message}");
                return;
            }

            // Create \objects\cut\00749.bod file
            dest = destroot + "\\objects\\cut\\00749.bod";
            string bod = BodGen.Generate_00749();
            try
            {
                File.WriteAllText(dest, bod);
            }
            catch (Exception a2)
            {
                MessageBox.Show($"Error copying 00749.bod file: {a2.Message}");
                return;
            }

            // Create /addon/maps/WareTemplate.xml.  Apparently this is blank.
            dest = destroot + "\\addon\\maps\\WareTemplate.xml";
            try
            {
                File.WriteAllText(dest, "");
            }
            catch (Exception a3)
            {
                MessageBox.Show($"Error copying WareTemplate file: {a3.Message}");
                return;
            }
            // Create /addon/mov/00044.xml
            src = m_CurX3RootFolder + "mayhem_data\\template_00044.xml";
            dest = destroot + $"\\addon\\mov\\000{language}.xml";
            try
            {
                Create00044(src, dest);
            }
            catch (Exception a4)
            {
                MessageBox.Show($"Error copying 00044.xml file: {a4.Message}");
                return;
            }

            // Create /addon/t/8379-L044.xml
            src = m_CurX3RootFolder + "mayhem_data\\template_8379-L044.xml";
            dest = destroot + $"\\addon\\t\\8379-L0{language}.xml";
            try
            {
                Create8379(src, dest);
            }
            catch (Exception a5)
            {
                MessageBox.Show($"Error copying 8379-....xml file: {a5.Message}");
                return;
            }

            // Create /addon/t/9970-L044.xml
            dest = destroot + $"\\addon\\t\\9970-L0{language}.xml";
            try
            {
                Create9970(dest);
            }
            catch (Exception a6)
            {
                MessageBox.Show($"Error copying 9970-....xml file: {a6.Message}");
                return;
            }

            // Create /addon/types/gamestarts.xml      (this is an override loaded automatically by X3)
            src = m_CurX3RootFolder +"mayhem_data\\template_gamestarts.xml";
            string src2 = m_CurX3RootFolder + "mayhem_data\\gamestart_ships.json";
            dest = destroot + "\\addon\\types\\gamestarts.xml";
            try
            {
                CreateGameStarts(src, src2, dest);
            }
            catch (Exception a7)
            {
                MessageBox.Show($"Error copying gamestarts file: {a7.Message}");
                return;
            }

            // Create /addon/types/jobs.txt
            dest = destroot + "\\addon\\types\\jobs.txt";
            try
            {
                File.WriteAllText(dest, "");
            }
            catch (Exception a8)
            {
                MessageBox.Show($"Error copying jobs.txt file: {a8.Message}");
                return;
            }

            // Create /addon/types/TBackgrounds.txt         (this is an override loaded automatically by X3)
            src = m_CurX3RootFolder + "mayhem_data\\template_TBackgrounds.txt";
            dest = destroot + "\\addon\\types\\TBackgrounds.txt";
            try
            {
                CreateBackgrounds(src, dest);
            }
            catch (Exception a9)
            {
                MessageBox.Show($"Error copying backgrounds file: {a9.Message}");
                return;
            }

            // Save current Galaxy settings to a json file.
            string settings = JsonConvert.SerializeObject(X3Galaxy.Instance.GalaxyCreationSettings, Newtonsoft.Json.Formatting.Indented);
            try
            {
                File.WriteAllText(destroot + "\\GalaxySettings.json", settings);
            }
            catch (Exception aa)
            {
                MessageBox.Show($"Error copying galaxysettings file: {aa.Message}");
                return;
            }



            // If this is the active map, we copy the files over to the X3 root, but don't try to manipulate the savegame files.
            if (isActiveMap)
            {
                int oldindex = lbAvailableGalaxies.SelectedIndex;
                LoadSavedGalaxiesList();
                lbAvailableGalaxies.SelectedIndex = oldindex;

                Message($"Map {tbGalaxyName.Text} successfully updated.", true);
                if (ActivateMap(tbGalaxyName.Text, true))
                {
                    Message("Done. You are ready to play.");
                }
                else
                {
                    Message("The map was saved, but activation failed.");
                }
            }
            else
            {
                // Update the displayed list
                LoadSavedGalaxiesList();
                Message($"Map {tbGalaxyName.Text} successfully saved. ", true);
                Message($"To play this map, first set it Active.");
            }
        }

        private string Localize9970Template(string template)
        {
            StringBuilder sb = new StringBuilder(template);
            sb.Replace("{9970Dejure}", X3Utils.GetLocalizedText("{9970Dejure}"));
            sb.Replace("{9970Company}", X3Utils.GetLocalizedText("{9970Company}"));
            sb.Replace("{9970Abnormal}", X3Utils.GetLocalizedText("{9970Abnormal}"));
            sb.Replace("{9970Terran}", X3Utils.GetLocalizedText("{9970Terran}"));
            sb.Replace("{9970Population}", X3Utils.GetLocalizedText("{9970Population}"));

            sb.Replace("{9970Support}", X3Utils.GetLocalizedText("{9970Support}"));
            sb.Replace("{9970Research}", X3Utils.GetLocalizedText("{9970Research}"));
            sb.Replace("{9970Production}", X3Utils.GetLocalizedText("{9970Production}"));
            sb.Replace("{9970Consumption}", X3Utils.GetLocalizedText("{9970Consumption}"));

            return sb.ToString();
        }


        private void Create9970(string destination)
        {
            List<string> content = new List<string>();
            string secid = "";

            string language = X3Utils.GetLocalizedText("language_id");

            // Add header
            content.Add("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            content.Add($"<language id=\"{language}\">");

            // Section 1 is list of systems
            content.Add("\t<page id=\"7\" title=\"Boardcomp.Sectornames\" descr=\"Names of all sectors(spoken by Boardcomputer)\" voice=\"yes\">");
            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                content.Add($"\t\t<t id=\"{sector.SectorID}\">{sector.SectorName}</t>");
            }
            content.Add("\t</page>");

            // Section 2 is list of sector data unique to Mayhem and their DISPLAYED strings for stats.  Terran Memory, Max Population for system etc..
            // Many of these are displayed using different colors depending on their value so they have to be specially formatted.
            string template = Localize9970Template(@"<t id=""{sys_id}"">{sys_name} \(x:{sys_x}, y:{sys_y}\)\n\n{9970Dejure}: {sys_dejure}\n{9970Company}: {sys_corp}\n{9970Abnormal}: {sys_xenon} %\n{9970Terran}: {sys_terran}\n\n{9970Population}: {sys_population}\n{9970Support}: {sys_support}\n{9970Research}: {sys_research} %\n\n{9970Production}: {sys_production}\n{9970Consumption}: {sys_consumption}\n</t>");

            content.Add("\t<page id=\"19\" title=\"Sector description\" descr=\"Long descriptions of all sectors\" voice=\"no\">");
            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                secid = "103" + sector.SectorID.Remove(0, 3);    // All the id's in this section start with 103. Weird.
                string val = template.Replace("{sys_id}", secid);
                val = val.Replace("{sys_name}", sector.SectorName);
                val = val.Replace("{sys_x}", sector.x.ToString());
                val = val.Replace("{sys_y}", sector.y.ToString());
                val = val.Replace("{sys_dejure}", X3Utils.GetFormattedRaceName(sector.SectorCreationData.DejureOwner));
                val = val.Replace("{sys_corp}", X3Utils.GetFormattedCompanyName(sector.SectorCreationData.Company));
                val = val.Replace("{sys_xenon}", X3Utils.GetFormattedAbnormalSignals(sector.SectorCreationData.Xenons));
                val = val.Replace("{sys_terran}", X3Utils.GetFormattedTerranMemory(sector.SectorCreationData.Terrans));
                val = val.Replace("{sys_population}", X3Utils.GetFormattedMaxPopulation(sector.SectorCreationData.Manpower));
                val = val.Replace("{sys_support}", X3Utils.GetFormattedStationSupport(sector.SectorCreationData.Support));
                val = val.Replace("{sys_research}", X3Utils.GetFormattedResearchRate(sector.SectorCreationData.Research));
                val = val.Replace("{sys_production}", X3Utils.GetFormattedFoodName((EFood)sector.SectorCreationData.Production));
                val = val.Replace("{sys_consumption}", X3Utils.GetFormattedFoodName((EFood)sector.SectorCreationData.Consumption));
                content.Add("\t\t" + val);
            }
            content.Add("\t</page>");

            // Section 3 is where actual USED values are stored for the settings.
            content.Add("\t<page id=\"9970\" title=\"Mayhem Galaxy Generator\">");
            content.Add($"\t\t<t id=\"1\">{(int)X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion}</t>");
            content.Add($"\t\t<t id=\"2\">{0}</t>");        // mayhem_galaxy.dominance  Not used in game.
            content.Add($"\t\t<t id=\"3\">{0}</t>");        // mayhem_galaxy.minority   Not used in game.
            content.Add($"\t\t<t id=\"4\">{X3Galaxy.Instance.GalaxyCreationSettings.GalaxyName}</t>");
            // 5-40 are relations.
            int id = 5;
            for (int x=1; x<7; x++)
            {
                for (int y=1; y<7; y++)
                {
                    content.Add($"\t\t<t id=\"{id}\">{X3Galaxy.Instance.GalaxyCreationSettings.Relations[x-1,y-1]}</t>");
                    id++;
                }
            }
            content.Add($"\t\t<t id=\"100\">{(X3Galaxy.Instance.GalaxyCreationSettings.ShatteredGalaxy ? 1 : 0)}</t>");        // Not Used in game.
            content.Add($"\t\t<t id=\"101\">{(X3Galaxy.Instance.GalaxyCreationSettings.ExtraSystemStats ? 1 : 0)}</t>");       // Not Used in game.
            content.Add($"\t\t<t id=\"102\">{(X3Galaxy.Instance.GalaxyCreationSettings.ClusteredXenons ? 1 : 0)}</t>");        // Not Used in game.
            content.Add($"\t\t<t id=\"110\">{(X3Galaxy.Instance.GalaxyCreationSettings.HollowedGalaxy ? 1 : 0)}</t>");         // Not Used in game.
            content.Add($"\t\t<t id=\"111\">{(X3Galaxy.Instance.GalaxyCreationSettings.RandomDistribution ? 1 : 0)}</t>");     // Not Used in game.
            content.Add($"\t\t<t id=\"113\">{(X3Galaxy.Instance.GalaxyCreationSettings.LimitedEnclaves ? 1 : 0)}</t>");        // Not Used in game.

            X3Sector portalSec = X3Galaxy.Instance.Sectors.Find(item => item.IsMaelstromPortal);
            if (portalSec != null)
            {
                content.Add($"\t\t<t id=\"120\">{portalSec.x},{portalSec.y}</t>");     // Maelstrom startup X,Y if set at all.
            }

            if (X3Galaxy.Instance.GalaxyCreationSettings.PeacefulStart)
            {
                content.Add($"\t\t<t id=\"121\">{0}</t>");   
            }
            else if (X3Galaxy.Instance.GalaxyCreationSettings.ChaoticStart)
            {
                content.Add($"\t\t<t id=\"121\">{1}</t>");
            }
            else
            {
                content.Add($"\t\t<t id=\"121\">{-1}</t>");
            }


            // Now comes sector stats that are actually used.                                               
            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                // 102YYXX is how it's stored.
                // NOTE:  original author decided to reverse sector ID's X and Y coordinates for this section that he custom made.  Normally, it's "...YYXX", but here it's "...XXYY".  So we have to do the same when saving.
                string secidyy = sector.SectorID.Substring(3, 2);
                string secidxx = sector.SectorID.Substring(5, 2);
               
                content.Add($"\t\t<t id=\"{"1000" + secidxx + secidyy}\">{sector.SectorCreationData.DejureOwner}</t>");
                content.Add($"\t\t<t id=\"{"1001" + secidxx + secidyy}\">{sector.SectorCreationData.Smugglers}</t>");
                content.Add($"\t\t<t id=\"{"1002" + secidxx + secidyy}\">{sector.SectorCreationData.Xenons}</t>");
                content.Add($"\t\t<t id=\"{"1003" + secidxx + secidyy}\">{sector.SectorCreationData.Terrans}</t>");
                content.Add($"\t\t<t id=\"{"1004" + secidxx + secidyy}\">{sector.SectorCreationData.Research}</t>");
                content.Add($"\t\t<t id=\"{"1005" + secidxx + secidyy}\">{sector.SectorCreationData.Support}</t>");
                content.Add($"\t\t<t id=\"{"1006" + secidxx + secidyy}\">{sector.SectorCreationData.Manpower}</t>");
                content.Add($"\t\t<t id=\"{"1007" + secidxx + secidyy}\">{sector.SectorCreationData.Cluster}</t>");
                content.Add($"\t\t<t id=\"{"1008" + secidxx + secidyy}\">{sector.SectorCreationData.Production}</t>");
                content.Add($"\t\t<t id=\"{"1009" + secidxx + secidyy}\">{sector.SectorCreationData.Consumption}</t>");
                content.Add($"\t\t<t id=\"{"1010" + secidxx + secidyy}\">{sector.SectorCreationData.Company}</t>");
            }
            content.Add("\t</page>");

            content.Add("</language>");
            File.WriteAllLines(destination, content);
        }

        /// <summary>
        /// 8379-L044 seems to have some general text used in menu's and the startup screen of X3.  Here, looks like we just insert the galaxy name and that's it.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        private void Create8379(string src, string dest)
        {
            List<string> template = File.ReadAllLines(src).ToList();
            List<string> content = new List<string>();

            string language = X3Utils.GetLocalizedText("language_id");

            // Add header
            content.Add("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            content.Add($"<language id=\"{language}\">");
            // Content
            content.AddRange(template);

            string ver = X3Utils.GetVersion();
            for (int x=0; x < content.Count; x++)
            {
                content[x] = content[x].Replace("{galaxyname}", tbGalaxyName.Text + " ZMap V" + ver);
            }

            // and for some stupid reason, he already has the closing </language> marker in the template.. fail.
            File.WriteAllLines(dest, content);
        }


        /// <summary>
        /// The 00044.xml file is a combination of mayhem_data\template_00044.xml and some streaming voice data specific to the sectors we generated for this map.
        /// See AssignSystemNamesAndSoundToGeneratedSystems() in the main form to see how the sector names and voice data is loaded from the 2 templates mayhem 3 provided.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        private void Create00044(string src, string dest)
        {
            List<string> template = File.ReadAllLines(src).ToList();
            List<string> content = new List<string>();

            string language = X3Utils.GetLocalizedText("language_id");

            // Add all stream="1" and "2" entries
            List<X3Sector> stream1Sectors = X3Galaxy.Instance.Sectors.FindAll(item => item.StreamID == 1);
            List<X3Sector> stream2Sectors = X3Galaxy.Instance.Sectors.FindAll(item => item.StreamID == 2);

            // Add a header. This is not included in the template.
            content.Add("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            content.Add($"<language id=\"{language}\">");

            // Add the template data.
            content.AddRange(template);

            // Add stream 2
            content.Add("<page id=\"7\" stream=\"2\">");
            foreach(X3Sector sector in stream2Sectors)
            {
                content.Add($"\t<t id=\"{sector.SectorID}\" s=\"{sector.StreamLocation}\" l=\"{sector.StreamLength}\"/>");
            }
            content.Add("</page>");

            // Add stream 1
            content.Add("<page id=\"7\" stream=\"1\">");
            foreach (X3Sector sector in stream1Sectors)
            {
                content.Add($"\t<t id=\"{sector.SectorID}\" s=\"{sector.StreamLocation}\" l=\"{sector.StreamLength}\"/>");
            }
            content.Add("</page>");


            content.Add("</language>");
            File.WriteAllLines(dest, content);
        }

        /// <summary>
        /// Creates the gamestarts.txt file which determines the player starting location and the actual type of M4 ship they get when they start.
        /// This uses 2 templates:
        /// 
        /// The first is template_gamestarts.xml   which has the raw template we fill in.
        /// The second is the gamestart_ships.json.  The first template has something like this in it:   <ship typename="{ship.argon.m4}"/>
        /// You actually look up that token inside the gamestart_ships.json file, to produce the actual ship:  SS_SH_A_M4.
        /// I think the author intended to allow the user to change their starting ship class, but they never added the capability in the generator.
        /// 
        /// TODO:  In phase 2, we will add the ability in the U.I. for the user to select their starting ship class from the list in this json file.
        /// 
        ///   <gamestart id="201" name="1. {race.argon}" description="Please read carefully the in-game Encyclopedia. For information about ship commands, type 'i' when highlighting a command. Many gameplay values can be edited in X3\addon\t\9972-L044.xml." difficulty="M4" image="" plot="0">
        ///     <player name = "" species="" gender="" age="{galaxyname}"/>
        ///     <sector x = "{sectorx.argon}" y="{sectory.argon}"/>
        ///     <ship typename = "{ship.argon.m4}" />
        ///   </ gamestart >
        /// 
        ///   <gamestart id="201" name="1. Argon" description="Please read carefully the in-game Encyclopedia. For information about ship commands, type 'i' when highlighting a command. Many gameplay values can be edited in X3\addon\t\9972-L044.xml." difficulty="M4" image="" plot="0">
        ///     <player name = "" species="" gender="" age="ZEROHOUR-1"/>
        ///         <sector x = "6" y="9"/>
        ///         <ship typename = "SS_SH_A_M4" />
        ///   </ gamestart >
        /// 
        /// </summary>
        /// <param name="template1">mayhem_data\\template_gamestarts.xml</param>
        /// <param name="template2">mayhem_data\\gamestart_ships.json</param>
        /// <param name="dest">\\addon\\types\\gamestarts.txt</param>
        private void CreateGameStarts(string template1, string template2, string dest)
        {
            Random r = new Random();
            string source = File.ReadAllText(template1);

            // Pretty simple for now.  Just randomly put start locations inside the proper owned space.

            // For each race, find a random location and use it.
            foreach (ERace race in Enum.GetValues(typeof(ERace)))
            {
                if (!X3Utils.RaceIsMain((int)race)) continue;
                List<X3Sector> racesectors = X3Galaxy.Instance.Sectors.FindAll(item => item.r == (int)race);
                X3Sector rnd = racesectors[r.Next(racesectors.Count)];
                string tokena = string.Empty;
                string tokenb = string.Empty;
                switch(race)
                {
                    case ERace.Argon:
                        tokena = "{sectorx.argon}";
                        tokenb = "{sectory.argon}";
                        source = source.Replace("{ship.argon.m4}", "SS_SH_A_M4");
                        source = source.Replace("{race.argon}", X3Utils.GetLocalizedText("{Argon}"));
                        break;
                    case ERace.Boron:
                        tokena = "{sectorx.boron}";
                        tokenb = "{sectory.boron}";
                        source = source.Replace("{ship.boron.m4}", "SS_SH_B_M4");
                        source = source.Replace("{race.boron}", X3Utils.GetLocalizedText("{Boron}"));
                        break;
                    case ERace.Split:
                        tokena = "{sectorx.split}";
                        tokenb = "{sectory.split}";
                        source = source.Replace("{ship.split.m4}", "SS_SH_S_M4");
                        source = source.Replace("{race.split}", X3Utils.GetLocalizedText("{Split}"));
                        break;
                    case ERace.Paranid:
                        tokena = "{sectorx.paranid}";
                        tokenb = "{sectory.paranid}";
                        source = source.Replace("{ship.paranid.m4}", "SS_SH_P_M4");
                        source = source.Replace("{race.paranid}", X3Utils.GetLocalizedText("{Paranid}"));
                        break;
                    case ERace.Teladi:
                        tokena = "{sectorx.teladi}";
                        tokenb = "{sectory.teladi}";
                        source = source.Replace("{ship.teladi.m4}", "SS_SH_T_M4");
                        source = source.Replace("{race.teladi}", X3Utils.GetLocalizedText("{Teladi}"));
                        break;
                    case ERace.Terran:
                        tokena = "{sectorx.terran}";
                        tokenb = "{sectory.terran}";
                        source = source.Replace("{ship.terran.m4}", "SS_SH_TR_M4");
                        source = source.Replace("{race.terran}", X3Utils.GetLocalizedText("{Terran}"));
                        break;
                    case ERace.Xenon:
                        tokena = "{sectorx.xenon}";
                        tokenb = "{sectory.xenon}";
                        source = source.Replace("{ship.xenon.m4}", "SS_SH_X_M4");
                        source = source.Replace("{race.xenon}", X3Utils.GetLocalizedText("{Xenon}"));
                        break;
                }

                // ZMap 1.8.5: Fixed for Xenon Defect.
                int xindx;
                if (race == ERace.Terran)
                    xindx = 5;
                else if (race == ERace.Xenon)
                    xindx = 6;
                else
                    xindx = (int)race - 1;

                int x = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 0];
                int y = X3Galaxy.Instance.GalaxyCreationSettings.StartSectors[xindx, 1];
                if (x == -1 || y == -1)       // for this race, means generate a random start location.
                {
                    source = source.Replace(tokena, rnd.x.ToString());
                    source = source.Replace(tokenb, rnd.y.ToString());
                }
                else
                {
                    source = source.Replace(tokena, x.ToString());      // Gamestarts keeps 1-based location for sector start.
                    source = source.Replace(tokenb, y.ToString());
                }

                source = source.Replace("{galaxyname}", tbGalaxyName.Text);

            }

            File.WriteAllText(dest, source);
        }

        /// <summary>
        /// Sorry for the crudeness..  I'm sure there's a pretty algorithm for all this somewhere.
        /// 
        /// TODO:  At some point, start using \mayhem_data\fog_presets.json, which has 5 parameter sets governing fog levels and fade-out distances for 5 user controlled fog levels.
        /// 
        /// Creates the /addon/types/Tbackgrounds.txt file from a template.
        /// The actual background file has alterations in it for Space particles (on/off) and Fog level (variable)
        /// 
        /// template:   0;0;0;0;0;0;0;bluedistance2;1;1;1;25;50;10;5;5;0;5;0; 7;0;0;0;0;50000000;60000000; 0;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// -particles: 0;0;0;0;0;0;0;bluedistance2;1;1;1; 0; 0; 0;0;0;0;0;0; 0;0;0;0;0;45000000;50000000; 0;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// +particles: 0;0;0;0;0;0;0;bluedistance2;1;1;1; 0; 0; 0;0;0;0;0;0; 0;0;0;0;0;45000000;50000000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// +25% fog:   0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;15;0;0;0;0;38250000;42500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///     "       0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;17;0;0;0;0;37350000;41500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///     "       0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;13;0;0;0;0;39150000;43500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// +50% fog:   0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;17;0;0;0;0;37350000;41500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///     "       0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;20;0;0;0;0;36000000;40000000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// +75% fog:   0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;69;0;0;0;0;13950000;15500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///      "      0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;33;0;0;0;0;30150000;33500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///      "      0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;54;0;0;0;0;20700000;23000000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///      "      0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;50;0;0;0;0;22500000;25000000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///      "      0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;44;0;0;0;0;25200000;28000000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// +100% fog:  0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;41;0;0;0;0;26550000;29500000;80;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        /// 
        /// 
        /// +75% fog(np)0;0;0;0;0;0;0;bluedistance2;1;1;1; 1; 1; 1;1;1;1;1;1;43;0;0;0;0;25650000;28500000; 0;120;120;120;0;0;0;0;0;0;0;SS_BG_0;
        ///                                                                                                ^ # of particles. (index 26) Always set to 80 apparently.
        ///                                                                  ^^ Fog Density (index 19)
        ///                                                                                 ^^ Fadeout start, stop (divide by 500 to get in Meters, which is displayed in X3 editor) (index 24,25)
        ///     Default fadeout start = 450000000 (90k meters)
        ///     Default fadeout stop =  50000000 (100k meters)
        /// 
        ///     From Observations of what the old generator did:
        /// 
        ///    75% fog                                                                        
        ///    fog density min      25
        ///    fog density max      60
        ///    fadeout start min meters 36000
        ///                  max meters 66750       (multiply by 500 to get stored value)
        ///    
        ///    100% fog
        ///    fog density min      35
        ///    fog density max      79
        ///    fadeout start min meters 18900       (fadeout stop values are 11% higher than start values.)
        ///                  max meters 55200      
        ///    
        ///    fadeout stop is 11.1% more than start roughly.
        ///     
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        private void CreateBackgrounds(string src, string dest)
        {
            Random r = new Random();
            int fadeoutstartmin = 45000000;      // default for 0% fog.  (90k meters)
            int fadeoutstartmax = 50000000;
            int fadeoutstart, fadeoutend;
            // Load source template.
            List<string> srcTemplates = File.ReadAllLines(src).ToList();
            List<string> dstTemplates = new List<string>();

            int foglevelpercent = X3Galaxy.Instance.GalaxyCreationSettings.FogLevelPercent;

            int fogdensitymin = (35 * foglevelpercent) / 100;
            int fogdensitymax = (79 * foglevelpercent) / 100;

            if (foglevelpercent == 100)       // 100% fog
            {
                fadeoutstartmin = 18900 * 500;       
                fadeoutstartmax = 55200 * 500;
            }
            else if (foglevelpercent == 75)  // 75% fog
            {
                fadeoutstartmin = 36000 * 500;
                fadeoutstartmax = 75000 * 500;
            }
            else if (foglevelpercent == 50)
            {
                fadeoutstartmin = 54000 * 500;
                fadeoutstartmax = 80000 * 500;
            }
            else if (foglevelpercent == 25)
            {
                fadeoutstartmin = 72000 * 500;
                fadeoutstartmax = 85500 * 500;
            }

            // Write this odd value to top of backgrounds file.  Not sure what the 25 stands for.
            // 25;248;
            dstTemplates.Add($"25;{srcTemplates.Count};");

            // Alter entries
            foreach (string srcTemplate in srcTemplates)
            {
                string[] entries = srcTemplate.Split(';');

                // ZMap 2.2: Space particles are independent of nebula dust.
                entries[26] = X3Galaxy.Instance.GalaxyCreationSettings.UseParticles ? "80" : "0";

                // The original template determines whether dust is supported.
                int templateDustInstances = int.Parse(entries[19]);
                bool enableDust = foglevelpercent > 0 && templateDustInstances > 0;
                bool hasSelectableDustPart = false;

                // Preserve the template's original dust-body weights.
                for (int x = 11; x < 19; x++)
                {
                    int templateWeight = int.Parse(entries[x]);

                    if (!enableDust || templateWeight <= 0)
                    {
                        entries[x] = "0";
                    }
                    else
                    {
                        hasSelectableDustPart = true;
                    }
                }

                // Randomize density only for dust-enabled backgrounds.
                entries[19] = enableDust && hasSelectableDustPart
                    ? r.Next(fogdensitymin, fogdensitymax).ToString()
                    : "0";


                if (foglevelpercent == 0)
                {
                    fadeoutstart = fadeoutstartmin;
                    fadeoutend = fadeoutstartmax;
                }
                else
                {
                    fadeoutstart = r.Next(fadeoutstartmin, fadeoutstartmax);
                    fadeoutend = (int)(fadeoutstart * 1.1);
                }

                // Preserve existing fog fadeout behavior.
                entries[24] = fadeoutstart.ToString();
                entries[25] = fadeoutend.ToString();

                // Save the generated background.
                dstTemplates.Add(string.Join(";", entries));
            }

            // Save dstTemplates to dest file.
            File.WriteAllLines(dest, dstTemplates);
        }

        private void btnRandomName_Click(object sender, EventArgs e)
        {
            tbGalaxyName.Text = X3Utils.GetRandomString(3) + "-" + X3Utils.GetRandomNumber(2);
        }


        /// <summary>
        /// Sets selected Galaxy as active and copies galaxy and save file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetGalaxyAsActive_Click(object sender, EventArgs e)
        {
            if (lbAvailableGalaxies.SelectedItem == null)
            {
                Message("Please select a map to activate.");
                return;
            }

            string selection = lbAvailableGalaxies.SelectedItem.ToString();
            bool isActiveMap = selection.Contains(ActiveString);
            bool success = ActivateMap(selection, isActiveMap);
            LoadSavedGalaxiesList();

            if (success)
            {
                Message($"{GetActiveMap()} is now the Active map in Mayhem 3.\r\nYou can now run Mayhem 3 to play this map.");
            }
        }

        /// <summary>
        /// Removes the obsolete standalone Mayhem Galaxy Generator.
        /// </summary>
        private void DisableLegacyGalaxyGenerator()
        {
            string legacyGenerator = Path.Combine(m_CurX3RootFolder, "Mayhem Galaxy Generator.exe");
            if (File.Exists(legacyGenerator))
            {
                File.Delete(legacyGenerator);
            }
        }

        /// <summary>
        /// Disables the legacy sound file.
        /// </summary>
        private void DisableLegacySoundFile()
        {
            string language = X3Utils.GetLocalizedText("language_id_file");
            if (string.IsNullOrWhiteSpace(language))
            {
                MessageBox.Show(
                    "Unable to determine the speech file language.",
                    "Speech Index Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string packedFile = Path.Combine(
                m_CurX3RootFolder, "addon", "mov", $"000{language}.pck");

            if (!File.Exists(packedFile))
                return;

            try
            {
                // Archive the original outside the active game directory.
                string backupFolder = Path.Combine(
                    m_CurX3RootFolder, "ZMap_Backups", "Speech");

                Directory.CreateDirectory(backupFolder);

                // A timestamp identifies the backup.
                string backupName = $"000{language}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.pck";
                string backupFile = Path.Combine(backupFolder, backupName);
                File.Move(packedFile, backupFile);

                Message($"Archived legacy speech file {Path.GetFileName(packedFile)}.");
                Message($"Backup: {backupFile}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not disable the legacy speech file:\r\n{ex.Message}\r\n\r\n" +
                    "The galaxy files were copied, but the board computer may " +
                    "continue announcing the original sector names.",
                    "Speech Index Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }


        /// <summary>
        /// Activates a saved galaxy and, when switching galaxies, backs up/restores its savegames.
        /// A successful result means file operations completed, not that the game was tested.
        /// </summary>
        private bool ActivateMap(string mapname, bool isActive)
        {
            try
            {
                mapname = mapname.Replace(ActiveString, string.Empty);
                string source = Path.Combine(m_CurX3RootFolder, "mayhem_galaxies", mapname);

                // Check the source before touching any savegames.
                if (!Directory.Exists(source))
                {
                    MessageBox.Show($"Galaxy folder not found: {source}");
                    return false;
                }

                string settingsFile = Path.Combine(source, "GalaxySettings.json");
                string universeFile = Path.Combine(source, "addon", "maps", "x3_universe.xml");

                // Check for essential files before proceeding with activation.
                if (!File.Exists(settingsFile) || !File.Exists(universeFile))
                {
                    MessageBox.Show(
                        $"Galaxy '{mapname}' is missing essential files and cannot be activated.",
                        "Invalid Galaxy",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                // Validate activation.
                X3Galaxy savedGalaxy = new X3Galaxy(0, 0);
                savedGalaxy.LoadFrom_X3_Universe_XML(universeFile);

                bool hasUnknownEnclave = savedGalaxy.Sectors.Any(sector => (ERace)sector.r == ERace.Unknown && sector.Gates.Count == 1);

                X3Galaxy.GateNetworkReport network = savedGalaxy.AnalyzeGateNetwork();

                if (!hasUnknownEnclave || network.GatelessSectors > 0 || network.OutsideLargestNetwork > 0 || network.MissingDestinations > 0 || network.MissingReturnGates > 0)
                {
                    MessageBox.Show(
                        "The saved galaxy failed validation and cannot be activated.", "Invalid Galaxy",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }


                if (!isActive)
                {
                    string activeMap = GetActiveMap();

                    if (!string.IsNullOrEmpty(activeMap))
                    {
                        string backupFolder = Path.Combine(m_X3SaveFolder, "backup_" + activeMap);
                        Directory.CreateDirectory(backupFolder);

                        string[] existingSaves = Directory.GetFiles(m_X3SaveFolder, "*.sav");

                        // Only delete the current saves after every backup copy succeeds.
                        foreach (string save in existingSaves)
                            File.Copy(save, Path.Combine(backupFolder, Path.GetFileName(save)), true);

                        foreach (string save in existingSaves)
                            File.Delete(save);
                    }

                    string savedBackup = Path.Combine(m_X3SaveFolder, "backup_" + mapname);
                    if (Directory.Exists(savedBackup))
                    {
                        foreach (string save in Directory.GetFiles(savedBackup, "*.sav"))
                            File.Copy(save, Path.Combine(m_X3SaveFolder, Path.GetFileName(save)), true);
                    }
                }

                Message($"Copying {mapname} files to {m_CurX3RootFolder}");
                DisableLegacyGalaxyGenerator();
                X3Utils.CopyFolder(source, m_CurX3RootFolder);
                DisableLegacySoundFile(); // Warns separately if the packed speech file cannot be archived.
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not activate galaxy '{mapname}': {ex.Message}\r\n\r\n" +
                    "Activation may be incomplete. Check the game installation and save files before playing.",
                    "Galaxy Activation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }


        private void lbAvailableGalaxies_SelectedValueChanged(object sender, EventArgs e)
        {
            if (lbAvailableGalaxies.SelectedItem == null) return;
            tbGalaxyName.Text = lbAvailableGalaxies.SelectedItem.ToString().Replace(ActiveString, "");
        }

        private void btnDeleteGalaxy_Click(object sender, EventArgs e)
        {
            if (lbAvailableGalaxies.SelectedItem == null)
            {
                Message("Please select a map to delete.");
                return;
            }

            string selectedGalaxyName = lbAvailableGalaxies.SelectedItem.ToString().Replace(ActiveString, string.Empty);

            // Do not delete the currently active galaxy.
            if (string.Equals(selectedGalaxyName, GetActiveMap(),
                StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "You cannot delete the active galaxy. Activate another galaxy first.",
                    "Cannot Delete Galaxy",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string pathToDelete = Path.Combine(
                m_CurX3RootFolder, "mayhem_galaxies", selectedGalaxyName);

            if (!Directory.Exists(pathToDelete))
            {
                Message($"Galaxy folder does not exist: {pathToDelete}");
                return;
            }

            // Ask if they are sure first!
            string msg = X3Utils.GetLocalizedText("{DelMapWarning}");
            string warning = X3Utils.GetLocalizedText("{Warning}");
            if (MessageBox.Show(msg + selectedGalaxyName + "?", warning, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Directory.Delete(pathToDelete, true);
            }
            catch (Exception ex)
            {
                Message($"Error deleting galaxy {selectedGalaxyName}: {ex.Message}");
                return;
            }

            Message($"Successfully deleted galaxy {selectedGalaxyName}.");

            // Preserve existing savegame backups.
            string backupFolder = Path.Combine(m_X3SaveFolder, "backup_" + selectedGalaxyName);

            if (Directory.Exists(backupFolder))
                Message($"Savegame backup preserved at: {backupFolder}");

            LoadSavedGalaxiesList();
        }
    }
}
