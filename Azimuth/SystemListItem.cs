// Pairs a candidate display name with its speech stream, start offset and clip length. Names may share audio.

namespace Azimuth
{
    public class SystemListItem
    {
        public string Name;             // System Name
        public int StreamID;            // stream 1 or stream 2
        public int StreamLocation;
        public int StreamLength;
    }

}
