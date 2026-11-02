using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X3_Mayhem_Galaxy_Generator
{
    public class Tips
    {
        public const string BtnGenerateSystems = "Takes all the settings below and Generates a new Map, new Gates, and fills in each system with a new set of Attributes";
        public const string BtnGenerateGates = "Allows you to re-generate the gate network as many times as you like.";
        public const string BtnGenerateStats = "Allows you to re-generate the system attributes as many times as you like.";
        public const string BtnX3Root = "Click to Set the root folder of your Mayhem 3 installation.";
        public const string BtnX3Save = "Click to set the location where your X3 saved game files are stored.";
        public const string CbClusteredVoids = "Attempts to Group the empty (void) areas together rather than sprinkle empty systems randomly.";
        public const string CbClusteredXenon = "Attempts to Group Xenon sectors together rather than sprinkle empty systems randomly.";
        public const string CbShattered = "Changes empty (void) map percentage from 15% to 35% resulting in far less systems for the same map dimensions.";
        public const string CbLimitedEnclaves = "When NOT checked, the gate generator will attempt to remove 'some' gates where doing so will result in a system having only 1 gate in or out.";
        public const string CbExtraSystemStats = "Adds extra Population, Research, and Support to every system.";
        public const string CbParticles = "Turns on the particle system";
        public const string SlFog = "Sets average fog level across the galaxy.";
        public const string GbExpansion = "Determines ratio of unknown sectors to populated sectors for a map. Higher expansion results in far less unknown sectors at game start.";
        public const string GbSystemSpread = "Provides a race specific offset (weight) that will lower or raise the starting number of systems of that race in comparison to other races.";
        public const string GbGalaxySize = "Determines the approximate dimensions of a system.";
        public const string GbGateDensity = "Determines the level of gates created near a race's borders.  Low density results in less gates connecting differing races.";
        public const string GbMapType = "Square or Rectangle map.";
        public const string GbMusic = "Determines list used for System music\nBasic = \\mayhem_data\\custom_musics.txt\nLitcube = \\mayhem_data\\custom_musics_lu.txt\nAll = Every music file found in your \\soundtrack folder";
        public const string GbRelations = "Allows you to set permanent friend/foe status between races at game start";
    }
}
