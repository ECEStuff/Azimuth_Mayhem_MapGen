using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 1
    /// <o t="1" x="0" y="0" r="3" size="22500000" m="08011" p="-2" qtrade="0" qfight="0" qbuild="0" qthink="0">
    /// 
    /// </summary>
    public class X3Sector
    {
        // attributes
        public const int t = 1;
        public int x;               // 0-based x,y coordinates in universe. 
        public int y;
        public int r;               // race
        public int size;            // size of system.  Set between 18500000 and 27500000.
        public string m_MusicFileName; // music.  Name of music mp3 file.  i.E. /soundtrack/01039.mp3 would show here as "01039"
        public int p = -2;          // population.  Not used.  Always set to -2;
        public int qtrade = 0;
        public int qfight = 0;
        public int qbuild = 0;
        public int qthink = 0;
        public SectorData SectorCreationData = new SectorData();
        public bool IsMaelstromPortal = false;

        // utility
        public bool IsVoid = false;
        public bool IsInitialized = false;
        private static readonly Random m_Rand = new Random();
        public bool IsOrphaned = false;
        public Color GlobColor = Color.White;
        public int GlobID;
        public string SectorID;     // Assigned when generated.
        public string SectorName;   // ""   system name assigned from master list in sector_names_stream1/2.txt
        public int StreamID;        // ""
        public int StreamLocation;   // ""   spoken system name from sector_names_stream1/2.txt
        public int StreamLength;    // ""
        public Rectangle ScreenBox;

        // content
        private X3Background m_Background = new X3Background();
        public X3Sun Sun;
        private List<X3Planet> m_Planets = new List<X3Planet>();
        public List<X3Gate> Gates = new List<X3Gate>();
        private List<X3Asteroid> m_Asteroids = new List<X3Asteroid>();
        private List<X3Special> m_Specials = new List<X3Special>();

        // Called when generator is creating a new sector.
        public X3Sector(int xx, int yy, string soundfile, int background, List<string> sunAndPlanets, List<string> asteroids)
        {
            x = xx;         // 0-based index for system coordinates
            y = yy;
            r = (int)ERace.None;
            size = m_Rand.Next(18500000, 27500000); // seems to vary between 1.85 Mil and 27.5 mil

            m_MusicFileName = soundfile;
            m_Background.s = background;

            // sunAndPlanets is a short list of 1 sun and 1 or more planets in raw XML.  Parse them out into our members.
            foreach (string sunorPlanet in sunAndPlanets)
            {
                string rawxml = sunorPlanet.Replace(" =", "=").Replace("= ", "=");
                int objtype = int.Parse(X3Utils.GetXMLAttribute(rawxml, "t"));
                if (objtype == 3)       // type 3 is a Sun
                {
                    // test
                    if (Sun != null) throw new Exception("Found 2 suns for a system in the template.");
                    Sun = new X3Sun(rawxml);
                }
                else  // type 4 is Planet
                {
                    m_Planets.Add(new X3Planet(rawxml));
                }
            }

            // asteroids  is a short list of 1 sun and 1 or more planets in raw XML.  Parse them out into our members.
            foreach (string asteroidxml in asteroids)
            {
                X3Asteroid ass = new X3Asteroid(asteroidxml);
                // Note, the asteroids template has asteroids in it of type 2 (nividium).  We only allow types 0 and 1 (ore and silicon).
                if (ass.atype < 2)
                {
                    m_Asteroids.Add(ass);
                }
            }
        }

        // Called when loading up a pre-existing map from a universe files.
        public X3Sector(XmlNode sectornode)
        {
            x = int.Parse(sectornode.Attributes["x"]?.InnerText);
            y = int.Parse(sectornode.Attributes["y"]?.InnerText);
            r = int.Parse(sectornode.Attributes["r"]?.InnerText);
            size = int.Parse(sectornode.Attributes["size"]?.InnerText);
            m_MusicFileName = sectornode.Attributes["m"]?.InnerText;
            p = int.Parse(sectornode.Attributes["p"]?.InnerText);
            qtrade = int.Parse(sectornode.Attributes["qtrade"]?.InnerText);
            qfight = int.Parse(sectornode.Attributes["qfight"]?.InnerText);
            qbuild = int.Parse(sectornode.Attributes["qbuild"]?.InnerText);
            qthink = int.Parse(sectornode.Attributes["qthink"]?.InnerText);

            foreach (XmlNode node in sectornode.ChildNodes)
            {
                // Depending on type, we have entirely different nodes.  egosoft XML designer Fail.  lol.  These should have been different top level nodes, each containing 1 or more subnodes.
                // Now we have to build what basically amounts to custom serializer and deserializer for it.
                if (node.NodeType == XmlNodeType.Comment) continue;

                ENodeType type = (ENodeType)int.Parse(node.Attributes["t"]?.InnerText);
                switch (type)
                {
                    case ENodeType.Background:  // 2
                        m_Background = new X3Background(node);
                        break;
                    case ENodeType.Sun:         // 3
                        Sun = new X3Sun(node);
                        break;
                    case ENodeType.Planet:      // 4
                        m_Planets.Add(new X3Planet(node));
                        break;
                    case ENodeType.Asteroid:    // 17
                        m_Asteroids.Add(new X3Asteroid(node));
                        break;
                    case ENodeType.Gate:        // 18
                        Gates.Add(new X3Gate(node));
                        break;
                    case ENodeType.Special:     // 20
                        m_Specials.Add(new X3Special(node));
                        break;
                }
            }
        }


        /// <summary>
        /// Creates XML in the "contents" list constructed from the galaxy elements.
        /// Note, that Suns and planets come from a template file as XML and we do not attempt to represent them within the Galaxy class.  They are treated as XML lines only.
        /// Note, that Asteroids are created from templates, but we DO represent them within the Galaxy.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="sunsandplanetstemplate"></param>
        /// <param name="asteroidstemplate"></param>
        public void CreateXml(List<string> contents)
        {
            contents.Add($"<o t=\"1\" x=\"{x}\" y=\"{y}\" r=\"{r}\" size=\"{size}\" m=\"{m_MusicFileName}\" p=\" -2\" qtrade=\"0\" qfight=\"0\" qbuild=\"0\" qthink=\"0\" >");

            // Add background   (type 2)
            m_Background.CreateXml(contents);

            // Add sun (type 3)
            Sun.CreateXml(contents);

            // Add Planets (type 4)
            foreach (X3Planet planet in m_Planets)
            {
                planet.CreateXml(contents);
            }

            // Add Asteroids (type 17)
            foreach (X3Asteroid asteroid in m_Asteroids)
            {
                asteroid.CreateXml(contents);
            }

            // Add Gates (type 18)
            foreach (X3Gate gate in Gates)
            {
                gate.CreateXml(contents);
            }

            // Add specials (signs)
            foreach (X3Special special in m_Specials)
            {
                special.CreateXml(contents);
            }

            contents.Add("</o>");
        }

        public int DistanceFrom(X3Sector dest)
        {
            int a = Math.Abs(x - dest.x);
            int b = Math.Abs(y - dest.y);

            return (int)Math.Sqrt(a * a + b * b);
        }

        public void MarkSectorVoidorXenon(bool isXenon)
        {
            if (isXenon)
            {
                r = (int)ERace.Xenon;
            }
            else
            {
                IsVoid = true;
            }
            IsInitialized = true;
        }

        public void MarkSectorRace(ERace race)
        {
            r = (int)race;
            IsInitialized = true;
        }

        /// <summary>
        /// Remove both gates, to and from the destination.
        /// </summary>
        /// <param name="destination"></param>
        public void RemoveGates(X3Sector destination)
        {
            X3Gate destgate = destination.Gates.Find(item => item.gx == x && item.gy == y);
            X3Gate srcgate = Gates.Find(item => item.gx == destination.x && item.gy == destination.y);

            if (destgate != null)
                destination.Gates.Remove(destgate);
            if (srcgate != null)
                Gates.Remove(srcgate);

        }

        public void RemoveGates()
        {
            Gates.Clear();
        }

        public bool IsConnectedTo(X3Sector destination)
        {
            return Gates.Exists(gate => gate.gx == destination.x && gate.gy == destination.y);
        }

        /// <summary>
        /// Creates 2 gates.  1 on each end.  
        /// Always matches outgoing North gate with destination South gate (etc.).
        /// </summary>
        /// <param name="direction"></param>
        public void AddGates(X3Sector destination, bool isSpecial = false)
        {
            CreateGate(this, destination, isSpecial);
            CreateGate(destination, this, isSpecial);
        }

        /// <summary>
        /// Creates destination gate in source sector.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        private void CreateGate(X3Sector source, X3Sector destination, bool isSpecial = false)
        {
            // If destination gate already exists in the source system, just exit.
            if (source.Gates.Exists(gate => gate.gx == destination.x && gate.gy == destination.y)) return;

            EGateDirection direction = EGateDirection.North;
            if (destination.x > source.x) direction = EGateDirection.East;
            else if (destination.y > source.y) direction = EGateDirection.South;
            else if (destination.x < source.x) direction = EGateDirection.West;


            X3Gate destgate = new X3Gate();
            destgate.IsSpecial = isSpecial;
            destgate.s = (int)direction;
            destgate.gid = (int)direction;
            destgate.gx = destination.x;
            destgate.gy = destination.y;
            destgate.b = 0;
            destgate.g = 0;

            // Gates appear in the original roughly at a distance of 50%-125% of the system size
            // Gates are offset (left/right for N/S gates or up/down for E/W gates) by roughly up to 15% of system size based on previous generator.
            // Gates are offset from the ecliptic plane by up to 43% of the system size based on previous generator.
            int gateswingoffset = (int)(0.15 * source.size);
            int gatedistancelow = (int)(0.5 * source.size);
            int gatedistancehigh = (int)(1.25 * source.size);
            int gatedepth = (int)(0.43 * source.size);
            destgate.y = m_Rand.Next(-gatedepth, gatedepth);      // depth above/below plane of system.
            switch (direction)
            {
                case EGateDirection.North:
                    destgate.gtid = (int)EGateDirection.South;
                    destgate.x = m_Rand.Next(-gateswingoffset, gateswingoffset);
                    destgate.z = m_Rand.Next(gatedistancelow, gatedistancehigh);
                    destgate.a = 0;
                    break;
                case EGateDirection.South:
                    destgate.gtid = (int)EGateDirection.North;
                    destgate.x = m_Rand.Next(-gateswingoffset, gateswingoffset);
                    destgate.z = m_Rand.Next(-gatedistancehigh, -gatedistancelow);
                    destgate.a = 32768;
                    break;
                case EGateDirection.East:
                    destgate.gtid = (int)EGateDirection.West;
                    destgate.z = m_Rand.Next(-gateswingoffset, gateswingoffset);
                    destgate.x = m_Rand.Next(gatedistancelow, gatedistancehigh);
                    destgate.a = 49152;
                    break;
                case EGateDirection.West:
                    destgate.gtid = (int)EGateDirection.East;
                    destgate.z = m_Rand.Next(-gateswingoffset, gateswingoffset);
                    destgate.x = m_Rand.Next(-gatedistancehigh, -gatedistancelow);
                    destgate.a = 16384;
                    break;
            }
            source.Gates.Add(destgate);
        }

    }

}
