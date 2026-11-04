// Stores one gate endpoint and its destination. The destination coordinates also identify incoming gates during deletion.

using System.Collections.Generic;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 18
    /// <o t="18" s="3" x="22426343" y="7498788" z="-518692" gid="3" gx="1" gy="0" gtid="2" a="49152" b="0" g="0"/>
    /// Note about Location:
    ///     x = left to right, y = depth (what I would consider z-axis), z = top to bottom.
    ///     +z = up.  -z = down.
    ///     +x = right
    ///     x or z vary from ? to Sector size.  So for a system that is 25,000,000 size, the west gate may have x=-25,000,000 and an east gate having x=25,000,000
    ///     other gate position values seem to range between 1 and 8 mil, putting them slightly off axis.. so a west gate may sit slightly above or below the horizontal axis.
    ///     
    ///     ON-AXIS Alignment of gates is within 20% of system size (i.e. for 22Mil system size, gate variance was -3.9M to +3.9M)
    ///     OFF-AXIS Alighment of gate varied from 55% to 125% of system size.
    /// 
    /// </summary>
    public class X3Gate
    {
        public const int t = 18;
        public int s;           // SubType:  0 = North, 1 = South, 2 = West, 3 = East
        public int x;           // physical coordinates.   left-right 
        public int y;           // depth
        public int z;           // up-down
        public int gid;         // gid - always same as SubType.  denotes gate direction.
        public int gx;          // represents the DESTINATION system coordinates for the gate, 0 based
        public int gy;          // ""
        public int gtid;        // represents the DESTINATION gate direction.  So for an SubType=1 (south gate), the destination gate would be 0 (north)

        public bool IsRemoved;      // temporary to see stuff..
        public bool IsSpecial;

        // Gate rotations.
        // North Gate - a="0"     b="0" g="0"
        // South Gate - a="32768" b="0" g="0"
        // West Gate -  a="16384" b="0" g="0"
        // East Gate -  a="49152" b="0" g="0"
        public int a;
        public int b;
        public int g;

        public X3Gate()
        {

        }

        public X3Gate(XmlNode node)
        {
            s = int.Parse(node.Attributes["s"]?.InnerText);
            x = int.Parse(node.Attributes["x"]?.InnerText);
            y = int.Parse(node.Attributes["y"]?.InnerText);
            z = int.Parse(node.Attributes["z"]?.InnerText);
            gid = int.Parse(node.Attributes["gid"]?.InnerText);
            gx = int.Parse(node.Attributes["gx"]?.InnerText);
            gy = int.Parse(node.Attributes["gy"]?.InnerText);
            gtid = int.Parse(node.Attributes["gtid"]?.InnerText);
            a = int.Parse(node.Attributes["a"]?.InnerText);
            b = int.Parse(node.Attributes["b"]?.InnerText);
            g = int.Parse(node.Attributes["g"]?.InnerText);
        }

        /// <summary>
        /// Serialize the gate and save the line to contents.
        /// </summary>
        /// <param name="contents"></param>
        public void CreateXml(List<string> contents)
        {
            contents.Add($"\t<o t=\"18\" s=\"{s}\" x=\"{x}\" y=\"{y}\" z=\"{z}\" gid=\"{gid}\" gx=\"{gx}\" gy=\"{gy}\" gtid=\"{gtid}\" a=\"{a}\" b=\"{b}\" g=\"{g}\" />");
        }
    }
}
