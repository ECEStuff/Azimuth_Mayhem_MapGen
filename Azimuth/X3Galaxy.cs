using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Azimuth
{
    // A Sector contains:
    // 1 Background
    // 1 Sun
    // 1 or more Planets
    // 1-4 Gates
    // 1 or more Asteroids
    // 1 or more Signs (specials)
   
    /// <summary>
    /// Universe encapsulates the structure of a universe as well as the ability to read/parse a x3_universe.xml file, and to write
    /// both the x3_universe.xml and the 00749.bod file, both of which are used by X3.
    /// The classes are similar enough in nature and composition that I stuffed them all in this one file for ease of editing and navigation.  Separate them if you are anal.
    /// </summary>
    public class X3Galaxy
    {
        private static readonly X3Galaxy m_Instance = new X3Galaxy();
        public int MaxX;
        public int MaxY;
        public List<X3Sector> Sectors = new List<X3Sector>();
        private Random m_Rand = new Random();
        public GalaxySettings GalaxyCreationSettings = new GalaxySettings();
        public Dictionary<string, string> LocalizationDictionary = new Dictionary<string, string>();
        public int MinimumSectorCount => GalaxyCreationSettings.MinimumCount;
        public int MaximumSectorCount => Math.Min(MaxX * MaxY, GalaxyCreationSettings.MaximumCount);
        public bool HasUnsavedChanges;
        public bool HasUnsavedManualEdits;

        public static bool HasUnknownEnclave()
        {
            foreach (X3Sector sector in m_Instance.Sectors)
            {
                if ((ERace)sector.r == ERace.Unknown)
                {
                    if (sector.Gates.Count == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public GateNetworkReport AnalyzeGateNetwork()
        {
            GateNetworkReport result = new GateNetworkReport();

            // Index by coordinates to avoid repeatedly searching all sectors.
            Dictionary<Tuple<int, int>, X3Sector> sectorLookup =
                new Dictionary<Tuple<int, int>, X3Sector>();
            Dictionary<X3Sector, HashSet<X3Sector>> connections =
                new Dictionary<X3Sector, HashSet<X3Sector>>();

            foreach (X3Sector sector in Sectors)
            {
                sectorLookup[Tuple.Create(sector.x, sector.y)] = sector;
                connections[sector] = new HashSet<X3Sector>();

                if (sector.Gates.Count == 0)
                    result.GatelessSectors++;
            }

            // Only reciprocal connections to existing sectors are traversable.
            foreach (X3Sector sector in Sectors)
            {
                foreach (X3Gate gate in sector.Gates)
                {
                    X3Sector destination;
                    if (!sectorLookup.TryGetValue(
                        Tuple.Create(gate.gx, gate.gy), out destination))
                    {
                        result.MissingDestinations++;
                        continue;
                    }

                    if (destination == sector ||
                        !destination.Gates.Any(returnGate =>
                            returnGate.gx == sector.x && returnGate.gy == sector.y))
                    {
                        result.MissingReturnGates++;
                        continue;
                    }

                    connections[sector].Add(destination);
                    connections[destination].Add(sector);
                }
            }

            // One traversal per connected component: O(sectors + valid gates).
            HashSet<X3Sector> visited = new HashSet<X3Sector>();
            int largestNetwork = 0;

            foreach (X3Sector sector in Sectors)
            {
                if (!visited.Add(sector))
                    continue;

                result.ConnectedNetworks++;
                int networkSize = 0;
                Queue<X3Sector> queue = new Queue<X3Sector>();
                queue.Enqueue(sector);

                while (queue.Count > 0)
                {
                    X3Sector current = queue.Dequeue();
                    networkSize++;

                    foreach (X3Sector neighbor in connections[current])
                    {
                        if (visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                largestNetwork = Math.Max(largestNetwork, networkSize);
            }

            result.OutsideLargestNetwork = Sectors.Count - largestNetwork;
            return result;
        }

        private X3Galaxy() { }

        public static X3Galaxy Instance
        {
            get
            {
                return m_Instance;
            }
        }

        public void SetSize(int maxx, int maxy)
        {
            MaxX = maxx;
            MaxY = maxy;
        }

        public X3Galaxy(int maxx, int maxy)
        {
            MaxX = maxx;
            MaxY = maxy;
        }



        /// <summary>
        /// Systems have just been generated.
        /// Here, we are going through each Sector that was Generated and assigning it a Sector Name and an available Name/Sound combination.
        /// 
        /// </summary>
        /// <param name="masterSystemList"></param>
        public void AssignSystemNamesAndSoundToGeneratedSystems(List<SystemListItem> masterSystemList)
        {
            List<string> usedNames = new List<string>();

            foreach (X3Sector sector in X3Galaxy.Instance.Sectors)
            {
                string sectorx = (sector.x + 1).ToString("00");         // 01, 02 etc.
                string sectory = (sector.y + 1).ToString("00");
                string sectorId = "102" + sectory + sectorx;             // 1020101, 1020301 etc.
                sector.SectorID = sectorId;

                // Find the next available one from the master system list that we have not used yet.
                SystemListItem nextAvailable = masterSystemList.Find(item => !usedNames.Contains(item.Name));
                if (nextAvailable == null)
                {
                    throw new Exception("Couldn't find another available system name to use from the sector_names templates");
                }

                // Store it all into the sector for ease of retrieval when creating the output files.
                sector.SectorName = nextAvailable.Name;
                sector.StreamID = nextAvailable.StreamID;
                sector.StreamLocation = nextAvailable.StreamLocation;
                sector.StreamLength = nextAvailable.StreamLength;

                usedNames.Add(sector.SectorName);
            }
        }



        public void CreateX3UniverseFile(string file)
        {
            List<string> contents = new List<string>();

            contents.Add("<?xml version = \"1.0\" encoding = \"UTF-8\" ?>");
            contents.Add("<universe>");
            foreach (X3Sector sector in Sectors)
            {
                sector.CreateXml(contents);
            }
            contents.Add("</universe>");

            File.WriteAllLines(file, contents);
        }


        public void LoadFrom_X3_Universe_Settings(string path)
        {
            GalaxyCreationSettings = X3Utils.LoadSettingsFile(path);
            if (GalaxyCreationSettings == null)
            {
                // If loading an old galaxy map, we will get here.  We just accept that we can't load old settings that don't exist.
            }
        }

        /// <summary>
        /// Unfortunately, because of the screwey nature of the nodes for a system (they represent totally different objects depending on type), we can't easily use an Xml deserializer,
        /// so we need to do this the hard way.
        /// </summary>
        /// <returns></returns>
        public void LoadFrom_X3_Universe_XML(string path)
        {
            XmlDocument doc = new XmlDocument();

            // project default is an x3_universe example file, generated by original Mayhem 3 galaxy generator.  197 sectors.
            doc.Load(path);
            Sectors.Clear();

            foreach (XmlNode sectornode in doc.DocumentElement.ChildNodes)
            {
                Sectors.Add(new X3Sector(sectornode));
            }

            MaxX = Sectors.Max(item => item.x) + 1;
            MaxY = Sectors.Max(item => item.y) + 1;
        }

        public void Load00044_XML(string path)
        {
            int streamID = 0;
            // Here, we're interested in loading the bottom 2 sections containing stream 1 and stream 2 sound pointers/lengths for the systems.
            // Find sector by matching the ID.
            // These are loaded into the matching sector.
            // 1.  Must set Sector's StreamID to 1 or 2.
            // 2.  Must set Sector's Stream Location and Stream Length

            List<string> content = File.ReadAllLines(path).ToList();

            foreach (string line in content)
            {
                string trimmedline = line.TrimStart();
                trimmedline = trimmedline.TrimStart('\t');

                if (trimmedline.StartsWith("<page id=\"7\" stream=\"1\">"))
                {
                    streamID = 1;
                    continue;
                }

                if (trimmedline.StartsWith("<page id=\"7\" stream=\"2\">"))
                {
                    streamID = 2;
                    continue;
                }

                if (streamID == 0) continue;
                if (trimmedline.StartsWith("</")) continue;
                if (trimmedline == "") continue;

                string id = X3Utils.GetXMLAttribute(trimmedline, "id");
                string s = X3Utils.GetXMLAttribute(trimmedline, "s");
                string l = X3Utils.GetXMLAttribute(trimmedline, "l");

                int xloc = int.Parse(id.Substring(5, 2));       // 1 - based locations.
                int yloc = int.Parse(id.Substring(3, 2));
                // Find the sector at this location and set it's Id and Name from the data we just retrieved.
                X3Sector sector = Sectors.Find(item => item.x == xloc - 1 && item.y == yloc - 1);
                if (sector == null)
                    throw new Exception($"Error reading 00044 file. System located at {xloc}:{yloc} not found in the loaded map.");

                sector.StreamID = streamID;
                sector.StreamLocation = int.Parse(s);
                sector.StreamLength = int.Parse(l);
            }
        }

        /// <summary>
        /// Contains sector options chosen by the user, and generated System stats such as Max Population.
        /// </summary>
        /// <param name="path"></param>
        public void Load9970_XML(string path)
        {
            // This beast is going to be far easier to parse manually than with an xml doc..  
            List<string> content = File.ReadAllLines(path).ToList();
            int section = 0;

            foreach (string line in content)
            {
                string trimmedline = line.TrimStart();
                trimmedline = trimmedline.TrimStart('\t');

                if (trimmedline.StartsWith("<page id=\"7\"") || trimmedline.StartsWith("<page id=\"19\"") || trimmedline.StartsWith("<page id=\"9970\""))
                {
                    section++;
                    continue;
                }
                if (trimmedline.StartsWith("</")) continue;
                if (trimmedline == "") continue;
                if (section == 0) continue;
                else if (section == 1)
                {
                    // Note:  This section has locations in format YYXX, which is normal. 
                    // <t id="1020101">Lasting Vengeance</t>   List of system ID' and names.  These names should match those we found in the MasterSystemList list which is what we actually use.
                    // For this section, we're just going to look up the Id in the sector list and make sure the name matches.
                    string id = X3Utils.GetXMLAttribute(trimmedline, "id");
                    string sectorname = X3Utils.GetXMLValue(trimmedline);

                    int xloc = int.Parse(id.Substring(5, 2));       // 1 - based locations. ...YYXX format.
                    int yloc = int.Parse(id.Substring(3, 2));
                    // Find the sector at this location and set it's Id and Name from the data we just retrieved.
                    X3Sector sector = Sectors.Find(item => item.x == xloc - 1 && item.y == yloc - 1);
                    if (sector == null)
                        throw new Exception($"Error reading 9970 file. System located at {xloc}:{yloc} not found in the loaded map.");
                    sector.SectorID = id;                   // format 102YYXX
                    sector.SectorName = sectorname;
                }
                else if (section == 2)
                {
                    // Note:  This section has locations in format YYXX, which is normal. 

                    // <t id="1030101">Lasting Vengeance \(x:0, y:0\)\n\nDejure owner: \033YTeladi\nCompany: \033YSalecrest Corp.\nAbnormal signals: \033Y48 %\nTerran memory: \033Ano\n\nMax. population: \033W107\nStation support: 9\nResearch rate: \033W99 %\n\nOutpost production: Vegetables\nOutpost consumption: Fruit\n</t>
                    // This is just DISPLAY data.  We are not going to verify it.  It is generated from the actual Sector Data in section 3.
                }
                else if (section == 3)
                {
                    // Note:  This section has locations in format XXYY, which is back-asswards.  Since it's his custom section, I guess he figured he'd be different.

                    string id = X3Utils.GetXMLAttribute(trimmedline, "id");
                    string value = X3Utils.GetXMLValue(trimmedline);

                    switch (id)
                    {
                        case "1":       // Galaxy Age
                            if (!Enum.TryParse(value, out GalaxyCreationSettings.GalaxyExpansion))
                                throw new Exception($"Invalid value found in 9970 file for galaxy age: {value}");
                            continue;
                        case "4":        // Universe Name
                            GalaxyCreationSettings.GalaxyName = System.Net.WebUtility.HtmlDecode(value);
                            continue;
                    }
                    int intId = int.Parse(id);
                    int yIndex = (intId - 5) % 6;
                    int intValue = -1;

                    int.TryParse(value, out intValue);

                    if (intId > 4 && intId < 11)        // Argon relation.  Set GalaxyCreationSettings.Relations[Race][Race];
                    {
                        GalaxyCreationSettings.Relations[(int)ERace.Argon - 1, yIndex] = intValue;
                    }
                    if (intId > 10 && intId < 17)
                    {
                        GalaxyCreationSettings.Relations[(int)ERace.Boron - 1, yIndex] = intValue;
                    }
                    if (intId > 16 && intId < 23)
                    {
                        GalaxyCreationSettings.Relations[(int)ERace.Split - 1, yIndex] = intValue;
                    }
                    if (intId > 22 && intId < 29) 
                    {
                        GalaxyCreationSettings.Relations[(int)ERace.Paranid - 1, yIndex] = intValue;
                    }
                    if (intId > 28 && intId < 35)
                    {
                        GalaxyCreationSettings.Relations[(int)ERace.Teladi - 1, yIndex] = intValue;
                    }
                    if (intId > 34 && intId < 41)
                    {
                        GalaxyCreationSettings.Relations[5, yIndex] = intValue;
                    }
                    if (intId == 120)       // Maelstrom portal location, 0-based in format "X,Y"
                    {
                        string[] split = value.Split(',');
                        int x = int.Parse(split[0]);
                        int y = int.Parse(split[1]);
                        X3Sector set = Sectors.Find(item => item.x == x && item.y == y);
                        if (set != null)
                        {
                            set.IsMaelstromPortal = true;
                        }

                    }

                    // Now we get into actual Sector Stat Values that are used in the game.
                    // <t id="10060101">107</t>
                    if (intId > 10000000)       
                    {
                        int yloc = int.Parse(id.Substring(6, 2));   // Last 4       1-based system location in format XXYY
                        int xloc = int.Parse(id.Substring(4, 2));
                        string prefix = id.Remove(4);               // First 4      Determines which type of setting this is.  See switch below.

                        // Find the sector with the coordinate found here.
                        X3Sector sector = Sectors.Find(item => item.x == xloc - 1 && item.y == yloc - 1);
                        if (sector == null)
                            throw new Exception($"Unknown sector {id} found in section 3 of 9970 file.");

                        switch(prefix)
                        {
                            case "1000":            // Dejure Owner
                                sector.SectorCreationData.DejureOwner = intValue;
                                break;
                            case "1001":            // Smugglers (not used)
                                break;
                            case "1002":            // Xenons (abnormal signals)
                                sector.SectorCreationData.Xenons = intValue;
                                break;
                            case "1003":            // Terran memory
                                sector.SectorCreationData.Terrans = intValue;
                                break;
                            case "1004":            // Research
                                sector.SectorCreationData.Research = intValue;
                                break;
                            case "1005":            // Station Support
                                sector.SectorCreationData.Support = intValue;
                                break;
                            case "1006":            // Manpower (system population)
                                sector.SectorCreationData.Manpower = intValue;
                                break;
                            case "1007":            // Cluster (not used)
                                break;
                            case "1008":            // Production
                                sector.SectorCreationData.Production = intValue;
                                break;
                            case "1009":            // Consumption
                                sector.SectorCreationData.Consumption = intValue;
                                break;
                            case "1010":            // Company
                                sector.SectorCreationData.Company = intValue;
                                break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Finds one sector ready to do something with.  
        /// Returns NULL if none exist.
        /// </summary>
        /// <returns></returns>
        public X3Sector GetRandomUninitializedSector(EQuadrant quadrant = EQuadrant.None)
        {
            // ensure we have at least one unitialized sector left.
            List<X3Sector> uninit;
            if (quadrant == EQuadrant.None)
            {
                uninit = Sectors.FindAll(item => item.IsInitialized == false);
            }
            else
            {
                uninit = Sectors.FindAll(item => item.IsInitialized == false && IsInQuadrant(item, quadrant));
            }

            if (uninit.Count == 0) return null;
            int randindex = m_Rand.Next(uninit.Count);
            return uninit.ElementAt(randindex);
        }

        public X3Sector GetCornerUninitializedSector()
        {
            List<X3Sector> uninit = Sectors.FindAll(item => item.IsInitialized == false);
            int y;

            List<X3Sector> leftside = uninit.FindAll(item => item.x == uninit.Min(t => t.x));
            List<X3Sector> rightside = uninit.FindAll(item => item.x == uninit.Max(t => t.x));
            List<X3Sector> side = m_Rand.Next(0, 2) == 0 ? leftside : rightside;

            if (m_Rand.Next(0, 2) == 0)
            {
                y = side.Min(item => item.y);
            }
            else
            {
                y = side.Max(item => item.y);
            }
            return side.Find(item => item.y == y);
        }

        /// <summary>
        /// Determines if a sector's location is within a particular quadrant of the universe.  Used when generating map positions for races.
        /// </summary>
        /// <param name="sec"></param>
        /// <param name="quadrant"></param>
        /// <returns></returns>
        public bool IsInQuadrant(X3Sector sec, EQuadrant quadrant)
        {
            switch(quadrant)
            {
                case EQuadrant.None:
                    return true;
                case EQuadrant.MiddleLeft:
                    return (sec.x > MaxX * 0.25) && (sec.x < MaxX * 0.50) && (sec.y > MaxY * 0.25) && (sec.y < MaxY * 0.50);
                case EQuadrant.MiddleRight:
                    return (sec.x > MaxX * 0.50) && (sec.x < MaxX * 0.75) && (sec.y > MaxY * 0.50) && (sec.y < MaxY * 0.75);
                case EQuadrant.UpperLeft:
                    return (sec.x < MaxX / 2) && (sec.y < MaxY / 2);
                case EQuadrant.UpperRight:
                    return (sec.x >= MaxX / 2) && (sec.y < MaxY / 2);
                case EQuadrant.LowerLeft:
                    return (sec.x < MaxX / 2) && (sec.y >= MaxY / 2);
                case EQuadrant.LowerRight:
                    return (sec.x >= MaxX / 2) && (sec.y >= MaxY / 2);
                default:
                    return true;
            }
        }


        public X3Sector GetSectorAt(int x, int y)
        {
            return Sectors.Find(item => item.x == x && item.y == y);
        }

        public List<X3Sector> GetSectorsBetween(X3Sector source, X3Sector dest)
        {
            if (source.x == dest.x)
            {
                return Sectors.FindAll(sector => sector.y.Between(source.y, dest.y));
            }
            else if (source.y == dest.y)
            {
                return Sectors.FindAll(sector => sector.x.Between(source.x, dest.x));
            }
            else
            {
                throw new Exception("GetSectorsBetween() does not work for diagnonal sectors");
            }
        }

        public bool HasNonVoidSectorsBetween(X3Sector source, X3Sector dest)
        {
            if (source.x == dest.x)
            {
                return Sectors.Exists(sector => sector.y.Between(source.y, dest.y) && !sector.IsVoid && sector.x == source.x);
            }
            else if (source.y == dest.y)
            {
                return Sectors.Exists(sector => sector.x.Between(source.x, dest.x) && !sector.IsVoid && sector.y == source.y);
            }
            else
            {
                throw new Exception("HasNonVoidSectorsBetween() does not work for diagnonal sectors");
            }
        }

        /// <summary>
        /// Returns list of all (1-4) neighboring sectors.
        /// </summary>
        /// <param name="sector"></param>
        /// <returns></returns>
        public List<X3Sector> GetNeighbors(X3Sector sector, int distance = 1)
        {
            List<X3Sector> nb = new List<X3Sector>();


            var west = Sectors.Find(item => item.x == sector.x - distance && item.y == sector.y);
            if (west != null) nb.Add(west);

            var east = Sectors.Find(item => item.x == sector.x + distance && item.y == sector.y);
            if (east != null) nb.Add(east);

            var north = Sectors.Find(item => item.x == sector.x && item.y == sector.y - distance);
            if (north != null) nb.Add(north);

            var south = Sectors.Find(item => item.x == sector.x && item.y == sector.y + distance);
            if (south != null) nb.Add(south);

            return nb;
        }

        /// <summary>
        /// Determines the sector that exists in a given direction.  Null if none exists.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public X3Sector GetSectorInDirection(X3Sector source, EGateDirection direction)
        {
            if (direction == EGateDirection.East || direction == EGateDirection.West)
            {
                for (int x = 1; x < MaxX; x++)
                {
                    int offset = x * ((direction == EGateDirection.East) ? 1 : -1);
                    X3Sector found = Sectors.Find(item => item.x == source.x + offset && item.y == source.y);
                    if (found != null) return found;
                }
            }
            else
            {
                for (int y = 1; y < MaxY; y++)
                {
                    int offset = y * ((direction == EGateDirection.South) ? 1 : -1);
                    X3Sector found = Sectors.Find(item => item.y == source.y + offset && item.x == source.x);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public List<X3Sector> GetConnectedNeighbors(X3Sector sector)
        {
            List<X3Sector> connected = new List<X3Sector>();
            foreach (X3Gate gate in sector.Gates)
            {
                X3Sector dest = Sectors.Find(item => item.x == gate.gx && item.y == gate.gy);
                if (dest == null) throw new Exception("GetConnectedNeigbors not able to find connected gate.");
                connected.Add(dest);
            }

            return connected;
        }


        /// <summary>
        /// Finds a random nearest neighbor along a horizontal or vertical axis, up to maxdistance.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public X3Sector GetNearestRandomNeighbor(X3Sector source, int startdistance = 1, int maxdistance = 6)
        {
            for (int x = startdistance; x <= maxdistance; x++)
            {
                List<X3Sector> neighbors = GetNeighbors(source, x);
                neighbors.RemoveAll(item => item.IsConnectedTo(source));

                if (neighbors.Count > 0)
                {
                    return neighbors[m_Rand.Next(neighbors.Count)];
                }
            }
            return null;
        }

        public void ClearAllGates()
        {
            foreach (X3Sector sector in Sectors)
            {
                sector.RemoveGates();
            }
        }

    }
}
