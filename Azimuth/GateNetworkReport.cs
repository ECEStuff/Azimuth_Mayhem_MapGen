// Collects connectivity findings used by the map validity display and the checks before saving or activating a galaxy.


// Analyze valid, reciprocal gate links without modifying sectors or gate data.
// This also catches broken links in loaded maps.
namespace Azimuth
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
