
// Analyze valid, reciprocal gate links without modifying sectors or gate data.
// This also catches broken links in loaded maps.
namespace X3_Mayhem_Galaxy_Generator
{
    public sealed class GateNetworkReport
    {
        public int GatelessSectors { get; internal set; }
        public int ConnectedNetworks { get; internal set; }
        public int OutsideLargestNetwork { get; internal set; }
        public int MissingDestinations { get; internal set; }
        public int MissingReturnGates { get; internal set; }
    }
}
