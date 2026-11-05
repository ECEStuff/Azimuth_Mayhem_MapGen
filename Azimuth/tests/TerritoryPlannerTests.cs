using System;
using System.Collections.Generic;
using System.Linq;
namespace Azimuth
{
    internal static class TerritoryPlannerTests
    {
        private static int assertions;
        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }
        private static int[][] Fixture()
        {
            // A path with five unknown leaf branches, plus a Xenon portal at the far end.
            var links = Enumerable.Range(0, 60).Select(i => new List<int>()).ToArray();
            for (int i = 0; i < 54; i++) { links[i].Add(i + 1); links[i + 1].Add(i); }
            for (int i = 0; i < 5; i++) { links[i * 10].Add(55 + i); links[55 + i].Add(i * 10); }
            return links.Select(v => v.ToArray()).ToArray();
        }
        public static void Main()
        {
            var rng = new Random(19);
            for (int count = 50; count <= 246; count++)
                foreach (int percent in Enumerable.Range(3, 8).Concat(new[] { 50 }))
                    foreach (int unknownPercent in new[] { 60, 40, 20 })
                    {
                        var budget = TerritoryPlanner.Budget(count, percent, false, unknownPercent, false);
                        Check(budget[0] == (int)Math.Ceiling(count * percent / 100m), "Xenon ceiling");
                        Check(budget[1] == (int)Math.Ceiling((count - budget[0]) * unknownPercent / 100m), "Non-Xenon denominator");
                        Check(budget.Sum() == count && budget[1] >= 4 && budget[2] >= 6, "Budget bounds");
                    }
            Check(TerritoryPlanner.Budget(85, 50, false, 60, false).SequenceEqual(new[] { 43, 26, 16 }), "Chaotic Early");
            Check(TerritoryPlanner.Budget(85, 50, false, 40, false).SequenceEqual(new[] { 43, 17, 25 }), "Chaotic Average");
            Check(TerritoryPlanner.Budget(85, 50, false, 20, false).SequenceEqual(new[] { 43, 9, 33 }), "Chaotic Advanced");
            Check(TerritoryPlanner.Budget(85, 50, false, 60, true).SequenceEqual(new[] { 43, 36, 6 }), "Origin");
            Check(TerritoryPlanner.Budget(85, 50, true, 60, false)[0] == 1, "Peaceful override");
            for (int total = 6; total <= 246; total++)
                foreach (var spread in new[] { new int[6], new[] { 40, 0, 0, 0, 0, 0 }, new[] { -100, 100, 100, -100, 0, 0 } })
                {
                    var targets = TerritoryPlanner.Quotas(total, spread, rng);
                    Check(targets.Sum() == total && targets.All(n => n >= 1), "Exact race budget");
                }
            Check(TerritoryPlanner.Quotas(60, Enumerable.Repeat(40, 6).ToArray(), rng).All(v => v == 10), "Equal spreads cancel");
            int[] seeds = { 5, 13, 21, 29, 37, 45 };
            var owners = new int[60];
            for (int r = 0; r < 6; r++) for (int i = 0; i < 3; i++) owners[seeds[r] + i] = r + 1;
            owners[54] = 7;
            var graph = Fixture();
            var planner = new TerritoryPlanner(graph, rng);
            var targets3 = Enumerable.Repeat(3, 6).ToArray();
            Check(planner.Validate(owners, seeds, targets3, 1, true, true, 54), "Valid separated territories");
            var trapped = (int[])owners.Clone();
            trapped[4] = trapped[8] = 7;
            Check(!planner.Validate(trapped, seeds, targets3, 3, false, false, 54),
                "Connected race trapped between Xenon has no expansion exit");
            var originOwners = new int[60];
            for (int r = 0; r < 6; r++) originOwners[seeds[r]] = r + 1;
            originOwners[54] = 7;
            var targets1 = Enumerable.Repeat(1, 6).ToArray();
            Check(planner.Validate(originOwners, seeds, targets1, 1, false, false, 54), "Origin exits accepted");
            originOwners[4] = originOwners[6] = 7;
            Check(!planner.Validate(originOwners, seeds, targets1, 3, false, false, 54),
                "Origin seed surrounded by Xenon rejected despite global connectivity");
            Func<int[][], bool> validNetwork = g => new TerritoryPlanner(g, new Random(1))
                .Validate(owners, seeds, targets3, 1, true, true, 54);
            var alternatives = graph.Select(v => v.ToList()).ToArray();
            alternatives[16].Add(18); alternatives[18].Add(16);
            var candidateGraph = alternatives.Select(v => v.ToArray()).ToArray();
            bool sawChange = false;
            for (int trial = 0; trial < 20; trial++)
            {
                var rewired = TerritoryPlanner.Rewire(graph, candidateGraph, new Random(trial), validNetwork, trial % 2 == 0);
                Check(rewired != null && validNetwork(rewired), "Rewiring preserves all territory constraints");
                sawChange |= rewired[16].Contains(18);
                Check(Enumerable.Range(0, graph.Length).All(i => rewired[i].All(j => rewired[j].Contains(i))), "Reciprocal gates");
                Check(Enumerable.Range(0, graph.Length).All(i => rewired[i].All(j => candidateGraph[i].Contains(j))), "Only eligible edges");
            }
            Check(sawChange, "Rewiring can add a safe alternative connection");
            var unchanged = TerritoryPlanner.Rewire(graph, graph, new Random(7), validNetwork, true);
            Check(unchanged != null && Enumerable.Range(0, graph.Length).All(i => graph[i].OrderBy(v => v)
                .SequenceEqual(unchanged[i].OrderBy(v => v))), "Tree with no safe edits remains valid, without failure");
            Check(TerritoryPlanner.Rewire(graph, candidateGraph, new Random(9), g => false, false) == null,
                "Invalid input is distinguished from no available gate changes");
            Check(Enumerable.Range(0, graph.Length).All(i => graph[i].SequenceEqual(Fixture()[i])), "Rewiring does not mutate input");
            var twoLeaves = graph.Select(v => v.ToList()).ToArray();
            twoLeaves[55].Add(56); twoLeaves[56].Add(55);
            twoLeaves[56].Add(57); twoLeaves[57].Add(56);
            var twoPlanner = new TerritoryPlanner(twoLeaves.Select(v => v.ToArray()).ToArray(), rng);
            Check(twoPlanner.Validate(owners, seeds, targets3, 1, true, true, 54), "Chaotic accepts two unknown enclaves");
            Check(!twoPlanner.Validate(owners, seeds, targets3, 1, true, false, 54), "Standard still requires four unknown enclaves");
            Check(TerritoryPlanner.RequiredUnknownEnclaves(true) == 2 && TerritoryPlanner.RequiredUnknownEnclaves(false) == 4, "Mode-specific minimum");
            var cancelled = new System.Threading.CancellationTokenSource(); cancelled.Cancel();
            bool cancellationObserved = false;
            try { new TerritoryPlanner(graph, rng) { Cancellation = cancelled.Token }.Plan(1, targets3, false, false); }
            catch (OperationCanceledException) { cancellationObserved = true; }
            Check(cancellationObserved, "Planner honours cancellation");
            var disconnected = (int[])owners.Clone();
            disconnected[15] = 0; disconnected[50] = 2;
            Check(!planner.Validate(disconnected, seeds, targets3, 1, false, false, 54), "Disconnected race rejected");
            var touching = (int[])owners.Clone();
            touching[15] = 0; touching[8] = 2;
            Check(!planner.Validate(touching, seeds, targets3, 1, false, false, 54), "Touching territories rejected");
            Check(!planner.Validate(owners, seeds, targets3, 2, false, false, 54), "Wrong Xenon budget rejected");
            Check(!planner.Validate(owners, seeds, targets3, 1, false, false, 53), "Non-enclave/non-Xenon portal rejected");
            var multiple = (int[])owners.Clone(); multiple[55] = 7;
            Check(planner.Validate(multiple, seeds, targets3, 2, true, false, 54), "Standard permits separate enclave clusters");
            Check(!planner.Validate(multiple, seeds, targets3, 2, true, true, 54), "Chaotic requires one cluster");
            multiple[56] = 7;
            Check(!planner.Validate(multiple, seeds, targets3, 3, false, false, 54), "Three unknown enclaves are insufficient");
            var shortcut = graph.Select(v => v.ToList()).ToList();
            shortcut.Add(new List<int> { 5, 13 }); shortcut[5].Add(60); shortcut[13].Add(60);
            Check(!new TerritoryPlanner(shortcut.Select(v => v.ToArray()).ToArray(), rng)
                .Validate(owners.Concat(new[] { 0 }).ToArray(), seeds, targets3, 1, false, false, 54), "Two-jump seed spacing rejected");
            foreach (bool clustered in new[] { false, true })
            {
                var plan = new TerritoryPlanner(Fixture(), new Random(clustered ? 22 : 21));
                var targets = Enumerable.Repeat(1, 6).ToArray();
                Check(plan.Plan(2, targets, clustered, false, 4000), "Origin planning");
                Check(plan.Validate(plan.Owners, plan.Seeds, targets, 2, clustered, false, plan.Portal), "Generated plan validation");
                if (!clustered) Check(plan.Owners.Where((owner, i) => owner == 7 && graph[i].Length == 1).Count() == 2,
                    "Unclustered Xenon exhaust available enclaves first");
            }
            var broken = Fixture(); broken[26] = new[] { 25 }; broken[27] = new[] { 28 };
            Check(!new TerritoryPlanner(broken, rng).Plan(1, targets3, false, false), "Disconnected map rejected");
            Console.WriteLine("Passed " + assertions + " assertions.");
        }
    }
}
