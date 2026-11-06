using System;
using System.Collections.Generic;
using System.Linq;

namespace Azimuth
{
    // A read-only topology snapshot. Proposals change arrays, never live sectors.
    internal sealed class GalaxyEditRules
    {
        internal static readonly int[] RaceIds = { 1, 2, 3, 4, 5, 18 };
        private readonly X3Galaxy galaxy;
        internal readonly List<X3Sector> Sectors;
        internal readonly int[][] Graph;
        internal readonly int[] Owners;
        private readonly int[] seeds;
        private readonly int portal;
        private readonly string captureError;

        internal GalaxyEditRules(X3Galaxy galaxy)
        {
            this.galaxy = galaxy;
            Sectors = galaxy.Sectors.ToList();
            Owners = Sectors.Select(s => Encode(s.r)).ToArray();
            Graph = Sectors.Select(s => s.Gates.Select(g => Sectors.FindIndex(t => t.x == g.gx && t.y == g.gy)).ToArray()).ToArray();
            if (Graph.Any(v => v.Any(w => w < 0)) || Sectors.Any(s => s.Gates.GroupBy(g => g.s).Any(g => g.Count() > 1)))
                captureError = "Edit.Gates";
            var coordinates = galaxy.GalaxyCreationSettings.TerritorySeeds;
            seeds = coordinates != null && coordinates.GetLength(0) == 6 && coordinates.GetLength(1) == 2
                ? Enumerable.Range(0, 6).Select(r => Sectors.FindIndex(s => s.x == coordinates[r, 0] && s.y == coordinates[r, 1])).ToArray()
                : null;
            portal = Sectors.FindIndex(s => s.IsMaelstromPortal);
            if (Sectors.Count(s => s.IsMaelstromPortal) > 1) captureError = "Edit.Portal";
        }
        internal static int Encode(int owner)
        {
            if (owner == 14) return 0;
            if (owner == 6) return 7;
            int index = Array.IndexOf(RaceIds, owner);
            return index < 0 ? -1 : index + 1;
        }
        internal bool IsProtected(X3Sector sector) { int i = Sectors.IndexOf(sector); return seeds != null && seeds.Contains(i); }
        internal bool IsSeed(X3Sector sector) { return seeds != null && seeds.Contains(Sectors.IndexOf(sector)); }
        // Applies also to imported maps without territory-seed metadata.
        internal string AttributeError()
        {
            if (Sectors.Count(s => s.IsMaelstromPortal) > 1 || Sectors.Any(s => s.IsMaelstromPortal && s.r != 6)) return "Edit.Portal";
            string share = XenonError(Owners.Length, Owners.Count(o => o == 7));
            if (share != null) return share;
            foreach (var sector in Sectors)
            {
                if (!SectorOwnershipRules.ValidDejure(sector.r, sector.SectorCreationData.DejureOwner)) return "Edit.DejureOwner";
                if (!SectorOwnershipRules.ValidCompany(sector.r, sector.SectorCreationData.Company)) return "Edit.CompanyOwner";
            }
            return null;
        }
        private string XenonError(int total, int xenon)
        {
            var settings = galaxy.GalaxyCreationSettings;
            int percent = GalaxySettings.ResolveXenonPercent(settings.XenonPercent, GalaxySettings.MinXenonPercent);
            return SectorOwnershipRules.ValidXenonCount(total, xenon, settings.PeacefulStart, settings.ChaoticStart, percent)
                ? null : settings.PeacefulStart ? "Edit.PeacefulXenon" : "Edit.MinimumXenon";
        }
        internal string CurrentError() { return AttributeError() ?? Validate(Graph, Owners, seeds, portal); }
        internal string GraphError(int[][] graph)
        {
            if (graph.Length != Sectors.Count || graph.Any(v => v.Any(w => w < 0 || w >= graph.Length))) return "Edit.Gates";
            for (int i = 0; i < graph.Length; i++)
                if (graph[i].GroupBy(j => Direction(Sectors[i].x, Sectors[i].y, Sectors[j].x, Sectors[j].y)).Any(g => g.Count() > 1))
                    return "Edit.Gates";
            return Validate(graph, Owners, seeds, portal);
        }
        internal string OwnerError(X3Sector sector, int owner)
        {
            if (owner == sector.r) return null;
            if (IsProtected(sector)) return "Edit.Protected";
            if (owner != 14 && owner != 6 && !RaceIds.Contains(owner)) return "Edit.Owner";
            var proposed = (int[])Owners.Clone();
            int i = Sectors.IndexOf(sector);
            if (i < 0) return "Edit.Seeds";
            proposed[i] = Encode(owner);
            // Connectedness of the recipient alone could allow its first remote
            // sector. Require an existing neighbor for each main-race transfer.
            if (RaceIds.Contains(owner) && !Graph[i].Any(j => j >= 0 && Owners[j] == proposed[i])) return "Edit.Territory";
            return Validate(Graph, proposed, seeds, i == portal && owner != 6 ? -1 : portal);
        }
        private string Validate(int[][] graph, int[] owners, int[] starts, int portalIndex)
        {
            string xenonError = XenonError(owners.Length, owners.Count(o => o == 7));
            if (xenonError != null) return xenonError;
            if (captureError != null) return captureError;
            if (graph.Any(v => v.Any(w => w < 0 || w >= graph.Length))) return "Edit.Gates";
            if (starts == null) return "Edit.Legacy";
            int[] counts = Enumerable.Range(1, 6).Select(r => owners.Count(o => o == r)).ToArray();
            var settings = galaxy.GalaxyCreationSettings;
            return new TerritoryPlanner(graph, new Random(0)).ValidationError(owners, starts, counts,
                owners.Count(o => o == 7), settings.ChaoticStart || settings.ClusteredXenons, settings.ChaoticStart, portalIndex, false);
        }
        internal string RemovalError(X3Sector sector)
        {
            if (IsProtected(sector)) return "Edit.Protected";
            if (!CountAllowed(Sectors.Count - 1)) return "Edit.Count";
            int removed = Sectors.IndexOf(sector);
            Func<int, int> remap = i => i < removed ? i : i - 1;
            var graph = Graph.Where((v, i) => i != removed)
                .Select(v => v.Where(w => w != removed).Select(remap).ToArray()).ToArray();
            return Validate(graph, Owners.Where((v, i) => i != removed).ToArray(),
                seeds == null ? null : seeds.Select(remap).ToArray(), portal == removed ? -1 : remap(portal));
        }
        internal bool CountAllowed(int count)
        {
            int cells = galaxy.MaxX * galaxy.MaxY;
            // Match the generator's integer void rounding (for example a 17x17
            // large grid permits 246 sectors). Do not introduce an off-by-one cap.
            var settings = galaxy.GalaxyCreationSettings;
            return count >= settings.SectorTarget(cells, 50)
                && count <= settings.SectorTarget(cells, 15);
        }
        // Return eligible owner -> safe neighbor subset. A new sector never needs
        // to be left disconnected while waiting for the user to add gates later.
        internal Dictionary<int, int[]> AdditionChoices(int x, int y)
        {
            var choices = new Dictionary<int, int[]>();
            if (!CountAllowed(Sectors.Count + 1) || seeds == null || captureError != null) return choices;
            var neighbors = Enumerable.Range(0, Sectors.Count).Where(i => Math.Abs(Sectors[i].x - x) + Math.Abs(Sectors[i].y - y) == 1)
                .Where(i => !Sectors[i].Gates.Any(g => g.s == (int)Direction(Sectors[i].x, Sectors[i].y, x, y))).ToArray();
            foreach (int owner in new[] { 14, 1, 2, 3, 4, 5, 18, 6 })
                foreach (int mask in Enumerable.Range(1, (1 << neighbors.Length) - 1)
                    .OrderByDescending(m => neighbors.Where((v, i) => (m & (1 << i)) != 0).Count()))
                {
                    var selected = neighbors.Where((v, i) => (mask & (1 << i)) != 0).ToArray();
                    int encoded = Encode(owner), added = Sectors.Count;
                    if (RaceIds.Contains(owner) && !selected.Any(i => Owners[i] == encoded)) continue;
                    var graph = Graph.Select((v, i) => selected.Contains(i) ? v.Concat(new[] { added }).ToArray() : v.ToArray())
                        .Concat(new[] { selected }).ToArray();
                    if (Validate(graph, Owners.Concat(new[] { encoded }).ToArray(), seeds, portal) != null) continue;
                    choices.Add(owner, selected); break;
                }
            return choices;
        }
        internal static EGateDirection Direction(int x, int y, int destX, int destY)
        { return destX > x ? EGateDirection.East : destX < x ? EGateDirection.West : destY > y ? EGateDirection.South : EGateDirection.North; }
    }
}
