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

namespace X3_Mayhem_Galaxy_Generator
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
                ctl.Text = GetLocalizedText(ctl.Name) ?? ctl.Text;
                if (ctl.HasChildren)
                {
                    SetControlChildrenText(ctl);    // recurse.
                }
            }
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

                // ZMap 1.8.5: Migrate old 6x2 StartSectors arrays to 7x2 for Xenon Defect.
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
                throw new Exception($"Attribute {attr} not found in: {src}");

            int start = attrindex + searchfor.Length;

            if (start >= src.Length || src[start] != '"')
                throw new Exception($"Attribute {attr} is not properly quoted in: {src}");

            int end = src.IndexOf('"', start + 1);

            if (end == -1)
                throw new Exception($"Attribute {attr} is not properly quoted in: {src}");

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
                    throw new Exception("Invalid Company in GetRaceForCompany()");
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
                    throw new Exception("Invalid Race in GetCompanyForRace()");
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
                    throw new Exception($"Invalid Company Name Index {index} in GetFormattedCompanyName()");
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
                    throw new Exception($"Invalid Race Name Index {index} in GetFormattedRaceName()");
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
                    throw new Exception($"Invalid food name {food} in GetFormattedFoodName()");
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
            throw new Exception($"Setting {settingName} not found.");
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
            if (index == -1) throw new Exception($"Setting {settingName} not found in Settings.txt");

            settings[index] = settingName + "|" + value;
            File.WriteAllLines("Settings.txt", settings);
        }

        public static string GetDefaultSaveGameFolder()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (folderPath == string.Empty)
            {
                return string.Empty;
            }

            string saveFolder = Path.Combine(folderPath, "Egosoft", "X3AP", "save");
            if (Directory.Exists(saveFolder))
            {
                return saveFolder;
            }

            string egosoftFolder = Path.Combine(folderPath, "Egosoft");
            if (Directory.Exists(egosoftFolder))
            {
                return egosoftFolder;
            }

            return folderPath;
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
                throw new Exception($"CopyFolder:  source folder {sourceFolder} does not exist.");
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