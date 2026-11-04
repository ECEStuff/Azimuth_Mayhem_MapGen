// Serializes special sector objects such as signs, retaining their subtype, position and orientation.

using System.Collections.Generic;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 20
    /// <o t="20" s="SS_SPECIAL_SIGN3" x="867612" y="-1200845" z="-4453430" a="0" b="0" g="0" v="-1"/>
    /// </summary>
    public class X3Special
    {
        public const int t = 20;
        public string s;           // sub-type indexed in TSpecials.pck(SS_SPECIAL_SIGN1 through SS_SPECIAL_SIGN4) Odd donut shaped signs.
        public int x;
        public int y;
        public int z;           // position
        public int a;
        public int b;
        public int g;           // rotational factors.
        public int v;           // video stream to be played on advertising buoys.	may want to play with this.

        public X3Special(XmlNode node)
        {
            s = node.Attributes["s"]?.InnerText;
            x = int.Parse(node.Attributes["x"]?.InnerText);
            y = int.Parse(node.Attributes["y"]?.InnerText);
            z = int.Parse(node.Attributes["z"]?.InnerText);

            a = int.Parse(node.Attributes["a"]?.InnerText);
            b = int.Parse(node.Attributes["b"]?.InnerText);
            g = int.Parse(node.Attributes["g"]?.InnerText);

            v = -1;
        }

        public void CreateXml(List<string> contents)
        {
            contents.Add($"\t<o t=\"20\" s=\"{s}\" x=\"{x}\" y=\"{y}\" z=\"{z}\" a=\"{a}\" b=\"{b}\" g=\"{g}\" v=\"{v}\"/>");
        }
    }
}
