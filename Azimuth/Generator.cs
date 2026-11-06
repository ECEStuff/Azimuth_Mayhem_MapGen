// Builds territories, voids, Xenon space, gates and economic statistics for the in-memory galaxy.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;

namespace Azimuth
{
    class Generator
    {
        private Random m_Rand;
        private readonly X3Galaxy m_Galaxy;
        public CancellationToken Cancellation { get; set; }
        private Stopwatch generationTimer;
        public Generator(X3Galaxy galaxy = null, int? seed = null)
        {
            m_Galaxy = galaxy ?? X3Galaxy.Instance;
            m_Rand = seed.HasValue ? new Random(seed.Value) : new Random();
        }
        private int m_GlobID;
        private List<List<X3Sector>> m_Globs;
        private struct Match { public X3Sector Source; public X3Sector Dest; public int Distance; };

        public delegate void Message(string msg);
        public event Message OnMessage;


        private static int ScaledStat(int value, int fifths)
        {
            // Apply the generated modifier in fifths, rounded to a whole stat.
            return (fifths * value + 2) / 5;
        }

        public void GenerateSystemStats()
        {
            Msg(X3Utils.Ui("Generator.GeneratingNewAttributesForEachSector"));

            var settings = m_Galaxy.GalaxyCreationSettings;
            int fifths = settings.SaboteurMode ? 4 : settings.ExtraSystemStats ? 6 : 5;
            

            //<t id="10000101">5</t>            // Dejure Owner:   20% chance galaxy wide and always in same race's system.  Some races up to 5% dejure, while others at 1%.  odd.
            //<t id="10010101">0</t>            // smugglers:      Not used
            //<t id="10020101">48</t>           // Xenons:         Abnormal signals.  0-60.   30% of these are at 0. The other 70% average 32% signal level.
            //<t id="10030101">0</t>            // Terrans:        20% chance galaxy wide.
            //<t id="10040101">99</t>           // research:       40-100, average 70
            //<t id="10050101">10</t>           // support         Number of stations supported in system. 8-15, nominal mean 9.65 before tier rounding.
            //<t id="10060101">107</t>          // manpower        Population of system (60-130, average 95)
            //<t id="10070101">-1</t>           // cluster         Not Used.  Set to -1 to duplicate old program.
            //<t id="10080101">3 </t >          // production      Type of Food produced.  See EFood above for enumeration.
            //<t id="10090101">1</t>            // consumption     Type of food consumed.  See EFood.
            //<t id="10100101">4</t>            // company         4% chance galaxy wide for a company to exist in their race system.  + 1 company of every kind in unknown space.

            // Do not use RaceIsMain, because Xenon do not count toward de jure sectors.
            List<X3Sector> populatedSectors = m_Galaxy.Sectors.FindAll(item => (item.r >= (int)ERace.Argon && item.r <= (int)ERace.Teladi) || item.r == (int)ERace.Terran);
            List<X3Sector> allSectors = m_Galaxy.Sectors;

            // Clear existing sector data because we may be re-generating over and over.
            allSectors.ForEach(item => item.SectorCreationData = new SectorData());

            // Seed guarantees count toward the normal de jure allocation, rather than
            // adding an unpredictable extra six sectors on every stat reroll.
            var guaranteed = new List<X3Sector>();
            var seeds = settings.TerritorySeeds;
            if (seeds != null && seeds.GetLength(0) == 6 && seeds.GetLength(1) == 2)
                for (int r = 0; r < 6; r++)
                {
                    var seed = m_Galaxy.GetSectorAt(seeds[r, 0], seeds[r, 1]);
                    if (seed != null && seed.r == (int)TerritoryRaces[r]) guaranteed.Add(seed);
                }
            guaranteed.ForEach(item => item.SectorCreationData.DejureOwner = item.r);
            populatedSectors.Shuffle();
            int dejureCount = Math.Max(guaranteed.Count, (int)(allSectors.Count * 0.2));
            List<X3Sector> target = populatedSectors.Where(item => !guaranteed.Contains(item))
                .Take(dejureCount - guaranteed.Count).ToList();
            target.ForEach(item => item.SectorCreationData.DejureOwner = item.r);

            // Set 70% of all sectors to a non-zero abnormal signal level.  Leave 30% at 0.  
            allSectors.Shuffle();
            target = allSectors.Take((int)(allSectors.Count * 0.7)).ToList();
            target.ForEach(item => item.SectorCreationData.Xenons = m_Rand.Next(1, 61));

            // Set 20% of all sectors to have Terran Memory
            allSectors.Shuffle();
            target = allSectors.Take((int)(allSectors.Count * 0.2)).ToList();
            target.ForEach(item => item.SectorCreationData.Terrans = 1);


            // Floor each original tier independently; leftover sectors also receive 9.
            allSectors.Shuffle();
            int count = allSectors.Count;
            int endEight = count * 25 / 100;
            int endNine = endEight + count * 30 / 100;
            int endTen = endNine + count * 30 / 100;
            int endHigh = endTen + count * 15 / 100;
            for (int i = 0; i < count; i++)
            {
                var data = allSectors[i].SectorCreationData;
                if (i < endEight) data.Support = 8;
                else if (i < endNine) data.Support = 9;
                else if (i < endTen) data.Support = 10;
                else if (i < endHigh) data.Support = m_Rand.Next(11, 16);
                else data.Support = 9;
                data.Research = m_Rand.Next(40, 101);
                data.Manpower = m_Rand.Next(60, 131);
                if (fifths != 5)
                {
                    data.Research = ScaledStat(data.Research, fifths);
                    data.Manpower = ScaledStat(data.Manpower, fifths);
                }
            }

            // 9.65 = 193 / 20. Round half-station ties upward and retain all tier counts.
            // Only the high tier has room to move: up to four single-station passes span 11–15.
            int normalTarget = (193 * count + 10) / 20;
            int difference = normalTarget - allSectors.Sum(sector => sector.SectorCreationData.Support);
            int step = Math.Sign(difference);
            for (int pass = 0; pass < 4 && difference != 0; pass++)
            {
                for (int i = endTen; i < endHigh && difference != 0; i++)
                {
                    var data = allSectors[i].SectorCreationData;
                    int adjusted = data.Support + step;
                    if (adjusted < 11 || adjusted > 15) continue;
                    data.Support = adjusted;
                    difference -= step;
                }
            }

            if (fifths != 5)
            {
                // For Extra Stats, 8 * 1.2 rounds to the editable minimum of 10.
                // Exclude those already rounded up from the remaining station awards.
                int minimum = ScaledStat(8, fifths);
                int targetSupport = ScaledStat(normalTarget, fifths);
                var roundingOrder = allSectors
                    .Where(sector => sector.SectorCreationData.Support * fifths / 5 >= minimum)
                    .OrderByDescending(sector => sector.SectorCreationData.Support * fifths % 5).ToList();
                foreach (var sector in allSectors)
                    sector.SectorCreationData.Support = Math.Max(minimum, sector.SectorCreationData.Support * fifths / 5);

                int remaining = targetSupport - allSectors.Sum(sector => sector.SectorCreationData.Support);
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



        public void GenerateGalaxy(int maxx, int maxy, List<string> musicFiles, List<string> backgroundFiles,
            List<List<string>> sunAndPlanets, List<List<string>> asteroids)
        {
            generationTimer = Stopwatch.StartNew();
            for (int layout = 1; layout <= 8 && generationTimer.Elapsed.TotalSeconds < 60; layout++)
            {
                Cancellation.ThrowIfCancellationRequested();
                Msg(X3Utils.Ui("Generator.LayoutAttempt", layout));
                if (GenerateLayout(maxx, maxy, musicFiles, backgroundFiles, sunAndPlanets, asteroids)) return;
            }
            throw new InvalidOperationException(X3Utils.Ui("Generator.TerritoryFailure"));
        }

        private bool GenerateLayout(int maxx, int maxy, List<string>musicFiles, List<string>backgroundFiles, List<List<string>> sunAndPlanets, List<List<string>> asteroids)
        {
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

            m_Galaxy.Sectors.Clear();
            m_Galaxy.SetSize(maxx, maxy);
            m_Galaxy.GalaxyCreationSettings.GridWidth = maxx;
            m_Galaxy.GalaxyCreationSettings.GridHeight = maxy;


            // Create all sectors
            for (int x = 0; x < m_Galaxy.MaxX; x++)
            {
                for (int y = 0; y < m_Galaxy.MaxY; y++)
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
                    m_Galaxy.Sectors.Add(sector);                     // All sectors exist at start.  They are just uninitialized.
                }
            }

            GenerateVoidSectors();
            m_Galaxy.Sectors.RemoveAll(sector => sector.IsVoid);
            m_Galaxy.Sectors.ForEach(sector => sector.MarkSectorRace(ERace.Unknown));
            if (!GenerateTerritories()) return false;

            // Deal tracks only to surviving sectors. Every complete round uses each ID once.
            int musicIndex = 0;
            foreach (var sector in m_Galaxy.Sectors)
            {
                if (musicIndex == 0)
                    musicFiles.Shuffle();
                sector.m_MusicFileName = musicFiles[musicIndex++];
                if (musicIndex == musicFiles.Count)
                    musicIndex = 0;
            }

            int sectorCount = m_Galaxy.Sectors.Count;
            Msg(X3Utils.Ui("Generator.CreatedTotalSectors", sectorCount));
            return true;
        }


        // Gate topology is finalized before ownership. Regeneration validates the existing
        // territories and restores the previous gate graph if no safe replacement is found.
        private static readonly ERace[] TerritoryRaces =
            { ERace.Argon, ERace.Boron, ERace.Split, ERace.Paranid, ERace.Teladi, ERace.Terran };

        private int[][] GateGraph()
        {
            var sectors = m_Galaxy.Sectors;
            var index = sectors.Select((v, i) => new { v, i }).ToDictionary(v => v.v, v => v.i);
            return sectors.Select(v => v.Gates.Select(g => m_Galaxy.GetSectorAt(g.gx, g.gy))
                .Where(w => w != null).Select(w => index[w]).Distinct().ToArray()).ToArray();
        }

        private bool GenerateTerritories()
        {
            var galaxy = m_Galaxy;
            var settings = galaxy.GalaxyCreationSettings;
            int total = galaxy.Sectors.Count;
            int percent = settings.ChaoticStart ? 50 : GalaxySettings.ResolveXenonPercent(settings.XenonPercent, GalaxySettings.MinXenonPercent);
            int unknownPercent = settings.GalaxyExpansion == EGalaxyExpansion.Early ? 60
                : settings.GalaxyExpansion == EGalaxyExpansion.Average ? 40 : 20;
            int[] budget = TerritoryPlanner.Budget(total, percent, settings.PeacefulStart,
                unknownPercent, settings.GalaxyExpansion == EGalaxyExpansion.Origin);
            int xenon = budget[0];
            int owned = budget[2];
            if (budget[1] < TerritoryPlanner.RequiredUnknownEnclaves(settings.ChaoticStart) || owned < TerritoryPlanner.RaceCount)
                throw new InvalidOperationException(X3Utils.Ui("Generator.InvalidTerritoryBudget"));
            int[] spread = { settings.ArgonSpread, settings.BoronSpread, settings.SplitSpread,
                settings.ParanidSpread, settings.TeladiSpread, settings.TerranSpread };
            int[] targets = TerritoryPlanner.Quotas(owned, spread, m_Rand);
            if (settings.GalaxyExpansion == EGalaxyExpansion.Origin) targets = Enumerable.Repeat(1, 6).ToArray();
            // Retry topology as well as ownership; never weaken quotas or separation silently.
            for (int attempt = 0; attempt < 8 && generationTimer.Elapsed.TotalSeconds < 60; attempt++)
            {
                Msg(X3Utils.Ui("Generator.TerritoryAttempt", attempt + 1));
                BuildTopology();
                var graph = GateGraph();
                // Reserve the mode-specific unknown frontiers plus the Xenon portal.
                if (graph.Count(edges => edges.Length == 1) < TerritoryPlanner.RequiredUnknownEnclaves(settings.ChaoticStart) + 1) continue;
                var planner = new TerritoryPlanner(graph, m_Rand) { Cancellation = Cancellation };
                if (!planner.Plan(xenon, targets, settings.ClusteredXenons || settings.ChaoticStart, settings.ChaoticStart, 1000))
                { Msg(X3Utils.Ui("Generator.SearchRejectedReason", X3Utils.Ui(planner.LastFailure))); continue; }
                settings.TerritorySeeds = new int[6, 2];
                for (int i = 0; i < total; i++)
                {
                    int owner = planner.Owners[i];
                    galaxy.Sectors[i].MarkSectorRace(owner == 0 ? ERace.Unknown : owner == 7 ? ERace.Xenon : TerritoryRaces[owner - 1]);
                    // The planner's enclave seed is not a user-selected override.
                    galaxy.Sectors[i].IsMaelstromPortal = false;
                }
                for (int r = 0; r < 6; r++)
                {
                    settings.TerritorySeeds[r, 0] = galaxy.Sectors[planner.Seeds[r]].x;
                    settings.TerritorySeeds[r, 1] = galaxy.Sectors[planner.Seeds[r]].y;
                }
                if (new GalaxyEditRules(galaxy).CurrentError() != null) continue;
                Msg(X3Utils.Ui("Generator.TerritoryCounts", xenon, owned, total - xenon - owned));
                Msg(X3Utils.Ui("Generator.TerritoryChecks", planner.MinimumStartDistance,
                    planner.XenonEnclaveCount, planner.UnknownEnclaveCount));
                return true;
            }
            return false;
        }

        public void GenerateGates()
        {
            var galaxy = m_Galaxy;
            var original = galaxy.Sectors.ToDictionary(s => s, s => s.Gates.ToList());
            var coordinates = galaxy.GalaxyCreationSettings.TerritorySeeds;
            bool legacy = coordinates == null;
            if (!legacy && (coordinates.GetLength(0) != 6 || coordinates.GetLength(1) != 2))
                throw new InvalidOperationException(X3Utils.Ui("Generator.GateRegenerationFailure"));
            int[] owners = galaxy.Sectors.Select(s => s.r == (int)ERace.Xenon ? 7 : Array.FindIndex(TerritoryRaces, r => (int)r == s.r) + 1).ToArray();
            var settings = galaxy.GalaxyCreationSettings;
            var initial = GateGraph();
            var sectors = galaxy.Sectors;
            var candidates = sectors.Select(s => galaxy.GetNeighbors(s)
                .Select(n => sectors.IndexOf(n)).Where(i => i >= 0).Distinct().ToArray()).ToArray();
            var editRules = new GalaxyEditRules(galaxy);
            Func<int[][], bool> valid = graph =>
            {
                // Limited Enclaves may preserve existing dead ends, but rewiring
                // must not introduce additional ones when this option is enabled.
                if (settings.LimitedEnclaves && graph.Count(v => v.Length == 1) > initial.Count(v => v.Length == 1))
                    return false;
                var planner = new TerritoryPlanner(graph, m_Rand);
                return legacy
                    ? planner.IsConnected && Enumerable.Range(0, graph.Length).Count(i => owners[i] == 0 && graph[i].Length == 1) >= TerritoryPlanner.RequiredUnknownEnclaves(settings.ChaoticStart)
                    : editRules.GraphError(graph) == null;
            };
            var replacement = TerritoryPlanner.Rewire(initial, candidates, m_Rand, valid,
                settings.BorderGateDensity == EGateDensity.Low);
            if (replacement == null)
                throw new InvalidOperationException(X3Utils.Ui("Generator.GateInputInvalid"));
            int changes = Enumerable.Range(0, initial.Length).Sum(i =>
                initial[i].Except(replacement[i]).Count() + replacement[i].Except(initial[i]).Count()) / 2;
            if (changes == 0)
            {
                Msg(X3Utils.Ui("Generator.GatesUnchanged"));
                return;
            }
            bool accepted = false;
            try
            {
                // Apply only the delta. Unchanged gates retain their original objects
                // and special-gate metadata. Every edge is changed at both endpoints.
                for (int i = 0; i < sectors.Count; i++)
                {
                    foreach (int j in initial[i].Except(replacement[i]).Where(j => j > i))
                        sectors[i].RemoveGates(sectors[j]);
                    foreach (int j in replacement[i].Except(initial[i]).Where(j => j > i))
                        sectors[i].AddGates(sectors[j]);
                }
                if (!valid(GateGraph()))
                    throw new InvalidOperationException(X3Utils.Ui("Generator.GateRegenerationFailure"));
                accepted = true;
                Msg(X3Utils.Ui("Generator.GatesRewired", changes));
            }
            finally
            {
                if (!accepted) foreach (var pair in original) { pair.Key.Gates.Clear(); pair.Key.Gates.AddRange(pair.Value); }
            }
        }

        private bool GraphConnected()
        {
            var graph = GateGraph();
            if (graph.Length == 0) return false;
            var seen = new HashSet<int> { 0 };
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
                foreach (int next in graph[queue.Dequeue()])
                    if (seen.Add(next)) queue.Enqueue(next);
            return seen.Count == graph.Length;
        }

        private void EnsureFrontier()
        {
            var sectors = m_Galaxy.Sectors;
            int required = TerritoryPlanner.RequiredUnknownEnclaves(m_Galaxy.GalaxyCreationSettings.ChaoticStart) + 1;
            if (sectors.Count(v => v.Gates.Count == 1) >= required) return;
            // Even Limited Enclaves must supply four unknown leaves and a portal leaf.
            // Remove only cycle edges, keeping the complete graph connected.
            foreach (var source in sectors.OrderBy(v => m_Rand.Next()).OrderBy(v => v.Gates.Count).ToList())
            {
                if (sectors.Count(v => v.Gates.Count == 1) >= required) return;
                if (source.Gates.Count <= 1) continue;
                var saved = sectors.ToDictionary(v => v, v => v.Gates.ToList());
                foreach (var gate in source.Gates.ToList().OrderBy(v => m_Rand.Next()))
                {
                    if (source.Gates.Count == 1) break;
                    var dest = m_Galaxy.GetSectorAt(gate.gx, gate.gy);
                    if (dest == null || dest.Gates.Count <= 1) continue;
                    var beforeSource = source.Gates.ToList();
                    var beforeDest = dest.Gates.ToList();
                    source.RemoveGates(dest);
                    if (!GraphConnected())
                    {
                        source.Gates.Clear(); source.Gates.AddRange(beforeSource);
                        dest.Gates.Clear(); dest.Gates.AddRange(beforeDest);
                    }
                }
                if (source.Gates.Count == 1) continue;
                foreach (var pair in saved) { pair.Key.Gates.Clear(); pair.Key.Gates.AddRange(pair.Value); }
            }
        }

        private void BuildTopology()
        {
            lastmatchesadded.Clear();
            m_Galaxy.ClearAllGates();

            // 1. Create gates to every possible direct neighbor.
            Msg(X3Utils.Ui("Generator.CreatingGates"));
            for (int x = 0; x < m_Galaxy.MaxX; x++)
            {
                for (int y = 0; y < m_Galaxy.MaxY; y++)
                {
                    X3Sector source = m_Galaxy.GetSectorAt(x, y);
                    if (source != null)
                    {
                        List<X3Sector> neighbors = m_Galaxy.GetNeighbors(source);
                        foreach (X3Sector neighbor in neighbors)
                        {
                            source.AddGates(neighbor);
                        }
                    }
                }
            }

            // Prune before ownership exists. The density setting now controls the whole network.
            int minimum = m_Galaxy.GalaxyCreationSettings.LimitedEnclaves ? 2 : 1;
            int chance = m_Galaxy.GalaxyCreationSettings.BorderGateDensity == EGateDensity.Low ? 70 : 20;
            var sources = m_Galaxy.Sectors.OrderBy(v => m_Rand.Next()).ToList();
            foreach (var source in sources)
                foreach (var dest in m_Galaxy.GetNeighbors(source))
                    if ((source.x < dest.x || (source.x == dest.x && source.y < dest.y))
                        && source.IsConnectedTo(dest) && source.Gates.Count > minimum && dest.Gates.Count > minimum
                        && m_Rand.Next(100) < chance) source.RemoveGates(dest);

            // 3.
            if (!m_Galaxy.GalaxyCreationSettings.LimitedEnclaves)
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
            EnsureFrontier();

            int gateCount = m_Galaxy.Sectors.Sum(item => item.Gates.Count);
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
            m_Galaxy.Sectors.ForEach(item => item.IsOrphaned = true); 
            m_GlobID = 0;

            while (m_Galaxy.Sectors.Exists(sec => sec.IsOrphaned))
            {
                // Create a new Glob
                List<X3Sector> glob = new List<X3Sector>();

                X3Sector start = m_Galaxy.Sectors.Find(item => item.IsOrphaned);  // Just pick any old orphaned sector to start with.
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
                List<X3Sector> possibleDestinations = m_Galaxy.Sectors.FindAll(item => item.GlobID != curGlobId);
                foreach (X3Sector srcsec in srcglob)
                {
                    foreach (X3Sector dstsec in possibleDestinations)
                    {
                        if (srcsec.x == dstsec.x || srcsec.y == dstsec.y)       // Only include destinations that are on the same vertical or horizontal axis.  nothing horizontal.
                        {
                            int dst = srcsec.DistanceFrom(dstsec);
                            // A usable link must have a clear, axis-aligned path.
                            // Evaluate the map itself, rather than its configured void %.
                            if (!m_Galaxy.HasNonVoidSectorsBetween(srcsec, dstsec))
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
                    int destmaxgates = (int)Math.Sqrt(m_Galaxy.Sectors.Count(item => item.GlobID == dst.GlobID));
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
                foreach (X3Sector globsecneighbor in m_Galaxy.GetConnectedNeighbors(globsec).FindAll(sec => sec.IsOrphaned))
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
                }
            }
        }




        public void ConnectOrphanedSectors()
        {
            int numorphaned = 0;
            foreach (X3Sector source in m_Galaxy.Sectors)
            {
                if (source.Gates.Count == 0)
                {
                    // Retain the original six-sector neighborhood whenever possible.
                    X3Sector dest = m_Galaxy.GetNearestRandomNeighbor(source);
                    if (dest == null)
                    {
                        // Sparse or shattered layouts may have no nearby sector.
                        // Find the closest unobstructed sector along either axis.
                        List<X3Sector> candidates = m_Galaxy.Sectors
                            .Where(candidate => candidate != source
                                && (candidate.x == source.x || candidate.y == source.y)
                                && !source.IsConnectedTo(candidate)
                                && !m_Galaxy.HasNonVoidSectorsBetween(source, candidate))
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

            foreach (X3Sector source in m_Galaxy.Sectors)
            {
                List<X3Sector> neighbors = m_Galaxy.GetNeighbors(source);
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
        /// We actually place an empty/non-existent/void sector into the sectors list as a placeholder, but they get removed after everything else is generated.
        /// IsVoid = true
        /// 
        /// </summary>
        private void GenerateVoidSectors()
        {
            var galaxy = m_Galaxy;
            int cells = galaxy.MaxX * galaxy.MaxY;
            int count = cells - galaxy.GalaxyCreationSettings.SectorTarget(cells,
                galaxy.GalaxyCreationSettings.EffectiveVoidPercent);
            for (int i = 0; i < count; i++)
            {
                var sector = galaxy.GetRandomUninitializedSector();
                if (galaxy.GalaxyCreationSettings.ClusteredVoids && i >= count / 2)
                {
                    var existing = galaxy.Sectors.Where(v => v.IsVoid).OrderBy(v => m_Rand.Next()).ToList();
                    bool found = false;
                    for (int distance = 1; distance <= 6 && !found; distance++)
                        foreach (var gap in existing)
                        {
                            var nearby = galaxy.GetNeighbors(gap, distance).Where(v => !v.IsInitialized).ToList();
                            if (nearby.Count == 0) continue;
                            sector = nearby[m_Rand.Next(nearby.Count)];
                            found = true;
                            break;
                        }
                }
                sector.MarkSectorVoidorXenon(false);
            }
        }

        private void Msg(string msg)
        {
            Cancellation.ThrowIfCancellationRequested();
            OnMessage?.Invoke(msg);
        }


    }
}
