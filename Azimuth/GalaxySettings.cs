// Stores the generator sidecar (GalaxySettings.json). Only the documented subset is exported to page 9970.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azimuth
{
    /// <summary>
    /// Encapsulates some data we write to 9970-L044.xml in the "<page id="9970" title="Mayhem Galaxy Generator">" section near the bottom.
    /// These are settings made by the user in the Generator that are acted on in the game.
    /// 
    /// See mayhem script --------Mayhem.GalaxyCreator.GalaxyData.xml============ to see how this is loaded.
    /// 
    /// </summary>
    public class GalaxySettings
    {
        // 9970 File, Bottom Section:
        // -----------------------------------------------------------------------
        // ID 1     mayhem_galaxy.age                   Used in Mayhem.GameCreator.Populate.xml and Mayhem.IsGalaxyExpansionNone.xml. (-1 = none, 0 = early, 1 = average, 2 = advanced)  
        // ID 2     mayhem_galaxy.dominance             Not Used - legacy setting
        // ID 3     mayhem_galaxy.minority              Not Used - legacy setting
        // ID 4     mayhem_galaxy.name                  Name of Galaxy.  I.E. whatever the generator made.
        // ID 5-40  Relations (36 entries)
        //  0 = No Change (dynamic)
        //  1 = Permanent Alliance
        // -1 = Permanent War
        // 5-10  Argons Relation to:
        //      Argons
        //      Borons
        //      Split
        //      Paranids
        //      Teladi
        //      Terran
        // 11-16  Boron Relations to same list.
        // 17-22  Split Relations to same list.
        // 23-28  Paranid Relations to same list.
        // 29-34  Teladi Relations to same list.
        // 35-40  Terran
        // ID 100   mayhem_galaxy.scattered_galaxy      Not Used
        // ID 101   Sector stats modifier: -1 = Saboteur, 0 = Normal, 1 = Extra sector stats.
        // ID 102   mayhem_galaxy.clustered_xenons      Not Used
        // ID 110   mayhem_galaxy.hollowed_galaxy       Not Used - legacy setting
        // ID 111   mayhem_galaxy.random_distribution   Not Used - legacy setting
        // ID 113   mayhem_galaxy.no_enclaves           Not Used
        // ID 120   Optional Maelstrom sector, zero-based X,Y.
        // ID 121   mayhem_galaxy.game_start            -1 = Standard Start, 0 = Peaceful Start, 1 = Chaotic Start

        // All remaining ID's are System Stats for every system.  I.E. max population etc.


        // Settings exported to 9970; game-side use depends on the Mayhem edition.
        // --------------------------------------------
        public int Dominance;                       // ID 2 Not used in editor. This editor uses Spread variables.          
        public int Minority;                        // ID 3 Not used in editor.
        public bool ShatteredGalaxy;                // ID 100 Used by legacy maps and other editors.
        public bool ExtraSystemStats;               // ID 101, True when ID 101 is 1; mutually exclusive with SaboteurMode.
        // Generated race seeds (zero-based X,Y); distinct from player-selected start overrides.
        public int[,] TerritorySeeds;

        public bool SaboteurMode;                   // ID 101, True when ID 101 is -1; mutually exclusive with ExtraSystemStats.
        public bool ClusteredXenons;                // ID 102
        public bool HollowedGalaxy;                 // ID 110 Not used in editor.
        public bool RandomDistribution;             // ID 111 Not used in editor.
        public bool LimitedEnclaves;                // ID 113

        // These are referenced in the 9970 file and used in Mayhem 3 script.
        // --------------------------------------------
        public EGalaxyExpansion GalaxyExpansion;    // ID 1
        public string GalaxyName;                   // ID 4
        public int[,] Relations = new int[6, 6]     // ID's 5-40
        { 
            {0, 0, 0, 0, 0, 0},                     // Argon Relations.   See ERelationship above for values.
            {0, 0, 0, 0, 0, 0},                     // Boron
            {0, 0, 0, 0, 0, 0},                     // Split
            {0, 0, 0, 0, 0, 0},                     // Paranid
            {0, 0, 0, 0, 0, 0},                     // Teladi
            {0, 0, 0, 0, 0, 0}                      // Terran
        };

        // Azimuth 1.8.5: Fix Xenon Defect not having manual start location!
        public int[,] StartSectors = new int[7, 2]
        {
            {-1, -1},                               // Argon Starting Coordinates. -1, -1 for random
            {-1, -1},                               // Boron
            {-1, -1},                               // Split
            {-1, -1},                               // Paranid
            {-1, -1},                               // Teladi
            {-1, -1},                               // Terran
            {-1, -1}                                // Xenon
        };

        public bool PeacefulStart;                  // ID 121 = 0
        public bool ChaoticStart;                   // ID 121 = 1

        // These are used in this Editor only and not saved in the 9970.
        public bool UseParticles;
        public EGateDensity BorderGateDensity;
        public int MusicSelection;
        public bool ClusteredVoids;
        public int MapType;
        public int ArgonSpread;
        public int BoronSpread;
        public int SplitSpread;
        public int ParanidSpread;
        public int TeladiSpread;
        public int TerranSpread;
        public int FogLevelPercent;
        public const int MinXenonPercent = 3;
        public const int MaxXenonPercent = 10;
        public int? XenonPercent;

        internal static int ResolveXenonPercent(int? percent, int fallback)
        {
            return percent >= MinXenonPercent && percent <= MaxXenonPercent ? percent.Value : fallback;
        }

        public int? VoidPercent;
        internal int EffectiveVoidPercent => VoidPercent.HasValue && VoidPercent >= 15 && VoidPercent <= 50
            ? VoidPercent.Value : (ShatteredGalaxy ? 35 : 15);

        internal int MinimumCount => GalaxySize == 0 ? 50 : GalaxySize == 1 ? 86 : 145;
        internal int MaximumCount => GalaxySize == 0 ? 85 : GalaxySize == 1 ? 144 : 246;

        internal int SectorTarget(int cells, int percent)
        {
            int raw = cells - cells * percent / 100;
            return Math.Min(cells, Math.Max(MinimumCount, Math.Min(MaximumCount, raw)));
        }

        internal int SquareSide => GalaxySize == 0 ? 10 : GalaxySize == 1 ? 13 : 17;

        public System.Drawing.Size PresetGridSize(bool legacy = false)
        {
            if (MapType == 0)
            {
                return new System.Drawing.Size(SquareSide, SquareSide);
            }
            if (GalaxySize == 0) return new System.Drawing.Size(legacy ? 13 : 14, 7);
            if (GalaxySize == 1) return legacy ? new System.Drawing.Size(18, 9) : new System.Drawing.Size(17, 10);
            return legacy ? new System.Drawing.Size(22, 13) : new System.Drawing.Size(24, 12);
        }

        public int GridWidth;
        public int GridHeight;
        public int GalaxySize;      // 0, 1, 2  (small, medium, large)
    }
}
