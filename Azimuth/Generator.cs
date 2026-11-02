using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.IO;

namespace Azimuth
{
    class Generator
    {
        private List<EQuadrant> m_quadrantsAvailable;
        private int m_AvgSpread;
        private int m_AvgSystemsPerRace;
        private Random m_Rand = new Random();
        private X3Sector m_FirstRaceSectorCreated;
        private int m_GlobID;
        private List<List<X3Sector>> m_Globs;
        private bool m_Extrastats;
        private struct Match { public X3Sector Source; public X3Sector Dest; public int Distance; };

        public delegate void Message(string msg);
        public event Message OnMessage;


        public void GenerateSystemStats()
        {
            Msg("Generating new attributes for each sector.");

            m_Extrastats = X3Galaxy.Instance.GalaxyCreationSettings.ExtraSystemStats;
            

            //<t id="10000101">5</t>            // Dejure Owner:   20% chance galaxy wide and always in same race's system.  Some races up to 5% dejure, while others at 1%.  odd.
            //<t id="10010101">0</t>            // smugglers:      Not used
            //<t id="10020101">48</t>           // Xenons:         Abnormal signals.  0-60.   30% of these are at 0. The other 70% average 32% signal level.
            //<t id="10030101">0</t>            // Terrans:        20% chance galaxy wide.
            //<t id="10040101">99</t>           // research:       40-100, average 70
            //<t id="10050101">9</t>            // support         Number of stations supported in system.  8-15, Average 9.
            //<t id="10060101">107</t>          // manpower        Population of system (60-130, average 95)
            //<t id="10070101">-1</t>           // cluster         Not Used.  Set to -1 to duplicate old program.
            //<t id="10080101">3 </t >          // production      Type of Food produced.  See EFood above for enumeration.
            //<t id="10090101">1</t>            // consumption     Type of food consumed.  See EFood.
            //<t id="10100101">4</t>            // company         4% chance galaxy wide for a company to exist in their race system.  + 1 company of every kind in unknown space.

            // Do not use RaceIsMain, because Xenon do not count toward de jure sectors.
            List<X3Sector> populatedSectors = X3Galaxy.Instance.Sectors.FindAll(item => (item.r >= (int)ERace.Argon && item.r <= (int)ERace.Teladi) || item.r == (int)ERace.Terran);
            List<X3Sector> allSectors = X3Galaxy.Instance.Sectors;

            // Clear existing sector data because we may be re-generating over and over.
            allSectors.ForEach(item => item.SectorCreationData = new SectorData());

            // Dejure - Randomly pick 20% of all systems, but located only in Populated systems.
            populatedSectors.Shuffle();
            List<X3Sector> target = populatedSectors.Take((int)(allSectors.Count * 0.2)).ToList();
            target.ForEach(item => item.SectorCreationData.DejureOwner = item.r);

            // Set 70% of all sectors to a non-zero abnormal signal level.  Leave 30% at 0.  
            allSectors.Shuffle();
            target = allSectors.Take((int)(allSectors.Count * 0.7)).ToList();
            target.ForEach(item => item.SectorCreationData.Xenons = m_Rand.Next(1, 61));

            // Set 20% of all sectors to have Terran Memory
            allSectors.Shuffle();
            target = allSectors.Take((int)(allSectors.Count * 0.2)).ToList();
            target.ForEach(item => item.SectorCreationData.Terrans = 1);


            // Research from 40-100 on all sectors
            if (m_Extrastats)
            {
                allSectors.ForEach(item => item.SectorCreationData.Research = m_Rand.Next(60, 151));
            }
            else
            {
                allSectors.ForEach(item => item.SectorCreationData.Research = m_Rand.Next(40, 101));
            }

            // Support levels 8-15, Average 9. So this is an odd distribution.  25% are at 8.  33% are at 9.  30% are at 10.  0% are at 11.  3% are 12 to 13.  6% are above 13.  
            int extrastations = m_Extrastats ? 3 : 0;
            allSectors.Shuffle();
            int p25 = (int)(allSectors.Count * 0.25);
            int p30 = (int)(allSectors.Count * 0.30);
            int p15 = (int)(allSectors.Count * 0.15);
            target = allSectors.Where(item => item.SectorCreationData.Support == 0).Take(p25).ToList();
            target.ForEach(item => item.SectorCreationData.Support = 8 + extrastations);
            target = allSectors.Where(item => item.SectorCreationData.Support == 0).Take(p30).ToList();
            target.ForEach(item => item.SectorCreationData.Support = 9 + extrastations);
            target = allSectors.Where(item => item.SectorCreationData.Support == 0).Take(p30).ToList();
            target.ForEach(item => item.SectorCreationData.Support = 10 + extrastations);
            target = allSectors.Where(item => item.SectorCreationData.Support == 0).Take(p15).ToList();
            target.ForEach(item => item.SectorCreationData.Support = m_Rand.Next(11, 16) + extrastations);
            target = allSectors.Where(item => item.SectorCreationData.Support == 0).ToList();
            target.ForEach(item => item.SectorCreationData.Support = 9);        // If any remain.. set them to 9.

            // Manpower
            if (m_Extrastats)
            {
                allSectors.ForEach(item => item.SectorCreationData.Manpower = m_Rand.Next(85, 181));
            }
            else
            {
                allSectors.ForEach(item => item.SectorCreationData.Manpower = m_Rand.Next(60, 131));
            }

            // Production.
            allSectors.ForEach(item => item.SectorCreationData.Production = m_Rand.Next(0, 4));

            // Consumption
            allSectors.ForEach(item => item.SectorCreationData.Consumption = X3Utils.GetAnotherFoodType(item.SectorCreationData.Production));

            // Company 20% of all sectors have a company located in a matching race sector.  + 1 company of each race exists in unknown space.
            int race;
            for (int r = 1; r < 7; r++)
            {
                race = r;
                if (race == 6) race = (int)ERace.Terran;  // Terrans use this weird id.

                target = allSectors.Where(item => item.r == race).ToList();
                target.Shuffle();
                int ct = (int)(allSectors.Count() * 0.04);
                target = target.Take(ct).ToList();
                target.ForEach(item => item.SectorCreationData.Company = (int)X3Utils.GetCompanyForRace((ERace)race));
            }

            // Put 1 company of each type in an unknown sector.
            target = allSectors.Where(item => item.r == (int)ERace.Unknown).ToList();
            target.Shuffle();
            target = target.Take(6).ToList();
            for (int c = 0; c < 6; c++)     // all companies
            {
                target[c].SectorCreationData.Company = c;
            }
        }



        public void GenerateGalaxy(int maxx, int maxy, List<string>musicFiles, List<string>backgroundFiles, List<List<string>> sunAndPlanets, List<List<string>> asteroids)
        {
            m_quadrantsAvailable = new List<EQuadrant> { EQuadrant.UpperLeft, EQuadrant.UpperRight, EQuadrant.LowerLeft, EQuadrant.LowerRight, EQuadrant.MiddleLeft, EQuadrant.MiddleRight };
            m_FirstRaceSectorCreated = null;
            m_GlobID = 1;

            int musicIndex = musicFiles.Count;
            int backgroundIndex = backgroundFiles.Count;
            int sunsIndex = sunAndPlanets.Count;
            int asteroidsIndex = asteroids.Count;

            string musicFile;
            int backgroundFile;
            List<string> sunsToUse;
            List<string> asteroidsToUse;

            musicFiles.Shuffle();
            backgroundFiles.Shuffle();
            sunAndPlanets.Shuffle();
            asteroids.Shuffle();

            X3Galaxy.Instance.Sectors.Clear();
            X3Galaxy.Instance.SetSize(maxx, maxy);


            // Create all sectors
            for (int x = 0; x < X3Galaxy.Instance.MaxX; x++)
            {
                for (int y = 0; y < X3Galaxy.Instance.MaxY; y++)
                {
                    // For music, background, planets and asteroids, we essentially are using Templates out of the \mayhem_data folder created by Mayhem 3.
                    // We randomize these lists, and use each and every template first, and when we run out we only then pick a random template.  This ensures the least duplicates in our universe.

                    // System Music
                    if (--musicIndex < 0)
                    {
                        musicFile = musicFiles[m_Rand.Next(musicFiles.Count)];
                    }
                    else
                    {
                        musicFile = musicFiles[musicIndex];
                    }


                    // System Background
                    if (--backgroundIndex < 0)
                    {
                        backgroundFile = int.Parse(backgroundFiles[m_Rand.Next(backgroundFiles.Count)]);
                    }
                    else
                    {
                        backgroundFile = int.Parse(backgroundFiles[backgroundIndex]);
                    }



                    // Suns and Planets list
                    if (--sunsIndex < 0)
                    {
                        sunsToUse = sunAndPlanets[m_Rand.Next(sunAndPlanets.Count)];
                    }
                    else
                    {
                        sunsToUse = sunAndPlanets[sunsIndex];
                    }


                    // Asteroids list
                    if (--asteroidsIndex < 0)
                    {
                        asteroidsToUse = asteroids[m_Rand.Next(asteroids.Count)];
                    }
                    else
                    {
                        asteroidsToUse = asteroids[asteroidsIndex];
                    }

                    // Create uninitialized sectors in all spaces to start with.  Void/Empty (not unknown) sectors will be removed from the list when we are all done.
                    X3Sector sector = new X3Sector(x, y, musicFile, backgroundFile, sunsToUse, asteroidsToUse);
                    X3Galaxy.Instance.Sectors.Add(sector);                     // All sectors exist at start.  They are just uninitialized.
                }
            }

            GenerateVoidOrXenonSectors();                       // Generate empty sectors.  These (void sectors) are removed at the end of process.
            GenerateVoidOrXenonSectors(true);                   // Generate Xenon sectors.
            GenerateAllRacesSectors();

            // Mark Remaining space as unknown.
            int unknown = X3Galaxy.Instance.Sectors.Count(item => !item.IsInitialized);
            Msg($"Generated {unknown} Unknown sectors.");
            X3Galaxy.Instance.Sectors.Where(item => !item.IsInitialized).ToList().ForEach(i => i.MarkSectorRace(ERace.Unknown));

            // remove any empty/void sectors.
            X3Galaxy.Instance.Sectors.RemoveAll(sector => sector.IsVoid);

            int sectorCount = X3Galaxy.Instance.Sectors.Count;
            Msg($"Created {sectorCount} total sectors.");
        }


        /// <summary>
        /// 
        /// 1. Connect everything to every immediate neighbor.
        /// 2. foreach Race system, randomly remove a link to a neighboring system not belonging to that race.  Increase chances of this if GateDensity is low.
        ///     This leans the gate network towards good interconnections within starting globs compared to connections to the outside systems.
        ///     Also remove some gates interconnecting this race's systems.  Low chance, but it helps break them up a bit.
        /// 3. foreach Race system, if it's connected to 2 neighbors, randomly remove one of those gates. 
        ///     Helps gates be less connected, promoting the formation of enclaves.
        /// 4. Connect orphaned lone systems to nearest neighbor.    
        /// 5. Here, we may now end up with small "islands" of systems that are interconnected with each other, but still surrounded by a void and are not reachable.
        ///     Start with the first Argon Glob system created, we can iterate through ALL remaining non-void sectors on the map and determining if there
        ///     is a valid path to that original system.  If not, connect that system to it's nearest neighbor that isn't already connected to it.
        ///     https://blog.theknightsofunity.com/pathfinding-on-a-hexagonal-grid-a-algorithm/  
        ///     
        /// </summary>
        public void GenerateGates()
        {
            X3Galaxy.Instance.ClearAllGates();

            // 1. Create gates to every possible direct neighbor.
            Msg("Creating gates.");
            for (int x = 0; x < X3Galaxy.Instance.MaxX; x++)
            {
                for (int y = 0; y < X3Galaxy.Instance.MaxY; y++)
                {
                    X3Sector source = X3Galaxy.Instance.GetSectorAt(x, y);
                    if (source != null)
                    {
                        List<X3Sector> neighbors = X3Galaxy.Instance.GetNeighbors(source);
                        foreach (X3Sector neighbor in neighbors)
                        {
                            source.AddGates(neighbor);
                        }
                    }
                }
            }

            // 2. Selectively remove some gates interconnecting differing races and then some interconnecting a race.
            //    This tends to help create race-centric Clusters.
            foreach (ERace race in Enum.GetValues(typeof(ERace)))
            {
                if (race == ERace.None) continue;
                if (race == ERace.Xenon) continue;

                RemoveBorderingGatesToOtherRaces(race);

                RemoveInternalGatesForRace(race);
            }

            // 3.
            if (!X3Galaxy.Instance.GalaxyCreationSettings.LimitedEnclaves)
            {
                Msg("Creating additional enclaves. Removing gates.");
                CreateMoreEnclaves();
            }

            // An individual sector is sitting there with no gates.
            Msg("Connecting orphaned sectors.");
            ConnectOrphanedSectors();

            // Everything up to now has been used to create groupings of systems and give them group ID's.  Each group being a Glob.
            // This allows us to intelligently connect one glob to another rather than just dumbly randomly spreading every possible new gate.
            CreateGlobs();

            // This will add back gates to interconnect the blobs.
            CombineGlobs();

            int gateCount = X3Galaxy.Instance.Sectors.Sum(item => item.Gates.Count);
            Msg($"Created {gateCount} gates.");
        }

        /// <summary>
        /// 
        /// Generating systems with a shattered galaxy can lead to highly isolated clusters of systems.
        /// If we simply connecting gates for every sector in each of these clusters to every possible other system, we truly lose that sense
        /// of having isolated clusters as they may be isolated positionally on the map, but are still connected everywhere by gates.  There's
        /// no point in having an isolated cluster in the first place if every border system in it is connected to every possible far away neighbor.
        /// Thus, we try to treat these groupings of close sectors as Clusters and give them an appropriate "Glob" ID.
        /// 
        /// Determine isolated clusters of systems (a Glob) and marks them with a specific GlobID.
        /// A Glob is recursively expanded as long as it has neighboring non-void systems.
        /// 
        /// We use the IsOrphaned flag to determine whether a system has been added to a cluster or not.
        /// We use the GlobID in each system to identify it's cluster (glob).
        /// </summary>
        public void CreateGlobs()
        {
            m_Globs = new List<List<X3Sector>>();
            X3Galaxy.Instance.Sectors.ForEach(item => item.IsOrphaned = true); 
            m_GlobID = 0;

            while (X3Galaxy.Instance.Sectors.Exists(sec => sec.IsOrphaned))
            {
                // Create a new Glob
                List<X3Sector> glob = new List<X3Sector>();

                X3Sector start = X3Galaxy.Instance.Sectors.Find(item => item.IsOrphaned);  // Just pick any old orphaned sector to start with.
                start.IsOrphaned = false;                                   // Adding a system to a glob makes it non-orphaned
                start.GlobID = m_GlobID;
                glob.Add(start);
                // Expand this glob to include all immediate connected neighbors until it has no more and then add it to our Globs colection..
                m_Globs.Add(ExpandGlob(glob));
                m_GlobID++;
            }

            // At this point, we should have 1 or more Globs.
            int globscount = m_Globs.Count;
            //Msg($"{globscount} Clusters found.");

            //int orphanedRemaining = uv.Sectors.Count(item => item.IsOrphaned == true);      // should be zero.

            // Debug Only.
#if (GLOBTESTING)
            PaintGlobs();
#endif
        }

        // Use the same effective void percentage for sector placement and gate searches.
        // This does not modify galaxy dimensions or sector-count targets.
        private int GetVoidPercent()
        {
            int voidpct = X3Galaxy.Instance.GalaxyCreationSettings.ShatteredGalaxy ? 35 : 15;
            int configured;
            if (int.TryParse(X3Utils.GetSetting("EmptySpacePercent"), out configured)
                && configured > 15 && configured < 51)
            {
                voidpct = configured;
            }
            return voidpct;
        }

        public void CombineGlobs()
        {
            // Ordinary clusters use nearby links. Distant links are a fallback only
            // when the actual sector layout offers no nearby connection.
            const int preferredLinkDistance = 4;
            int curGlobId;

            bool done = false;
            while(!done)
            {
                List<X3Sector> srcglob = m_Globs[0];
                List<int> globIDsToCombine = new List<int>();
                List<Match> matches = new List<Match>();
                curGlobId = srcglob[0].GlobID;

                globIDsToCombine.Add(curGlobId);

                // Each glob, based on number of systems in it will have only so many gates we want running into or out if it.
                ComputeMaxGatesForAllGlobs();

                // Create an array of Matches where each represents:
                // 1. Source System (in Glob)
                // 2. Dest System (outside Glob on vertical or horizontal axis)
                // 3. Distance between them.
                List<X3Sector> possibleDestinations = X3Galaxy.Instance.Sectors.FindAll(item => item.GlobID != curGlobId);
                foreach (X3Sector srcsec in srcglob)
                {
                    foreach (X3Sector dstsec in possibleDestinations)
                    {
                        if (srcsec.x == dstsec.x || srcsec.y == dstsec.y)       // Only include destinations that are on the same vertical or horizontal axis.  nothing horizontal.
                        {
                            int dst = srcsec.DistanceFrom(dstsec);
                            // A usable link must have a clear, axis-aligned path.
                            // Evaluate the map itself, rather than its configured void %.
                            if (!X3Galaxy.Instance.HasNonVoidSectorsBetween(srcsec, dstsec))
                            {
                                matches.Add(new Match { Source = srcsec, Dest = dstsec, Distance = dst });
                            }
                        }
                    }
                }
                if (matches.Count == 0)  break;      // shouldn't get here.

                // Prefer the old four-sector neighborhood. If it has no valid
                // link, allow only the nearest longer reach needed for this glob.
                // This adapts to real gaps without lengthening every gate on sparse maps.
                matches.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                int allowedDistance = Math.Max(preferredLinkDistance, matches[0].Distance);
                matches.RemoveAll(match => match.Distance > allowedDistance);

                // Try to create from x number of gates for each blob depending on size.  Some globs are only 2 sytems, while others are 40+ systems.
                int srcGatesToCreate = (int)Math.Sqrt(srcglob.Count);

                Dictionary<int, int> numGatesAddedToGlob = new Dictionary<int, int>();
                int matchIndex = 0;

                while (srcGatesToCreate > 0)
                {
                    if (matchIndex >= matches.Count) break;
                    X3Sector src = matches[matchIndex].Source;
                    X3Sector dst = matches[matchIndex].Dest;
                    if (!globIDsToCombine.Contains(dst.GlobID))
                    {
                        globIDsToCombine.Add(dst.GlobID);
                    }

                    // Look at destination glob and see how many gates it can effectively handle.  It may be much smaller than the source glob.
                    // First init a new entry in our dictionary if one doesn't already exist.  We're tracking gates added per glob.
                    if (!numGatesAddedToGlob.ContainsKey(dst.GlobID))
                    {
                        numGatesAddedToGlob.Add(dst.GlobID, 0);
                    }
                    // How many gates can that destination glob handle.
                    int destmaxgates = (int)Math.Sqrt(X3Galaxy.Instance.Sectors.Count(item => item.GlobID == dst.GlobID));
                    // How many have we already added the maximum to that destination glob?
                    if (numGatesAddedToGlob[dst.GlobID] == destmaxgates)
                    {
                        matchIndex++;
                        continue;      // move onto next possible dest, which may be in another glob that has slots open.
                    }
                    numGatesAddedToGlob[dst.GlobID]++;

                    src.AddGates(dst);
                    //Msg($"Connecting {src.x},{src.y} to {dst.x},{dst.y}");
                    lastmatchesadded.Add(matches[matchIndex]);

                    matchIndex++;
                    srcGatesToCreate--;
                }

                // Each pass through this loop connects only the globs that have systems within 5 squares of each other..  So each
                // pass results in globs absorbing only nearby globs, creating larger ones until in the end there are only a few left to combine.
                done = CombineSelectedGlobs(globIDsToCombine);
            }
        }

        private Dictionary<int, int> MaxGates = new Dictionary<int, int>();     // GlobId, MaxGatesInGlob
        private void ComputeMaxGatesForAllGlobs()
        {
            MaxGates.Clear();
            foreach (List<X3Sector>glob in m_Globs)
            {
                MaxGates.Add(glob[0].GlobID, (int)Math.Sqrt(glob.Count));
            }
        }

        private List<Match> lastmatchesadded = new List<Match>();
        public void RemoveLastMatches()
        {
            foreach (Match match in lastmatchesadded)
            {
                match.Source.RemoveGates(match.Dest);
            }
        }


        private bool CombineSelectedGlobs(List<int>combinelist)
        {
            if (m_Globs.Count == 1)
            {
                throw new Exception("CombineGlobs was given 1 glob to combine");
            }
            int homeId = combinelist[0];
            combinelist.Remove(homeId);
            List<X3Sector> homeGlob = m_Globs.Find(item => item.Exists(system => system.GlobID == homeId));
            Color homeColor = homeGlob[0].GlobColor;
            foreach(int targetId in combinelist)
            {
                List<X3Sector> targetGlob = m_Globs.Find(item => item.Exists(system => system.GlobID == targetId));
                foreach(X3Sector sec in targetGlob)
                {
                    sec.GlobID = homeId;
                    sec.GlobColor = homeColor;
                }
                homeGlob.AddRange(targetGlob);
                m_Globs.Remove(targetGlob);
            }
            return (m_Globs.Count == 1);
        }




        /// <summary>
        /// Recursive routine that takes a glob containing 1 or more starting sectors, and adds all gate-interconnected neighbors to that glob.
        /// </summary>
        /// <param name="glob"></param>
        /// <returns></returns>
        private List<X3Sector> ExpandGlob(List<X3Sector>glob)
        {
            List<X3Sector> neighbors = new List<X3Sector>();
            foreach(X3Sector globsec in glob)
            {
                // Get all connected orphaned neighbors
                foreach (X3Sector globsecneighbor in X3Galaxy.Instance.GetConnectedNeighbors(globsec).FindAll(sec => sec.IsOrphaned))
                {
                    if (!neighbors.Exists(item => item.x == globsecneighbor.x && item.y == globsecneighbor.y))
                    {
                        globsecneighbor.IsOrphaned = false;
                        globsecneighbor.GlobID = m_GlobID;
                        neighbors.Add(globsecneighbor);
                    }
                }
            }

            if (neighbors.Count > 0)
            {
                glob.AddRange(neighbors);
                return ExpandGlob(glob);
            }
            else
            {
                return glob;    // no new neighbors found, just return the glob we passed in and stop recursing.
            }
        }

        private void PaintGlobs()
        {
            List<Color> colors = new List<Color>();
            for (int x = 0; x < 100; x++)
            {
                colors.Add(Color.FromArgb(m_Rand.Next(180), m_Rand.Next(180), m_Rand.Next(180)));
            }

            // Debug only.  Color Globs
            foreach (List<X3Sector> glob in m_Globs)
            {
                if (m_Globs[0] == glob) continue;
                Color randy = colors[m_Rand.Next(colors.Count)];
                foreach (X3Sector sector in glob)
                {
                    sector.GlobColor = randy;
                    if (sector == m_FirstRaceSectorCreated)
                        sector.GlobColor = Color.Red;
                }
            }
        }




        /// <summary>
        /// Start with the first Argon Glob system created, we can iterate through ALL remaining sectors on the map and determining if there
        /// is a valid path to that original system.  If not, it's marked as Orphaned
        /// </summary>
        public void MarkOrphanedSectors()
        {
            foreach (X3Sector dest in X3Galaxy.Instance.Sectors)
            {
                if (dest != m_FirstRaceSectorCreated)
                {
                    dest.IsOrphaned = !X3MapPath.HasPath(m_FirstRaceSectorCreated, dest);
                }
            }
        }


        public void ConnectOrphanedSectors()
        {
            int numorphaned = 0;
            foreach (X3Sector source in X3Galaxy.Instance.Sectors)
            {
                if (source.Gates.Count == 0)
                {
                    // Retain the original six-sector neighborhood whenever possible.
                    X3Sector dest = X3Galaxy.Instance.GetNearestRandomNeighbor(source);
                    if (dest == null)
                    {
                        // Sparse or shattered layouts may have no nearby sector.
                        // Find the closest unobstructed sector along either axis.
                        List<X3Sector> candidates = X3Galaxy.Instance.Sectors
                            .Where(candidate => candidate != source
                                && (candidate.x == source.x || candidate.y == source.y)
                                && !source.IsConnectedTo(candidate)
                                && !X3Galaxy.Instance.HasNonVoidSectorsBetween(source, candidate))
                            .ToList();

                        if (candidates.Count > 0)
                        {
                            int nearest = candidates.Min(candidate => source.DistanceFrom(candidate));
                            List<X3Sector> closest = candidates
                                .Where(candidate => source.DistanceFrom(candidate) == nearest)
                                .ToList();
                            dest = closest[m_Rand.Next(closest.Count)];
                        }
                    }

                    if (dest != null)
                    {
                        numorphaned++;
                        source.AddGates(dest);
                    }
                }
            }
            Msg($"Connected {numorphaned} Orphaned Sectors.");
        }


        private void CreateMoreEnclaves()
        {
            int numremoved = 0;

            foreach (X3Sector source in X3Galaxy.Instance.Sectors)
            {
                List<X3Sector> neighbors = X3Galaxy.Instance.GetNeighbors(source);
                if ((neighbors.Count == 2) && (source.Gates.Count == 2))
                {
                    if (m_Rand.Next(100) > 50)
                    {
                        // pick one neighbor to remove, but first ensure that neighbor has more than 1 gate.
                        X3Sector secToRemove = neighbors[m_Rand.Next(neighbors.Count)];

                        // Don't want to create an enclave in destination system if limited enclaves is on.
                        if (secToRemove.Gates.Count > 1)
                        {
                            numremoved++;
                            source.RemoveGates(secToRemove);
                        }
                    }
                }
            }
            Msg($"Removed {numremoved} gates to produce more Enclaves.");
        }

        /// <summary>
        /// Up to 70% chance that a gate will be removed between dissimilar races, provided removal of that
        /// gate does not create an unwanted Enclave.
        /// This tends to create race centric clusters.
        /// </summary>
        /// <param name="erace"></param>
        private void RemoveBorderingGatesToOtherRaces(ERace erace)
        {
            int minimumsystems = X3Galaxy.Instance.GalaxyCreationSettings.LimitedEnclaves ? 2 : 1;
            int race = (int)erace;

            List<X3Sector> raceSectors = X3Galaxy.Instance.Sectors.FindAll(item => item.r == race);
            foreach (X3Sector source in raceSectors)
            {
                if (source.Gates.Count <= minimumsystems) continue;
                // Remove some bordering gates that go to other races.
                List<X3Sector> non_race_neighbors = X3Galaxy.Instance.GetNeighbors(source).FindAll(item => item.r != race);
                if (non_race_neighbors.Count > 1)
                {
                    int chanceToremove = X3Galaxy.Instance.GalaxyCreationSettings.BorderGateDensity == EGateDensity.High ? 20 : 70;
                    if (m_Rand.Next(100) < chanceToremove)
                    {
                        // pick one neighbor to remove.
                        X3Sector secToRemove = non_race_neighbors[m_Rand.Next(non_race_neighbors.Count)];
                        if (secToRemove.Gates.Count > minimumsystems)
                        {
                            source.RemoveGates(secToRemove);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Up to 20% chance that a gate between systems of the same race is removed.
        /// </summary>
        /// <param name="erace"></param>
        private void RemoveInternalGatesForRace(ERace erace)
        {
            int minimumsystems = X3Galaxy.Instance.GalaxyCreationSettings.LimitedEnclaves ? 2 : 1;
            int race = (int)erace;

            List<X3Sector> raceSectors = X3Galaxy.Instance.Sectors.FindAll(item => item.r == race);
            foreach (X3Sector source in raceSectors)
            {
                if (source.Gates.Count <= minimumsystems) continue;

                // Remove some (20% chance, very little) gates that go between systems of the same race.  Not adjustable in U.I.
                List<X3Sector> race_neighbors = X3Galaxy.Instance.GetNeighbors(source).FindAll(item => item.r == race);
                if (race_neighbors.Count > 2)
                {
                    if (m_Rand.Next(100) < 20)
                    {
                        // pick one neighbor to remove.
                        X3Sector secToRemove = race_neighbors[m_Rand.Next(race_neighbors.Count)];
                        if (secToRemove.Gates.Count > minimumsystems)
                        {
                            source.RemoveGates(secToRemove);
                        }
                    }
                }
            }
        }

        private void GenerateAllRacesSectors()
        {
            // First determine, based on number of available systems and the Race System Spread, how many systems this race is allowed.
            // If no changes are made, the baseline is 0, meaning no adjustment.
            // If they choose +25 for every race, it should work out the same.  All are equal.   It's the difference from the average that matters.
            m_AvgSpread = (
                X3Galaxy.Instance.GalaxyCreationSettings.ArgonSpread +
                X3Galaxy.Instance.GalaxyCreationSettings.BoronSpread +
                X3Galaxy.Instance.GalaxyCreationSettings.SplitSpread +
                X3Galaxy.Instance.GalaxyCreationSettings.ParanidSpread +
                X3Galaxy.Instance.GalaxyCreationSettings.TeladiSpread +
                X3Galaxy.Instance.GalaxyCreationSettings.TerranSpread) / 6;

            // count total non-void and non-Xenon sectors available for colonization.
            int totalsystemsAvailable = X3Galaxy.Instance.Sectors.Count(item => item.IsVoid == false && item.r == (int)ERace.None);
            switch(X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion)
            {
                case EGalaxyExpansion.Origin:
                    totalsystemsAvailable = 6;
                    break;
                case EGalaxyExpansion.Early:
                    totalsystemsAvailable = (int)(totalsystemsAvailable * 0.4);     // 60% unknown space
                    break;
                case EGalaxyExpansion.Average:
                    totalsystemsAvailable = (int)(totalsystemsAvailable * 0.6);     // 40% unknown space
                    break;
                case EGalaxyExpansion.Advanced:
                    totalsystemsAvailable = (int)(totalsystemsAvailable * 0.8);     // 20% unknown space
                    break;
            }
            m_AvgSystemsPerRace =  totalsystemsAvailable / 6;   // number of systems each race would get if all left at equal spreads.
            Debug.WriteLine($"Average sectors per race: {m_AvgSystemsPerRace}");
            Msg($"Average sectors per race: { m_AvgSystemsPerRace}");

            GenerateRaceSector(ERace.Argon, X3Galaxy.Instance.GalaxyCreationSettings.ArgonSpread);
            GenerateRaceSector(ERace.Boron, X3Galaxy.Instance.GalaxyCreationSettings.BoronSpread);
            GenerateRaceSector(ERace.Paranid, X3Galaxy.Instance.GalaxyCreationSettings.ParanidSpread);
            GenerateRaceSector(ERace.Split, X3Galaxy.Instance.GalaxyCreationSettings.SplitSpread);
            GenerateRaceSector(ERace.Teladi, X3Galaxy.Instance.GalaxyCreationSettings.TeladiSpread);
            GenerateRaceSector(ERace.Terran, X3Galaxy.Instance.GalaxyCreationSettings.TerranSpread);
        }


        private void GenerateRaceSector(ERace race, int racespread)
        {
            int targetNum, created1, created2, created3, created4 = 0, remaining = 0;
            int systemsToCreate = m_AvgSystemsPerRace + (m_AvgSystemsPerRace * (racespread - m_AvgSpread) / 100);

            if (X3Galaxy.Instance.GalaxyCreationSettings.GalaxyExpansion == EGalaxyExpansion.Origin)
            {
                systemsToCreate = 1;
            }

            EQuadrant primaryQuadrant = m_quadrantsAvailable[m_Rand.Next(m_quadrantsAvailable.Count)];
            m_quadrantsAvailable.Remove(primaryQuadrant);

            // Divide map into 5 quadrants:  1 in middle and 4 on each corner.  Each race gets a quadrant.
            // Randomly place 2 systems in one of the quadrants.  Build on those for 90% of the race systems.
            // Build 10% of the race systems randomly in another quadrant.
            targetNum = (int)(systemsToCreate * 0.45);
            created1 = CreateGlob(primaryQuadrant, race, targetNum);        // In the chance we can't create all targetNum here, we try to make it up with the 2nd glob.
            created2 = CreateGlob(primaryQuadrant, race, targetNum + (targetNum - created1));

            // Because of rounding, we will always create less than we need with the first 2 globs.  We're going to make it up with the 3rd glob.
            remaining = systemsToCreate - (created1 + created2);
            created3 = CreateGlob(EQuadrant.None, race, remaining);

            // If all 3 don't add up to the total we want, we basically have a cornered race that was nearly last to put down.. in that case, give them another Glob randomly.
            remaining = systemsToCreate - (created1 + created2 + created3);
            if (remaining > 0)
            {
                created4 = CreateGlob(EQuadrant.None, race, remaining);
            }
            Msg($"{race} - {created1 + created2 + created3 + created4} sectors created");
        }


        private int CreateGlob(EQuadrant primaryQuadrant, ERace race, int systemsToCreate)
        {
            List<X3Sector> glob = new List<X3Sector>();
            int systemsCreated = 0;
            X3Sector startsec = X3Galaxy.Instance.GetRandomUninitializedSector(primaryQuadrant);
            if (startsec == null)
            {
                // Should seldom happen where we run out of totally uninitialized sectors in a target quadrant.. but if we do, just put the Glob somewhere else.
                startsec = X3Galaxy.Instance.GetRandomUninitializedSector(EQuadrant.None);
                if (startsec == null) return 0;
            }
                
            glob.Add(startsec);


            if (m_FirstRaceSectorCreated == null && race == ERace.Argon)
            {
                m_FirstRaceSectorCreated = startsec;
            }

            while (systemsToCreate > systemsCreated)
            {
                // Pick a random sector in the existing glob.
                X3Sector sec = GetRandomSectorInGlobHavingNeighbors(glob);
                if (sec == null) break;
                sec.MarkSectorRace(race);
                glob.Add(sec);
                systemsCreated++;
            }
            return systemsCreated;
        }

        private X3Sector GetRandomSectorInGlobHavingNeighbors(List<X3Sector>glob)
        {
            glob.Shuffle();

            bool done = false;
            int distance = 1;
            while (!done)
            {
                foreach (X3Sector sec in glob)
                {
                    List<X3Sector> freeneighbors = X3Galaxy.Instance.GetNeighbors(sec, distance).FindAll(item => item.IsInitialized == false); // get all uninitialized neighbors.
                    if (freeneighbors.Count == 0)
                    {
                        continue;
                    }
                    X3Sector afreeneighbor = freeneighbors[m_Rand.Next(freeneighbors.Count)];    // pick one of them.
                    return afreeneighbor;
                }
                if (distance++ > 5) done = true;
            }
            return null;
        }

        /// <summary>
        /// We actually place an empty/non-existent/void sector into the sectors list as a placeholder, but they get removed after everything else is generated.
        /// IsVoid = true
        /// 
        /// For xenon generation, the algorithm for placement is identical so we just pass a flag.
        /// </summary>
        /// <param name="uv"></param>
        private void GenerateVoidOrXenonSectors(bool isXenon = false)
        {
            bool isVoid = !isXenon;
            int voidpct = GetVoidPercent();
            bool isChaotic = X3Galaxy.Instance.GalaxyCreationSettings.ChaoticStart;
            X3Sector sector;

            // xenon space override
            int xenonpct = 3;
            string xenonpercentsetting = X3Utils.GetSetting("XenonPercent");    // override the standard values.  Only usable overrides are 4-10
            int xps;
            if (int.TryParse(xenonpercentsetting, out xps))
            {
                if (xps > 3 && xps < 11)
                {
                    xenonpct = xps;
                }
            }
            if (isChaotic)
            {
                xenonpct = 50;
            }


            int avgspace = isXenon ? xenonpct : voidpct;
            int numSectors = (X3Galaxy.Instance.MaxX * X3Galaxy.Instance.MaxY * avgspace) / 100;


            if (isXenon)
            {
                if (X3Galaxy.Instance.GalaxyCreationSettings.PeacefulStart)
                {
                    // Get a corner sector, mark it as xenon and as the maelstrom portal and then we're done for xenon's.
                    sector = X3Galaxy.Instance.GetCornerUninitializedSector();
                    sector.MarkSectorVoidorXenon(true);
                    sector.IsMaelstromPortal = true;
                    return;
                }
            }
            else
            {
                // safety check here to make sure we don't generate more than 256 actual systems, as we only have that many system names.
                // Note, this means the map dimensions can support 300 raw sectors, cut down by 15% for voids which would yield 255 actual sectors.
                // This is 17x17 or 16x18
                int rawsectorcount = X3Galaxy.Instance.Sectors.Count;       // 300
                int dif = rawsectorcount - numSectors - 256;                // 300 - 45 - 256 = -1
                if (dif > 0) numSectors += dif;
            }

            string action = isXenon ? "Xenon" : "Void (gaps)";
            Msg($"Generating {numSectors} {action} sectors.");

            for (int x = 0; x < numSectors; x++)
            {
                if (isChaotic && isXenon && x == 0)
                {
                    sector = X3Galaxy.Instance.GetCornerUninitializedSector();      // For chaotic start, the first xenon sector generated is in a corner.
                    sector.MarkSectorVoidorXenon(true);
                    sector.IsMaelstromPortal = true;
                    continue;
                }

                sector = X3Galaxy.Instance.GetRandomUninitializedSector();
                if (sector == null) throw new Exception("Unable to place sector. No unitialized sectors left.");

                bool clustered = isXenon ? X3Galaxy.Instance.GalaxyCreationSettings.ClusteredXenons : X3Galaxy.Instance.GalaxyCreationSettings.ClusteredVoids;
                if (clustered)
                {
                    // First Half of clustered sectors are randomly placed and second half must be located next to another void sector.

                    // We must place some void sectors here in the first half, and in the second half of our series we will b able to cluster next to the first half.
                    if (x < numSectors / 2 && (isVoid || !isChaotic))
                    {
                        sector.MarkSectorVoidorXenon(isXenon);
                    }
                    else  // Search randomly for an existing void or xenon sector to cluster next to.
                    {  
                        List<X3Sector> secs = X3Galaxy.Instance.Sectors.FindAll(item => isXenon ? item.r == (int)ERace.Xenon : item.IsVoid);
                        if (secs.Count == 0)
                            break;
                        //throw new Exception("Unable to find any void/Xenon sectors to cluster next to.");

                        secs.Shuffle();

                        bool done = false;
                        int distance = 1;
                        while (!done)
                        {
                            foreach (X3Sector randomvoidsec in secs)
                            {
                                //X3Sector randomvoidsec = secs[rand.Next(secs.Count)]; // pick one of the random ones
                                List<X3Sector> freeneighbors = X3Galaxy.Instance.GetNeighbors(randomvoidsec, distance).FindAll(item => item.IsInitialized == false); // get all uninitialized neighbors.
                                if (freeneighbors.Count == 0) continue;

                                X3Sector afreeneighbor = freeneighbors[m_Rand.Next(freeneighbors.Count)];    // pick one of them.
                                afreeneighbor.MarkSectorVoidorXenon(isXenon);
                                done = true;
                                break;
                            }
                            if (distance++ > 5)
                            {
                                // Could find no neighboring uninit sectors within 5 squares of any of these sectors.
                                // This usually occurs only on the first Xenon sector for a Chaotic Start, and also using the Shattered Galaxy option.  In this condition,
                                // you can have the first xenon system in a corner with no nearby neighbors.  Usually it will find one within 2 squares 90% of the time.
                                done = true;
                            }
                        }
                    }
                }
                else
                {
                    sector.MarkSectorVoidorXenon(isXenon);
                }
            }
        }


        private void Msg(string msg)
        {
            OnMessage?.Invoke(msg);
        }


    }
}
