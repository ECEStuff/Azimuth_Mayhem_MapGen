// Shared X3 conversions, localization lookup, settings loading and file helpers used by the editor and generators.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using System.Reflection;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace Azimuth
{
    public class X3Utils
    {
        private static Random m_Rand = new Random();


        /// <summary>
        /// Creates a "unique as possible' integer for any given input string.  X3 appears to use 6 digit filenames, but we're going to try 8 digits.
        /// It is understood that it may produce a duplicate integer for 2 different string inputs.  
        /// In that rare case, we simply will ignore that music file.  oh well.  Blame x3 for using int's for music file names.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Transmute(string input)
        {
            string root = Path.GetFileNameWithoutExtension(input);

            // See if it's already in an integer format.  No need to convert those.
            int result;
            if (int.TryParse(root, out result))
            {
                return root;
            }

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Take the first 4 bytes and convert to int (little endian)
                int rawHash = BitConverter.ToInt32(hash, 0);
                int sixDigitHash = Math.Abs(rawHash % 1_000_000);
                int eightDigitHash = Math.Abs(rawHash % 100_000_000);

                return eightDigitHash.ToString();
            }
        }

        public static bool IsSectorMusicTrack(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // IDs 15+ are sector music. ID 12 (X:BTF station building theme) is permitted as an exception.
            return int.TryParse(name.Trim(), out int trackId) && (trackId >= 15 || trackId == 12);
        }

        public static bool RaceIsMain(int race)
        {
            // Add-Ons 1.8.1: Changed bound from 1-5 to 1-6 (Xenon)
            if ((race > 0 && race < 7) || (race == (int)ERace.Terran)) 
                return true;
            return false;
        }
        
        /// <summary>
        /// Sets the .Text property of all control children using the passed localization dictionary.  Recurses if there are containers with other controls.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        public static void SetControlChildrenText(Control parent)
        {
            foreach (Control ctl in parent.Controls)
            {
                var form = ctl.FindForm();
                ctl.Text = GetLocalizedText(form?.Name + "." + ctl.Name) ?? GetLocalizedText(ctl.Name) ?? ctl.Text;
                if (ctl is ToolStrip strip)
                    LocalizeItems(strip.Items, form?.Name);
                if (ctl.HasChildren)
                {
                    SetControlChildrenText(ctl);    // recurse.
                }
            }
        }

        private static void LocalizeItems(ToolStripItemCollection items, string scope)
        {
            foreach (ToolStripItem item in items)
            {
                string text = GetLocalizedText(scope + "." + item.Name);
                if (text != null) item.Text = text;
                if (item is ToolStripDropDownItem menu)
                    LocalizeItems(menu.DropDownItems, scope);
            }
        }

        public static string HelpFile(string document = "HelpMain")
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string language = GetLocalizedText("language_id_file");
            int code;
            if (int.TryParse(language, out code))
            {
                string localized = Path.Combine(root, document + "_" + language + ".txt");
                if (File.Exists(localized)) return localized;
            }
            return Path.Combine(root, document + ".txt");
        }

        public static void LoadLocalization()
        {
            Dictionary<string, string> catalog;
            using (var stream = typeof(X3Utils).Assembly.GetManifestResourceStream("Azimuth.English.json"))
            using (var reader = new StreamReader(stream))
                catalog = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localizations.json");
            if (File.Exists(path))
            {
                var active = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
                foreach (var entry in active)
                    if (!string.IsNullOrEmpty(entry.Value))
                        catalog[entry.Key] = entry.Value;
            }
            X3Galaxy.Instance.LocalizationDictionary = catalog;
        }

        public static string Ui(string key, params object[] values)
        {
            string text = GetLocalizedText(key) ?? key;
            return values.Length == 0 ? text : string.Format(text, values);
        }

        public static string GetLocalizedText(string token)
        {
            Dictionary<string, string> locz = X3Galaxy.Instance.LocalizationDictionary;

            if (locz.ContainsKey(token))
            {
                string val = locz[token];
                if (val != string.Empty)
                {
                    return locz[token];
                }
            }
            return null;
        }

        /// <summary>
        /// Update version number inside AssemblyInfo.cs for this to work.
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {        
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return string.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);
        }

        public static string GetSettingsMap(string path)
        {
            GalaxySettings gs = X3Utils.LoadSettingsFile(path);
            return gs.GalaxyName;
        }

        public static GalaxySettings LoadSettingsFile(string path)
        {
            if (File.Exists(path))
            {
                string settings = File.ReadAllText(path);
                GalaxySettings gs = JsonConvert.DeserializeObject<GalaxySettings>(settings);

                if (gs == null)
                    return new GalaxySettings();

                // Azimuth 1.8.5: Migrate old 6x2 StartSectors arrays to 7x2 for Xenon Defect.
                if (gs.StartSectors == null ||
                    gs.StartSectors.GetLength(0) != 7 ||
                    gs.StartSectors.GetLength(1) != 2)
                {
                    int[,] oldStartSectors = gs.StartSectors;
                    int[,] newStartSectors = new int[7, 2];

                    // Default all starts to random.
                    for (int x = 0; x < 7; x++)
                    {
                        newStartSectors[x, 0] = -1;
                        newStartSectors[x, 1] = -1;
                    }

                    // Preserve whatever valid old entries exist.
                    if (oldStartSectors != null)
                    {
                        int rows = Math.Min(oldStartSectors.GetLength(0), 7);
                        int columns = Math.Min(oldStartSectors.GetLength(1), 2);

                        for (int x = 0; x < rows; x++)
                        {
                            for (int y = 0; y < columns; y++)
                            {
                                newStartSectors[x, y] = oldStartSectors[x, y];
                            }
                        }
                    }

                    gs.StartSectors = newStartSectors;
                }

                return gs;
            }

            return new GalaxySettings();
        }

        public static string GetXMLAttribute(string src, string attr)
        {
            string searchfor = $" {attr}=";
            int attrindex = src.IndexOf(searchfor, StringComparison.Ordinal);

            if (attrindex == -1)
                throw new Exception(X3Utils.Ui("X3Utils.AttributeNotFoundIn", attr, src));

            int start = attrindex + searchfor.Length;

            if (start >= src.Length || src[start] != '"')
                throw new Exception(X3Utils.Ui("X3Utils.AttributeIsNotProperlyQuotedIn", attr, src));

            int end = src.IndexOf('"', start + 1);

            if (end == -1)
                throw new Exception(X3Utils.Ui("X3Utils.AttributeIsNotProperlyQuotedIn", attr, src));

            return src.Substring(start + 1, end - start - 1);
        }

        /// <summary>
        /// k = position of digit you want.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int PlaceDigitValue(int k, int num)
        {
            return (int)(num / Math.Pow(10, k)) % 10; ;
        }

        public static ERace GetRaceForCompany(ECompany c)
        {
            switch (c)
            {
                case ECompany.None:
                    return ERace.Unknown;
                case ECompany.Markus:
                    return ERace.Argon;
                case ECompany.Oceana:
                    return ERace.Boron;
                case ECompany.Pontifex:
                    return ERace.Paranid;
                case ECompany.Thurok:
                    return ERace.Split;
                case ECompany.Salecrest:
                    return ERace.Teladi;
                case ECompany.Industritech:
                    return ERace.Terran;
                default:
                    throw new Exception(X3Utils.Ui("X3Utils.InvalidCompanyInGetRaceForCompany"));
            }
        }

        public static ECompany GetCompanyForRace(ERace r)
        {
            switch (r)
            {
                case ERace.Argon:
                    return ECompany.Markus;
                case ERace.Boron:
                    return ECompany.Oceana;
                case ERace.Split:
                    return ECompany.Thurok;
                case ERace.Paranid:
                    return ECompany.Pontifex;
                case ERace.Teladi:
                    return ECompany.Salecrest;
                case ERace.Terran:
                    return ECompany.Industritech;
                default:
                    throw new Exception(X3Utils.Ui("X3Utils.InvalidRaceInGetCompanyForRace"));
            }
        }

        // Gets any other food type than the one passed in.
        public static int GetAnotherFoodType(int foodtype)
        {
            int next = foodtype;
            while (next == foodtype)
            {
                next = m_Rand.Next(0, 4);
            }
            return next;
        }

        public static string GetFormattedCompanyName(int index)
        {
            switch (index)
            {
                case -1:
                    return @"\033A" + GetLocalizedText("{none}");
                case 0:
                    return @"\033C" + GetLocalizedText("{Markus}");
                case 1:
                    return @"\033G" + GetLocalizedText("{Oceana}");
                case 2:
                    return @"\033O" + GetLocalizedText("{Pontifex}");
                case 3:
                    return @"\033M" + GetLocalizedText("{Thurok}");
                case 4:
                    return @"\033Y" + GetLocalizedText("{Salecrest}");
                case 5:
                    return @"\033C" + GetLocalizedText("{Industritech}");
                default:
                    throw new Exception(X3Utils.Ui("X3Utils.InvalidCompanyNameIndexInGetFormattedCompanyName", index));
            }
        }


        public static string GetFormattedRaceName(int index)
        {
            string race = GetLocalizedText("{SaveLoadFormTitle}");

            switch (index)
            {
                case 0:
                    return @"\033A" + GetLocalizedText("{none}"); 
                case 1:
                    return @"\033C" + GetLocalizedText("{Argon}"); 
                case 2:
                    return @"\033G" + GetLocalizedText("{Boron}");
                case 3:
                    return @"\033M" + GetLocalizedText("{Split}");
                case 4:
                    return @"\033O" + GetLocalizedText("{Paranid}");
                case 5:
                    return @"\033Y" + GetLocalizedText("{Teladi}");
                case 18:
                    return @"\033C" + GetLocalizedText("{Terran}");
                default:
                    throw new Exception(X3Utils.Ui("X3Utils.InvalidRaceNameIndexInGetFormattedRaceName", index));
            }
        }

        public static string GetFormattedFoodName(EFood food)
        {
            switch(food)
            {
                case EFood.Fish:
                    return GetLocalizedText("{Fish}");
                case EFood.Fruit:
                    return GetLocalizedText("{Fruit}");
                case EFood.Meat:
                    return GetLocalizedText("{Meat}");
                case EFood.Vegetables:
                    return GetLocalizedText("{Vegetables}");
                default:
                    throw new Exception(X3Utils.Ui("X3Utils.InvalidFoodNameInGetFormattedFoodName", food));
            }

        }

        public static string GetFormattedAbnormalSignals(int val)
        {
            // 0 - green.  \033G
            // 1-20 white - no formatting
            // 21-49 yellow \033Y
            // 49+ Redish.  \033R
            if (val == 0)
            {
                return @"\033G" + val;
            }
            else if (val < 20)
            {
                return val.ToString();
            }
            else if (val < 50)
            {
                return @"\033Y" + val;
            }
            else return @"\033R" + val;
        }

        public static string GetFormattedTerranMemory(int val)
        {
            // 0 - white.  \033Ano
            // 1 - blue.  \033Byes

            if (val == 0)
            {
                return @"\033A" + GetLocalizedText("{no}");
            }

            else return @"\033B" + GetLocalizedText("{yes}");
        }

        public static string GetFormattedMaxPopulation(int val)
        {

            if (val > 95)
            {
                return @"\033W" + val;
            }

            else return val.ToString();
        }

        public static string GetFormattedStationSupport(int val)
        {

            if (val > 10)
            {
                return @"\033W" + val;
            }

            else return val.ToString();
        }

        public static string GetFormattedResearchRate(int val)
        {
            if (val > 70)
            {
                return @"\033W" + val;
            }
            else return val.ToString();
        }

        /// <summary>
        /// For the following XML, this would retrive "Family Whi"
        /// <t id="1021103">Family Whi</t>
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string GetXMLValue(string src)
        {
            int start = src.IndexOf('>') + 1;
            src = src.Substring(start);
            int end = src.IndexOf('<');
            src = src.Remove(end);
            return src;
        }

        public static string GetSetting(string settingName)
        {
            List<string> settings = File.ReadAllLines("Settings.txt").ToList();

            foreach (string setting in settings)
            {
                if (setting.StartsWith(settingName))
                {
                    return setting.Replace(settingName + "|", string.Empty);
                }
            }
            throw new Exception(X3Utils.Ui("X3Utils.SettingNotFound", settingName));
        }

        public static void EnsureSetting(string settingName, string defaultValue)
        {
            List<string> settings = File.ReadAllLines("Settings.txt").ToList();

            if (settings.Any(line => line.StartsWith(settingName + "|", StringComparison.Ordinal)))
                return;

            settings.Add(settingName + "|" + defaultValue);
            File.WriteAllLines("Settings.txt", settings);
        }

        public static void SaveSetting(string settingName, string value)
        {
            List<string> settings = File.ReadAllLines("Settings.txt").ToList();

            int index = settings.FindIndex(item => item.StartsWith(settingName + "|"));
            if (index == -1) throw new Exception(X3Utils.Ui("X3Utils.SettingNotFoundInSettingsTxt", settingName));

            settings[index] = settingName + "|" + value;
            File.WriteAllLines("Settings.txt", settings);
        }

        // Suggest the closest existing folder to the standard X3AP save location.
        // A user-selected X3Save path takes precedence in the Form1 browse handler.
        public static string FindSuggestedX3SaveDirectory()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(documents))
                return string.Empty;

            DirectoryInfo documentsDirectory = new DirectoryInfo(documents);
            DirectoryInfo directory = new DirectoryInfo(
                Path.Combine(documentsDirectory.FullName, "Egosoft", "X3AP", "save"));

            // Walk upward from the expected save location until an existing folder is found.
            // This also handles a new install whose X3AP or save subfolder does not exist yet.
            while (directory != null)
            {
                if (directory.Exists)
                    return directory.FullName;

                if (string.Equals(directory.FullName, documentsDirectory.FullName,
                    StringComparison.OrdinalIgnoreCase))
                    break;

                directory = directory.Parent;
            }

            return string.Empty;
        }


        public static void CopyFolder(string sourceFolder, string dest)
        {
            if (Directory.Exists(sourceFolder))
            {
                DirectoryInfo dinfo = new DirectoryInfo(sourceFolder);
                foreach (FileSystemInfo f in dinfo.GetFileSystemInfos())
                {
                    string destName = Path.Combine(dest, f.Name);

                    if (f is FileInfo)
                    {
                        File.Copy(f.FullName, destName, true);  // true = overwrite                  
                    }
                    else
                    {
                        // If it is a folder...
                        if (!Directory.Exists(destName))
                        {
                            Directory.CreateDirectory(destName);
                        }
                        CopyFolder(f.FullName, destName);    // Recurse
                    }
                }
            }
            else
            {
                throw new Exception(X3Utils.Ui("X3Utils.CopyFolderSourceFolderDoesNotExist", sourceFolder));
            }
        }

        public static string GetRandomString(int stringlen)
        {
            int randValue;
            string str = "";
            char letter;
            for (int i = 0; i < stringlen; i++)
            {

                // Generating a random number.
                randValue = m_Rand.Next(0, 26);

                // Generating random character by converting
                // the random number into character.
                letter = Convert.ToChar(randValue + 65);

                // Appending the letter to string.
                str = str + letter;
            }
            return str;
        }
        public static string GetRandomNumber(int stringlen)
        {
            int randValue;
            string str = "";
            for (int i = 0; i < stringlen; i++)
            {
                // Generating a random number.
                randValue = m_Rand.Next(10);

                // Appending the letter to string.
                str = str + randValue;
            }
            return str;
        }
    }
}