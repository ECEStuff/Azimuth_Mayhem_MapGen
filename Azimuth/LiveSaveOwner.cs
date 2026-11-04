// Identifies the owner of shared X3 save slots by installation root and galaxy name together.

namespace Azimuth
{
    internal sealed class LiveSaveOwner
    {
        public string InstallRoot { get; set; }
        public string GalaxyName { get; set; }
    }
}
