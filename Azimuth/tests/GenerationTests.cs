using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Azimuth
{
    // Run against a built Azimuth.exe. Synthetic scenery keeps these tests
    // independent of a game installation; the real Generator/topology code runs.
    internal static class GenerationTests
    {
        private static int failures, assertions;
        private static readonly List<string> csv = new List<string> { "case,sectors,seconds,result" };
        private static void Check(bool condition, string message)
        { assertions++; if (!condition) throw new Exception(message); }
        private static List<List<string>> Sky()
        { return new List<List<string>> { new List<string> { "<o t=\"3\" s=\"145\" x=\"100000\" y=\"100000\" z=\"100000\" color=\"16777160\"/>" } }; }
        private static void Run(int size, int shape, int start, EGalaxyExpansion expansion, bool limited, int trial)
        {
            var galaxy = new X3Galaxy(0, 0);
            var settings = galaxy.GalaxyCreationSettings;
            settings.GalaxySize = size; settings.MapType = shape;
            settings.VoidPercent = trial < 100 ? 15 : (trial % 3 == 0 ? 50 : trial % 3 == 1 ? 35 : 15); settings.XenonPercent = 10;
            settings.ChaoticStart = start == 2; settings.PeacefulStart = start == 1;
            settings.ClusteredXenons = start == 2 || trial % 2 == 0;
            settings.LimitedEnclaves = limited;
            settings.BorderGateDensity = trial % 2 == 0 ? EGateDensity.High : EGateDensity.Low;
            settings.GalaxyExpansion = expansion;
            var sizePixels = settings.PresetGridSize();
            string label = size + "-" + shape + "-" + start + "-" + expansion + "-" + limited + "-" + trial;
            var timer = Stopwatch.StartNew();
            try
            {
                var generator = new Generator(galaxy, trial);
                generator.GenerateGalaxy(sizePixels.Width, sizePixels.Height, new List<string> { "15" },
                    new List<string> { "0" }, Sky(), new List<List<string>> { new List<string>() });
                var rules = new GalaxyEditRules(galaxy);
                Check(rules.CurrentError() == null, "Generated structural contract");
                int percent = expansion == EGalaxyExpansion.Early ? 60 : expansion == EGalaxyExpansion.Average ? 40 : 20;
                var budget = TerritoryPlanner.Budget(galaxy.Sectors.Count, start == 2 ? 50 : 10, start == 1, percent, expansion == EGalaxyExpansion.Origin);
                Check(galaxy.Sectors.Count(s => s.r == 6) == budget[0], "Exact Xenon budget");
                Check(galaxy.Sectors.Count(s => s.r == 14) == budget[1], "Exact unknown budget");
                foreach (var sector in galaxy.Sectors.Where(s => rules.IsProtected(s)))
                {
                    Check(rules.RemovalError(sector) == "Edit.Protected", "Protected removal blocked");
                    Check(rules.OwnerError(sector, sector.r == 14 ? 1 : 14) == "Edit.Protected", "Protected transfer blocked");
                }
                foreach (var sector in galaxy.Sectors)
                    foreach (int owner in GalaxyEditRules.RaceIds.Where(o => o != sector.r))
                        if (rules.OwnerError(sector, owner) == null)
                            Check(sector.Gates.Any(g => galaxy.GetSectorAt(g.gx, g.gy).r == owner), "Recipient territory adjacency");
                var removable = galaxy.Sectors.FirstOrDefault(sector => rules.RemovalError(sector) == null);
                if (removable != null)
                {
                    var savedSectors = galaxy.Sectors.ToList();
                    var savedGates = galaxy.Sectors.ToDictionary(v => v, v => v.Gates.ToList());
                    try
                    {
                        foreach (var other in galaxy.Sectors) other.RemoveGates(removable);
                        galaxy.Sectors.Remove(removable);
                        Check(new GalaxyEditRules(galaxy).CurrentError() == null, "Accepted removal passes full validation");
                    }
                    finally
                    {
                        galaxy.Sectors.Clear(); galaxy.Sectors.AddRange(savedSectors);
                        foreach (var pair in savedGates) { pair.Key.Gates.Clear(); pair.Key.Gates.AddRange(pair.Value); }
                    }
                }
                bool checkedAddition = false;
                for (int x = 0; x < galaxy.MaxX && !checkedAddition; x++)
                    for (int y = 0; y < galaxy.MaxY && !checkedAddition; y++)
                    {
                        if (galaxy.GetSectorAt(x, y) != null) continue;
                        var choices = rules.AdditionChoices(x, y);
                        if (choices.Count == 0) continue;
                        var choice = choices.First();
                        var added = new X3Sector(x, y, "15", 0, Sky()[0], new List<string>());
                        added.MarkSectorRace((ERace)choice.Key);
                        try
                        {
                            galaxy.Sectors.Add(added);
                            foreach (int index in choice.Value) added.AddGates(rules.Sectors[index]);
                            Check(new GalaxyEditRules(galaxy).CurrentError() == null, "Accepted addition passes full validation");
                        }
                        finally
                        {
                            foreach (var other in galaxy.Sectors.ToList()) other.RemoveGates(added);
                            galaxy.Sectors.Remove(added);
                        }
                        checkedAddition = true;
                    }
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    generator.GenerateGates();
                    Check(new GalaxyEditRules(galaxy).CurrentError() == null, "Regeneration preserves rules");
                }
                Console.WriteLine("PASS " + label + " " + timer.Elapsed.TotalSeconds.ToString("F2") + "s");
                csv.Add(label + "," + galaxy.Sectors.Count + "," + timer.Elapsed.TotalSeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ",PASS");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("FAIL " + label + ": " + ex.Message);
                csv.Add(label + "," + galaxy.Sectors.Count + "," + timer.Elapsed.TotalSeconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ",FAIL");
            }
        }
        private static void AsteroidSelectionTests()
        {
            var sky = Sky()[0];
            var template = new List<string>();
            // Real asteroids: all s=0..2, including minerals Mayhem excludes.
            for (int i = 0; i < 120; i++)
                template.Add($"<o t=\"17\" s=\"{i % 3}\" x=\"{i * 500000}\" y=\"0\" z=\"{i * 300000}\" atype=\"{i % 2}\" aamount=\"{i % 40 + 1}\" a=\"0\" b=\"0\" g=\"0\"/>");
            template.Add("<o t=\"17\" s=\"1\" x=\"100000000\" y=\"0\" z=\"0\" atype=\"2\" aamount=\"20\" a=\"0\" b=\"0\" g=\"0\"/>");
            template.Add("<o t=\"17\" s=\"2\" x=\"110000000\" y=\"0\" z=\"0\" atype=\"3\" aamount=\"20\" a=\"0\" b=\"0\" g=\"0\"/>");

            // Debris: s=3..9, including Nividium and Ice (do not filter them out).
            for (int i = 0; i < 100; i++)
                template.Add($"<o t=\"17\" s=\"{3 + (i % 7)}\" x=\"{200000000 + i * 500000}\" y=\"0\" z=\"{i * 300000}\" atype=\"{i % 4}\" aamount=\"{i % 20 + 1}\" a=\"0\" b=\"0\" g=\"0\"/>");

            var realCounts = new HashSet<int>();
            var debrisCounts = new HashSet<int>();
            var layouts = new HashSet<string>();
            bool retainedNividiumDebris = false, retainedIceDebris = false;
            for (int seed = 0; seed < 1000; seed++)
            {
                var sector = new X3Sector(0, 0, "15", 0, sky, template, new Random(seed));
                var xml = new List<string>();
                sector.CreateXml(xml);
                var entries = xml.Where(line => line.Contains("t=\"17\"")).ToList();
                var real = entries.Where(line =>
                    line.Contains("s=\"0\"") || line.Contains("s=\"1\"") || line.Contains("s=\"2\"")).ToList();
                var debris = entries.Except(real).ToList();
                Check(real.Count <= 50, "Real asteroids capped at 50");
                Check(real.All(line => line.Contains("atype=\"0\"") || line.Contains("atype=\"1\"")),
                    "Real asteroids exclude Nividium and Ice");
                Check(debris.Count <= 100, "Debris limited to available source entries");
                Check(debris.All(line =>
                    !line.Contains("s=\"0\"") && !line.Contains("s=\"1\"") && !line.Contains("s=\"2\"")),
                    "Debris excludes large-asteroid subtypes");
                Check(entries.Distinct().Count() == entries.Count, "Sampling is without replacement");
                var spatialPositions = entries.Select(line => new X3Asteroid(line))
                    .Select(a => Tuple.Create(a.x, a.y, a.z)).ToList();
                Check(spatialPositions.Distinct().Count() == spatialPositions.Count,
                    "Asteroid and debris coordinates are unique within each sector");
                realCounts.Add(real.Count);
                debrisCounts.Add(debris.Count);
                layouts.Add(string.Join("|", entries));
                retainedNividiumDebris |= debris.Any(line => line.Contains("atype=\"2\""));
                retainedIceDebris |= debris.Any(line => line.Contains("atype=\"3\""));
            }
            Check(realCounts.Count == 1 && realCounts.Contains(50), "Overfull template always retains exactly 50 real asteroids");
            Check(debrisCounts.Contains(0) && debrisCounts.Contains(100), "Inclusive debris percentage range 0-100");
            Check(debrisCounts.Any(count => count > 50), "Debris not capped by large-asteroid limit");
            Check(retainedNividiumDebris && retainedIceDebris, "All debris minerals are retained");
            Check(layouts.Count > 100, "Random combined subsets differ across sectors");

            // Coordinates must genuinely change even when every real asteroid is retained.
            // All non-positional properties, notably subtype and mineral yield, stay intact.
            var fixedSource = new List<string> {
                "<o t=\"17\" s=\"0\" x=\"5000000\" y=\"500000\" z=\"2000000\" atype=\"0\" aamount=\"31\" a=\"11\" b=\"22\" g=\"33\"/>",
                "<o t=\"17\" s=\"2\" x=\"12000000\" y=\"-1000000\" z=\"-5000000\" atype=\"1\" aamount=\"17\" a=\"44\" b=\"55\" g=\"66\"/>"
            };
            var fixedLayouts = new HashSet<string>();
            for (int seed = 0; seed < 50; seed++)
            {
                var sector = new X3Sector(0, 0, "15", 0, sky, fixedSource, new Random(seed));
                var output = new List<string>();
                sector.CreateXml(output);
                var rocks = output.Where(line => line.Contains("t=\"17\"")).Select(line => new X3Asteroid(line)).OrderBy(a => a.s).ToList();
                Check(rocks.Count == 2, "Repositioning preserves real-asteroid count");
                Check(rocks[0].s == 0 && rocks[0].atype == 0 && rocks[0].aamount == 31 &&
                    rocks[0].a == 11 && rocks[0].b == 22 && rocks[0].g == 33,
                    "Repositioning preserves the first asteroid's properties");
                Check(rocks[1].s == 2 && rocks[1].atype == 1 && rocks[1].aamount == 17 &&
                    rocks[1].a == 44 && rocks[1].b == 55 && rocks[1].g == 66,
                    "Repositioning preserves the second asteroid's properties");
                Check(rocks[0].x != 5000000 || rocks[0].y != 500000 || rocks[0].z != 2000000,
                    "Asteroids are genuinely repositioned");
                Check(rocks.Select(a => Tuple.Create(a.x, a.y, a.z)).Distinct().Count() == 2,
                    "Repositioned asteroids do not share coordinates");
                fixedLayouts.Add(string.Join("|", rocks.Select(a => a.x + "," + a.y + "," + a.z)));
            }
            Check(fixedLayouts.Count > 40, "Different random seeds create different spatial layouts");

            // Below the 50 cap, all eligible real asteroids must be preserved for every seed.
            var underCap = new List<string>();
            for (int i = 0; i < 20; i++)
                underCap.Add($"<o t=\"17\" s=\"{i % 3}\" x=\"{i * 500000}\" y=\"0\" z=\"0\" atype=\"{i % 2}\" aamount=\"1\" a=\"0\" b=\"0\" g=\"0\"/>");
            for (int seed = 0; seed < 100; seed++)
            {
                var sector = new X3Sector(0, 0, "15", 0, sky, underCap, new Random(seed));
                var xml = new List<string>();
                sector.CreateXml(xml);
                Check(xml.Count(line => line.Contains("t=\"17\"")) == 20,
                    "Under-cap template preserves all 20 eligible real asteroids");
            }

            var empty = new X3Sector(0, 0, "15", 0, sky, new List<string>(), new Random(3));
            var emptyXml = new List<string>();
            empty.CreateXml(emptyXml);
            Check(!emptyXml.Any(line => line.Contains("t=\"17\"")), "Empty source yields zero asteroids and debris");

            // If the source has fewer than 50 real asteroids, never create new objects.
            var small = new List<string> {
                "<o t=\"17\" s=\"0\" x=\"0\" y=\"0\" z=\"0\" atype=\"0\" aamount=\"3\" a=\"0\" b=\"0\" g=\"0\"/>",
                "<o t=\"17\" s=\"7\" x=\"1\" y=\"0\" z=\"0\" atype=\"2\" aamount=\"3\" a=\"0\" b=\"0\" g=\"0\"/>"
            };
            for (int seed = 0; seed < 100; seed++)
            {
                var sector = new X3Sector(0, 0, "15", 0, sky, small, new Random(seed));
                var xml = new List<string>();
                sector.CreateXml(xml);
                var smallEntries = xml.Where(line => line.Contains("t=\"17\"")).ToList();
                Check(smallEntries.Count(line => line.Contains("s=\"0\"")) == 1,
                    "Single eligible asteroid is always retained");
                Check(smallEntries.Count <= 2, "Small source pool is respected");
            }
        }

        public static int Main(string[] args)
        {
            // Load English strings from the built app directory when present.
            string strings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localizations.json");
            if (File.Exists(strings)) X3Galaxy.Instance.LocalizationDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(strings));
            AsteroidSelectionTests();
            bool matrix = args.Contains("--matrix");
            for (int trial = 0; trial < 10; trial++) Run(0, 0, 2, EGalaxyExpansion.Advanced, trial % 2 == 0, trial);
            if (matrix)
                for (int size = 0; size < 3; size++)
                    for (int shape = 0; shape < 2; shape++)
                        for (int start = 0; start < 3; start++)
                            foreach (var expansion in new[] { EGalaxyExpansion.Origin, EGalaxyExpansion.Early, EGalaxyExpansion.Average, EGalaxyExpansion.Advanced })
                                Run(size, shape, start, expansion, false, 101 + size * 6 + shape * 3 + start);
            var bounds = new X3Galaxy(17, 17); bounds.GalaxyCreationSettings.GalaxySize = 2;
            Check(new GalaxyEditRules(bounds).CountAllowed(246), "Large retains 246 upper bound with generator rounding");
            Check(!new GalaxyEditRules(bounds).CountAllowed(247), "Large rejects 247");
            var previous = X3Galaxy.Instance.Sectors.ToArray();
            var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            bool observed = false;
            try { new Generator(new X3Galaxy(10, 10)) { Cancellation = cancellation.Token }.GenerateGalaxy(10, 10,
                new List<string> { "15" }, new List<string> { "0" }, Sky(), new List<List<string>> { new List<string>() }); }
            catch (OperationCanceledException) { observed = true; }
            Check(observed && previous.SequenceEqual(X3Galaxy.Instance.Sectors), "Cancellation leaves singleton galaxy untouched");
            File.WriteAllLines("generation-results.csv", csv);
            Console.WriteLine("Assertions: " + assertions + "; failed cases: " + failures + ". See generation-results.csv.");
            return failures == 0 ? 0 : 1;
        }
    }
}
