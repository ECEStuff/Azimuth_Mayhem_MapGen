// Builds territories, voids, Xenon space, gates and economic statistics for the in-memory galaxy.

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
        private struct Match { public X3Sector Source; public X3Sector Dest; public int Distance; };

        public delegate void Message(string msg);
        public event Message OnMessage;


        private static int ReducedStat(int value)
        {
            // Round 80% to the nearest whole stat.
            return (4 * value + 2) / 5;
        }

        public void GenerateSystemStats()
        {
            Msg(X3Utils.Ui("Generator.GeneratingNewAttributesForEachSector"));

            var settings = X3Galaxy.Instance.GalaxyCreationSettings;
            // New rolls replace any boosted values imported from a legacy galaxy.
            settings.ExtraSystemStats = false;
            

            //<t id="10000101">5</t>            // Dejure Owner:   20% chance galaxy wide and always in same race's system.  Some races up to 5% dejure, while others at 1%.  odd.
            //<t id="10010101">0</t>            // smugglers:      Not used
            //<t id="10020101">48</t>           // Xenons:         Abnormal signals.  0-60.   30% of these are at 0. The other 70% average 32% signal level.
            //<t id="10030101">0</t>            // Terrans:        20% chance galaxy wide.
            //<t id="10040101">99</t>           // research:       40-100, average 70
            //<t id="10050101">10</t>           // support         Number of stations supported in system. 8-15, expected average 10.
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


            // Each full block totals 200. Its three high rolls total 39;
            // remaining sectors outside a full block receive 10 each.
            allSectors.Shuffle();
            int fullBlockSectors = allSectors.Count / 20 * 20;
            int spread = 0;
            for (int i = 0; i < allSectors.Count; i++)
            {
                var data = allSectors[i].SectorCreationData;
                int slot = i % 20;
                data.Support = 10;
                if (i < fullBlockSectors)
                {
                    if (slot < 2) data.Support = 8;
                    else if (slot < 7) data.Support = 9;
                    else if (slot == 7)
                    {
                        spread = m_Rand.Next(3);
                        data.Support = 13 - spread;
                    }
                    else if (slot == 8) data.Support = 13;
                    else if (slot == 9) data.Support = 13 + spread;
                }
                data.Research = m_Rand.Next(40, 101);
                data.Manpower = m_Rand.Next(60, 131);
                if (settings.SaboteurMode)
                {
                    data.Research = ReducedStat(data.Research);
                    data.Manpower = ReducedStat(data.Manpower);
                }
            }

            if (settings.SaboteurMode)
            {
                // Round down first, then award the remaining whole stations to the
                // largest fractional shares. Equal shares follow the shuffled order.
                var roundingOrder = allSectors.OrderByDescending(
                    sector => sector.SectorCreationData.Support * 4 % 5).ToList();
                foreach (var sector in allSectors)
                    sector.SectorCreationData.Support = sector.SectorCreationData.Support * 4 / 5;

                int remaining = 8 * allSectors.Count - allSectors.Sum(sector => sector.SectorCreationData.Support);
                foreach (var sector in roundingOrder.Take(remaining))
                    sector.SectorCreationData.Support++;
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
            for (int c = 0; c < target.Count; c++)     // all companies
            {
                target[c].SectorCreationData.Company = c;
            }
        }



        public void GenerateGalaxy(int maxx, int maxy, List<string>musicFiles, List<string>backgroundFiles, List<List<string>> sunAndPlanets, List<List<string>> asteroids)
        {
            m_quadrantsAvailable = new List<EQuadrant> { EQuadrant.UpperLeft, EQuadrant.UpperRight, EQuadrant.LowerLeft, EQuadrant.LowerRight, EQuadrant.MiddleLeft, EQuadrant.MiddleRight };
            m_FirstRaceSectorCreated = null;
            m_GlobID = 1;

            int backgroundIndex = backgroundFiles.Count;
            int sunsIndex = sunAndPlanets.Count;
            int asteroidsIndex = asteroids.Count;

            int backgroundFile;
            List<string> sunsToUse;
            List<string> asteroidsToUse;

            backgroundFiles.Shuffle();
            sunAndPlanets.Shuffle();
            asteroids.Shuffle();

            X3Galaxy.Instance.Sectors.Clear();
            X3Galaxy.Instance.SetSize(maxx, maxy);
            X3Galaxy.Instance.GalaxyCreationSettings.GridWidth = maxx;
            X3Galaxy.Instance.GalaxyCreationSettings.GridHeight = maxy;


            // Create all sectors
            for (int x = 0; x < X3Galaxy.Instance.MaxX; x++)
            {
                for (int y = 0; y < X3Galaxy.Instance.MaxY; y++)
                {
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
                    X3Sector sector = new X3Sector(x, y, null, backgroundFile, sunsToUse, asteroidsToUse);
                    X3Galaxy.Instance.Sectors.Add(sector);                     // All sectors exist at start.  They are just uninitialized.
                }
            }

            GenerateVoidOrXenonSectors();                       // Generate empty sectors.  These (void sectors) are removed at the end of process.
            GenerateVoidOrXenonSectors(true);                   // Generate Xenon sectors.
            GenerateAllRacesSectors();

            // Mark Remaining space as unknown.
            int unknown = X3Galaxy.Instance.Sectors.Count(item => !item.IsInitialized);
            Msg(X3Utils.Ui("Generator.GeneratedUnknownSectors", unknown));
            X3Galaxy.Instance.Sectors.Where(item => !item.IsInitialized).ToList().ForEach(i => i.MarkSectorRace(ERace.Unknown));

            // remove any empty/void sectors.
            X3Galaxy.Instance.Sectors.RemoveAll(sector => sector.IsVoid);

            // Deal tracks only to surviving sectors. Every complete round uses each ID once.
            int musicIndex = 0;
            foreach (var sector in X3Galaxy.Instance.Sectors)
            {
                if (musicIndex == 0)
                    musicFiles.Shuffle();
                sector.m_MusicFileName = musicFiles[musicIndex++];
                if (musicIndex == musicFiles.Count)
                    musicIndex = 0;
            }

            int sectorCount = X3Galaxy.Instance.Sectors.Count;
            Msg(X3Utils.Ui("Generator.CreatedTotalSectors", sectorCount));
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
            Msg(X3Utils.Ui("Generator.CreatingGates"));
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
                Msg(X3Utils.Ui("Generator.CreatingAdditionalEnclavesRemovingGates"));
                CreateMoreEnclaves();
            }

            // An individual sector is sitting there with no gates.
            Msg(X3Utils.Ui("Generator.ConnectingOrphanedSectors"));
            ConnectOrphanedSectors();

            // Everything up to now has been used to create groupings of systems and give them group ID's.  Each group being a Glob.
            // This allows us to intelligently connect one glob to another rather than just dumbly randomly spreading every possible new gate.
            CreateGlobs();

            // This will add back gates to interconnect the blobs.
            CombineGlobs();

            int gateCount = X3Galaxy.Instance.Sectors.Sum(item => item.Gates.Count);
            Msg(X3Utils.Ui("Generator.CreatedGates", gateCount));
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
                throw new Exception(X3Utils.Ui("Generator.CombineGlobsWasGivenGlobToCombine"));
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
            Msg(X3Utils.Ui("Generator.ConnectedOrphanedSectors", numorphaned));
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
            Msg(X3Utils.Ui("Generator.RemovedGatesToProduceMoreEnclaves", numremoved));
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
            Msg(X3Utils.Ui("Generator.AverageSectorsPerRace", m_AvgSystemsPerRace));

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
            Msg(X3Utils.Ui("Generator.SectorsCreated", race, created1 + created2 + created3 + created4));
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
            int voidpct = X3Galaxy.Instance.GalaxyCreationSettings.EffectiveVoidPercent;
            bool isChaotic = X3Galaxy.Instance.GalaxyCreationSettings.ChaoticStart;
            X3Sector sector;

            var galaxy = X3Galaxy.Instance;
            int xenonpct = isChaotic ? 50 : GalaxySettings.ResolveXenonPercent(
                galaxy.GalaxyCreationSettings.XenonPercent, GalaxySettings.MinXenonPercent);
            int cells = galaxy.MaxX * galaxy.MaxY;
            int numSectors = isXenon ? cells * xenonpct / 100 : cells - galaxy.GalaxyCreationSettings.SectorTarget(cells, voidpct);


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

            string action = isXenon ? X3Utils.Ui("Generator.Xenon") : X3Utils.Ui("Generator.VoidGaps");
            Msg(X3Utils.Ui("Generator.GeneratingSectors", numSectors, action));

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
                if (sector == null) throw new Exception(X3Utils.Ui("Generator.UnableToPlaceSectorNoUnitializedSectors"));

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
                            if (!done && distance++ > 5)
                            {
                                // Could find no neighboring uninit sectors within 5 squares of any of these sectors.
                                // This usually occurs only on the first Xenon sector for a Chaotic Start, and also using the Shattered Galaxy option.  In this condition,
                                // you can have the first xenon system in a corner with no nearby neighbors.  Usually it will find one within 2 squares 90% of the time.
                                if (isVoid) sector.MarkSectorVoidorXenon(false);
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
