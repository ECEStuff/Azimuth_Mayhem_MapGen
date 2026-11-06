// Ownership planning on the final gate graph. Plans are isolated from live galaxy data.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Azimuth
{
    internal sealed class TerritoryPlanner
    {
        public const int RaceCount = 6;
        public const int MinimumUnknownEnclaves = 4;
        public static int RequiredUnknownEnclaves(bool chaotic) { return chaotic ? 2 : 4; }
        public CancellationToken Cancellation { get; set; }
        public string LastFailure { get; private set; }
        public const int MinimumSeedDistance = 4;
        private readonly int[][] graph;
        private readonly Random random;
        private readonly int[][] distances;
        private readonly int[] leaves;
        public int[] Owners { get; private set; } // 0 unknown, 1..6 races, 7 Xenon
        public int[] Seeds { get; private set; }
        public int Portal { get; private set; }
        public bool IsConnected { get { return graph.Length > 0 && distances[0].All(d => d >= 0); } }
        public int MinimumStartDistance
        {
            get { return Seeds == null ? 0 : Seeds.Min(v => Seeds.Where(w => w != v).Min(w => distances[v][w])); }
        }
        public int XenonEnclaveCount
        {
            get { return Owners == null ? 0 : leaves.Count(v => Owners[v] == 7); }
        }
        public int UnknownEnclaveCount
        {
            get { return Owners == null ? 0 : leaves.Count(v => Owners[v] == 0); }
        }

        public TerritoryPlanner(int[][] graph, Random random)
        {
            this.graph = graph;
            this.random = random;
            distances = Enumerable.Range(0, graph.Length).Select(Distances).ToArray();
            leaves = Enumerable.Range(0, graph.Length).Where(v => graph[v].Length == 1).ToArray();
        }

        // Explore changes from a valid network, never replace it with an unchecked
        // random topology. Candidate additions are supplied by the geometric map.
        // Return null only when the INPUT violates the contract; an unchanged valid
        // network is a normal outcome when no safe alternative is found.
        public static int[][] Rewire(int[][] initial, int[][] candidates, Random random,
            Func<int[][], bool> valid, bool lowDensity)
        {
            if (!valid(initial)) return null;
            var current = initial.Select(v => new HashSet<int>(v)).ToArray();
            var pairs = new List<int[]>();
            for (int v = 0; v < initial.Length; v++)
                foreach (int w in candidates[v].Concat(initial[v]).Distinct())
                    if (w > v) pairs.Add(new[] { v, w });
            int[][] best = initial.Select(v => v.ToArray()).ToArray();
            int bestDifference = 0;
            for (int pass = 0; pass < 2; pass++)
                foreach (var pair in pairs.OrderBy(v => random.Next()).Take(100))
                {
                    int a = pair[0], b = pair[1];
                    bool remove = current[a].Contains(b);
                    if (random.Next(100) >= (remove ? (lowDensity ? 85 : 35) : (lowDensity ? 35 : 85))) continue;
                    if (remove) { current[a].Remove(b); current[b].Remove(a); }
                    else { current[a].Add(b); current[b].Add(a); }
                    var proposed = current.Select(v => v.ToArray()).ToArray();
                    if (!valid(proposed))
                    {
                        if (remove) { current[a].Add(b); current[b].Add(a); }
                        else { current[a].Remove(b); current[b].Remove(a); }
                        continue;
                    }
                    // Keep a changed result even if later random edits undo it.
                    int difference = Enumerable.Range(0, initial.Length)
                        .Sum(v => initial[v].Except(proposed[v]).Count() + proposed[v].Except(initial[v]).Count());
                    if (difference > bestDifference) { bestDifference = difference; best = proposed; }
                }
            return best;
        }

        // All percentage arithmetic is integer ceiling. Unknown uses NON-XENON sectors.
        public static int[] Budget(int count, int xenonPercent, bool peaceful, int unknownPercent, bool origin)
        {
            if (count < 1 || xenonPercent < 0 || xenonPercent > 100 || unknownPercent < 0 || unknownPercent > 100)
                throw new ArgumentOutOfRangeException();
            int xenon = peaceful ? 1 : (count * xenonPercent + 99) / 100;
            int remaining = count - xenon;
            int unknown = origin ? remaining - RaceCount : (remaining * unknownPercent + 99) / 100;
            return new[] { xenon, unknown, remaining - unknown };
        }

        // Relative spread adjustment around the mean; normalize after clamping at zero.
        // Largest remainders preserve the exact budget and at least one sector per race.
        public static int[] Quotas(int total, int[] spread, Random random)
        {
            if (total < RaceCount || spread.Length != RaceCount) throw new ArgumentException("Invalid race budget.");
            double average = spread.Average();
            double[] weights = spread.Select(v => Math.Max(0.0, 100.0 + v - average)).ToArray();
            double sum = weights.Sum();
            double[] ideal = weights.Select(w => sum > 0 ? total * w / sum : total / (double)RaceCount).ToArray();
            int[] result = ideal.Select(v => Math.Max(1, (int)Math.Floor(v))).ToArray();
            int[] tieOrder = Enumerable.Range(0, RaceCount).OrderBy(i => random.Next()).ToArray();
            while (result.Sum() < total)
                result[tieOrder.OrderByDescending(i => ideal[i] - result[i]).First()]++;
            while (result.Sum() > total)
                result[tieOrder.Where(i => result[i] > 1).OrderByDescending(i => result[i] - ideal[i]).First()]--;
            return result;
        }

        private int[] Distances(int start)
        {
            var result = Enumerable.Repeat(-1, graph.Length).ToArray();
            var queue = new Queue<int>();
            result[start] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int v = queue.Dequeue();
                foreach (int w in graph[v]) if (result[w] < 0)
                {
                    result[w] = result[v] + 1;
                    queue.Enqueue(w);
                }
            }
            return result;
        }

        // Maximize the nearest pair, then balance nearest-rival distances. This is a
        // bounded heuristic, not a claim of globally optimal/equidistant placement.
        private long SeedScore(int[] seeds)
        {
            int min = int.MaxValue, maxNearest = 0, total = 0;
            for (int i = 0; i < RaceCount; i++)
            {
                int nearest = int.MaxValue;
                for (int j = 0; j < RaceCount; j++) if (i != j)
                {
                    int d = distances[seeds[i]][seeds[j]];
                    nearest = Math.Min(nearest, d);
                    total += d;
                }
                min = Math.Min(min, nearest);
                maxNearest = Math.Max(maxNearest, nearest);
            }
            return min * 10000000L - (maxNearest - min) * 10000L + total;
        }

        private List<int[]> SeedSets(int[] xenonOwners, HashSet<int> reserved, Stopwatch timer, int milliseconds)
        {
            var candidates = new List<int[]>();
            int[] eligible = Enumerable.Range(0, graph.Length)
                .Where(v => xenonOwners[v] == 0 && !reserved.Contains(v)
                    && graph[v].Any(w => xenonOwners[w] == 0)).ToArray();
            foreach (int first in eligible.OrderBy(v => random.Next()).Take(24))
            {
                if (timer.ElapsedMilliseconds >= milliseconds) break;
                var selected = new List<int> { first };
                while (selected.Count < RaceCount)
                {
                    var choices = eligible.Where(v => !selected.Contains(v)).ToArray();
                    if (choices.Length == 0) break;
                    int best = choices.Max(v => selected.Min(s => distances[s][v]));
                    if (best < MinimumSeedDistance) break;
                    var tied = choices.Where(v => selected.Min(s => distances[s][v]) == best).ToArray();
                    selected.Add(tied[random.Next(tied.Length)]);
                }
                if (selected.Count == RaceCount) candidates.Add(selected.ToArray());
            }
            // Refine the best initial layouts with one coordinate-descent pass.
            foreach (var seeds in candidates.OrderByDescending(SeedScore).Take(4))
            {
                long score = SeedScore(seeds);
                for (int r = 0; r < RaceCount; r++)
                {
                    if (timer.ElapsedMilliseconds >= milliseconds) break;
                    int best = seeds[r];
                    foreach (int v in eligible)
                    {
                        if (seeds.Contains(v)) continue;
                        int old = seeds[r]; seeds[r] = v;
                        long candidateScore = SeedScore(seeds);
                        seeds[r] = old;
                        if (candidateScore > score) { score = candidateScore; best = v; }
                    }
                    seeds[r] = best;
                }
            }
            return candidates.OrderByDescending(SeedScore).ToList();
        }

        // A cluster grows by BFS from an enclave seed. At the same BFS depth, take
        // enclave candidates first. Unknown reservations are never traversed/claimed.
        private bool GrowXenon(int[] owners, int seed, int target, HashSet<int> reserved)
        {
            var queue = new Queue<int>();
            var seen = new HashSet<int> { seed };
            owners[seed] = 7;
            queue.Enqueue(seed);
            int count = 1;
            while (queue.Count > 0 && count < target)
            {
                int source = queue.Dequeue();
                foreach (int v in graph[source].OrderBy(v => random.Next()).OrderBy(v => graph[v].Length))
                {
                    if (!seen.Add(v) || reserved.Contains(v) || owners[v] != 0) continue;
                    owners[v] = 7;
                    queue.Enqueue(v);
                    if (++count == target) break;
                }
            }
            return count == target;
        }

        private int[] PlaceXenon(int xenonCount, bool clustered, bool chaotic, int portal, HashSet<int> reserved)
        {
            var owners = new int[graph.Length];
            if (!clustered && !chaotic)
            {
                owners[portal] = 7;
                // This achieves the maximum possible enclave count for this reservation.
                var eligible = Enumerable.Range(0, graph.Length).Where(v => v != portal && !reserved.Contains(v))
                    .OrderBy(v => random.Next()).OrderBy(v => graph[v].Length == 1 ? 0 : 1);
                foreach (int v in eligible.Take(xenonCount - 1)) owners[v] = 7;
                return owners;
            }
            if (chaotic) return GrowXenon(owners, portal, xenonCount, reserved) ? owners : null;

            // Standard clustered mode may have several BFS clusters. Each normally has
            // at least two sectors; a total Xenon budget of one is the only exception.
            int maxClusters = Math.Min(leaves.Length - reserved.Count, Math.Max(1, xenonCount / 2));
            int clusterCount = 1 + random.Next(maxClusters);
            var starts = new List<int> { portal };
            starts.AddRange(leaves.Where(v => v != portal && !reserved.Contains(v)).OrderBy(v => random.Next()).Take(clusterCount - 1));
            var allReserved = new HashSet<int>(reserved.Concat(starts));
            for (int c = 0; c < starts.Count; c++)
            {
                int target = xenonCount / starts.Count + (c < xenonCount % starts.Count ? 1 : 0);
                allReserved.Remove(starts[c]);
                if (!GrowXenon(owners, starts[c], target, allReserved)) return null;
            }
            return owners;
        }

        private int[] GrowRaces(int[] xenonOwners, int[] seeds, int[] targets, HashSet<int> reserved)
        {
            int[] owners = (int[])xenonOwners.Clone();
            for (int r = 0; r < RaceCount; r++)
            {
                var reachable = new HashSet<int> { seeds[r] };
                var pending = new Queue<int>();
                pending.Enqueue(seeds[r]);
                while (pending.Count > 0 && reachable.Count < targets[r])
                    foreach (int v in graph[pending.Dequeue()])
                        if (owners[v] == 0 && !reserved.Contains(v) && reachable.Add(v)) pending.Enqueue(v);
                if (reachable.Count < targets[r]) return null;
            }
            var queues = new Queue<int>[RaceCount];
            var seen = new HashSet<int>[RaceCount];
            int[] counts = Enumerable.Repeat(1, RaceCount).ToArray();
            for (int r = 0; r < RaceCount; r++)
            {
                owners[seeds[r]] = r + 1;
                queues[r] = new Queue<int>();
                seen[r] = new HashSet<int> { seeds[r] };
                queues[r].Enqueue(seeds[r]);
            }
            bool progress = true;
            while (progress && !counts.SequenceEqual(targets))
            {
                progress = false;
                foreach (int r in Enumerable.Range(0, RaceCount).OrderBy(v => random.Next()))
                {
                    if (counts[r] >= targets[r]) continue;
                    int layer = queues[r].Count;
                    while (layer-- > 0 && counts[r] < targets[r])
                    {
                        int source = queues[r].Dequeue();
                        foreach (int v in graph[source].OrderBy(v => random.Next()))
                        {
                            if (!seen[r].Add(v) || owners[v] != 0 || reserved.Contains(v)) continue;
                            if (graph[v].Any(w => owners[w] >= 1 && owners[w] <= RaceCount && owners[w] != r + 1)) continue;
                            owners[v] = r + 1;
                            queues[r].Enqueue(v);
                            counts[r]++;
                            progress = true;
                            if (counts[r] == targets[r]) break;
                        }
                    }
                }
            }
            return counts.SequenceEqual(targets) ? owners : null;
        }

        public bool Plan(int xenonCount, int[] targets, bool clustered, bool chaotic, int milliseconds = 1500)
        {
            Cancellation.ThrowIfCancellationRequested();
            int required = RequiredUnknownEnclaves(chaotic);
            LastFailure = "Edit.Budget";
            Owners = null; Seeds = null; Portal = -1;
            if (graph.Length == 0 || distances[0].Any(d => d < 0) || targets.Length != RaceCount
                || targets.Any(t => t < 1) || xenonCount < 1
                || graph.Length - targets.Sum() - xenonCount < required
                || leaves.Length < required + 1) return false;
            var timer = Stopwatch.StartNew();
            long bestScore = long.MinValue;
            for (int proposal = 0; proposal < 64 && timer.ElapsedMilliseconds < milliseconds; proposal++)
            {
                Cancellation.ThrowIfCancellationRequested();
                int portal = leaves[random.Next(leaves.Length)];
                // In Chaotic, favor unknown frontier reservations far from the portal;
                // vary the selection so a poor reservation cannot block all attempts.
                var unknownCandidates = leaves.Where(v => v != portal).OrderBy(v => random.Next());
                var reserved = new HashSet<int>((chaotic && proposal % 2 == 0
                    ? unknownCandidates.OrderByDescending(v => distances[portal][v])
                    : unknownCandidates).Take(required));
                int[] xenonOwners = PlaceXenon(xenonCount, clustered, chaotic, portal, reserved);
                if (xenonOwners == null) { LastFailure = "Edit.Xenon"; continue; }
                int enclaveScore = leaves.Count(v => xenonOwners[v] == 7);
                // Frontier reservations and seed spacing are independent of expansion level.
                LastFailure = "Edit.Spacing";
                foreach (int[] locations in SeedSets(xenonOwners, reserved, timer, milliseconds).Take(12))
                {
                    if (timer.ElapsedMilliseconds >= milliseconds) break;
                    for (int trial = 0; trial < 3; trial++)
                    {
                        int[] seeds = locations.OrderBy(v => random.Next()).ToArray();
                        int[] owners = GrowRaces(xenonOwners, seeds, targets, reserved);
                        if (owners == null || !Validate(owners, seeds, targets, xenonCount, clustered, chaotic, portal))
                            owners = BacktrackRaces(xenonOwners, seeds, targets, reserved, timer, milliseconds);
                        if (owners == null) { LastFailure = "Edit.Territory"; continue; }
                        string error = ValidationError(owners, seeds, targets, xenonCount, clustered, chaotic, portal);
                        if (error != null) { LastFailure = error; continue; }
                        long score = enclaveScore * 1000000000000L + SeedScore(seeds);
                        if (score <= bestScore) continue;
                        bestScore = score;
                        Owners = owners; Seeds = seeds; Portal = portal;
                    }
                }
            }
            return Owners != null;
        }

        public bool Validate(int[] owners, int[] seeds, int[] targets, int xenonCount, bool clustered, bool chaotic, int portal)
        {
            return ValidationError(owners, seeds, targets, xenonCount, clustered, chaotic, portal) == null;
        }

        // Return localization keys so the same structural contract explains edits
        // and generation failures. Targets passed by editors are CURRENT counts.
        public string ValidationError(int[] owners, int[] seeds, int[] targets, int xenonCount, bool clustered, bool chaotic, int portal, bool requirePortalEnclave = true)
        {
            if (graph.Length == 0 || owners.Length != graph.Length || !IsConnected) return "Edit.Disconnected";
            for (int v = 0; v < graph.Length; v++)
                if (graph[v].Any(w => w == v || w < 0 || w >= graph.Length || !graph[w].Contains(v))) return "Edit.Gates";
            if (seeds == null || seeds.Length != RaceCount || seeds.Distinct().Count() != RaceCount
                || targets.Length != RaceCount || seeds.Any(s => s < 0 || s >= graph.Length)
                || owners.Any(o => o < 0 || o > 7)) return "Edit.Seeds";
            // Generation reserves a Xenon enclave, but the exported Maelstrom
            // override is optional and may point to any Xenon sector.
            if (requirePortalEnclave)
            {
                if (portal < 0 || portal >= graph.Length || owners[portal] != 7 || graph[portal].Length != 1) return "Edit.Portal";
            }
            else if (portal != -1 && (portal < 0 || portal >= graph.Length || owners[portal] != 7)) return "Edit.Portal";
            if (owners.Count(o => o == 7) != xenonCount) return "Edit.Budget";
            if (leaves.Count(v => owners[v] == 0) < RequiredUnknownEnclaves(chaotic)) return "Edit.Enclaves";
            for (int r = 0; r < RaceCount; r++)
            {
                if (owners[seeds[r]] != r + 1 || owners.Count(o => o == r + 1) != targets[r]) return "Edit.Seeds";
                if (seeds.Where(s => s != seeds[r]).Any(s => distances[seeds[r]][s] < MinimumSeedDistance)) return "Edit.Spacing";
                if (Component(owners, seeds[r]).Count != targets[r]) return "Edit.Territory";
                if (!Enumerable.Range(0, graph.Length).Any(v => owners[v] == r + 1
                    && graph[v].Any(w => owners[w] == 0))) return "Edit.Exit";
            }
            for (int v = 0; v < graph.Length; v++)
                if (owners[v] >= 1 && owners[v] <= RaceCount
                    && graph[v].Any(w => owners[w] >= 1 && owners[w] <= RaceCount && owners[w] != owners[v])) return "Edit.Border";
            int xenonSeed = Array.IndexOf(owners, 7);
            if (chaotic && (xenonSeed < 0 || Component(owners, xenonSeed).Count != xenonCount)) return "Edit.Xenon";
            if (clustered)
            {
                var remaining = new HashSet<int>(Enumerable.Range(0, graph.Length).Where(v => owners[v] == 7));
                while (remaining.Count > 0)
                {
                    var component = Component(owners, remaining.First());
                    if (!component.Any(v => graph[v].Length == 1)) return "Edit.Xenon";
                    remaining.ExceptWith(component);
                }
            }
            return null;
        }

        private int[] BacktrackRaces(int[] xenon, int[] seeds, int[] targets, HashSet<int> reserved,
            Stopwatch timer, int milliseconds)
        {
            var owners = (int[])xenon.Clone();
            for (int r = 0; r < RaceCount; r++) owners[seeds[r]] = r + 1;
            var counts = Enumerable.Repeat(1, RaceCount).ToArray();
            int visits = 0;
            return GrowBranch(owners, seeds, targets, counts, reserved, timer, milliseconds, ref visits) ? owners : null;
        }

        // Most-constrained race first. Undo a growth choice when it blocks another
        // race's capacity or exit; do not discard a viable seed layout immediately.
        private bool GrowBranch(int[] owners, int[] seeds, int[] targets, int[] counts,
            HashSet<int> reserved, Stopwatch timer, int milliseconds, ref int visits)
        {
            Cancellation.ThrowIfCancellationRequested();
            if (++visits > 2500 || timer.ElapsedMilliseconds >= milliseconds) return false;
            int chosen = -1;
            List<int> options = null;
            for (int r = 0; r < RaceCount; r++)
            {
                var territory = Component(owners, seeds[r]);
                var frontier = territory.SelectMany(v => graph[v]).Where(v => owners[v] == 0).Distinct().ToList();
                if (frontier.Count == 0) return false;
                if (counts[r] == targets[r]) continue;
                int race = r + 1;
                var eligible = frontier.Where(v => !reserved.Contains(v)
                    && !graph[v].Any(w => owners[w] >= 1 && owners[w] <= RaceCount && owners[w] != race)).ToList();
                if (eligible.Count == 0) return false;
                // Count reachable usable cells, excluding other seeds/territories
                // and buffers already forced by them, before exploring this branch.
                var reachable = new HashSet<int>(territory);
                var queue = new Queue<int>(territory);
                while (queue.Count > 0 && reachable.Count < targets[r])
                    foreach (int v in graph[queue.Dequeue()])
                        if (owners[v] == 0 && !reserved.Contains(v)
                            && !graph[v].Any(w => owners[w] >= 1 && owners[w] <= RaceCount && owners[w] != race)
                            && reachable.Add(v)) queue.Enqueue(v);
                if (reachable.Count < targets[r]) return false;
                if (options == null || eligible.Count < options.Count) { chosen = r; options = eligible; }
            }
            if (chosen < 0) return true;
            foreach (int v in options.OrderBy(v => random.Next()).OrderBy(v => distances[seeds[chosen]][v]))
            {
                owners[v] = chosen + 1; counts[chosen]++;
                if (GrowBranch(owners, seeds, targets, counts, reserved, timer, milliseconds, ref visits)) return true;
                counts[chosen]--; owners[v] = 0;
            }
            return false;
        }

        private HashSet<int> Component(int[] owners, int start)
        {
            var seen = new HashSet<int> { start };
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
                foreach (int v in graph[queue.Dequeue()])
                    if (owners[v] == owners[start] && seen.Add(v)) queue.Enqueue(v);
            return seen;
        }
    }
}
