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
        public static int Main(string[] args)
        {
            // Load English strings from the built app directory when present.
            string strings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localizations.json");
            if (File.Exists(strings)) X3Galaxy.Instance.LocalizationDictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(strings));
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
