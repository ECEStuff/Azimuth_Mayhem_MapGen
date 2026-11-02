using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 3
    /// <o t="3" s="208" x="171233183" y="420674633" z="-167238936" color="16777160"/>
    /// </summary>
    public class X3Sun
    {
        private static readonly Random m_Rand = new Random();
        public const int t = 3;
        public int s;     // type of sun.index into type\TSuns.pck	0-218 index.See 11.cat.
        public int x;
        public int y;
        public int z;
        public int color;    // rgb color index. see appendix 3 in  http://www.xenotaph.net/lib_pdf/galaxy_hacking_x3tc.pdf

        public X3Sun(string rawxml)
        {
            rawxml = rawxml.Replace(" =", "=").Replace("= ", "=");
            // Here's a strange thing.  Sun ID's embedded in 11.cat\TSuns.pck look like this "SS_SUN_180" through "SS_SUN_189" for a given sun type.
            // What's interesting is those that end in a 4 are 50% sun level in game. Those that end in 5 are 100% in game. etc.. up through those that end with 8 are 300% sun level in game.
            // So we have to do the same here with our Sun ID because the template he provides has all the ID's and we don't want to be seeing sun levels lower than 100% in mayhem.
            // He used ID's 13X through 19X, where X is between 4 and 8 inclusive.
            s = int.Parse(X3Utils.GetXMLAttribute(rawxml, "s"));
            x = int.Parse(X3Utils.GetXMLAttribute(rawxml, "x"));
            y = int.Parse(X3Utils.GetXMLAttribute(rawxml, "y"));
            z = int.Parse(X3Utils.GetXMLAttribute(rawxml, "z"));
            color = int.Parse(X3Utils.GetXMLAttribute(rawxml, "color"));

            // Sun ID's only exist in these ranges in TSuns.
            if (s < 134)
            {
                s = 134;
            }
                
            if (s > 218)
            { 
                s = 218;
            }

            // Test..  suns should be ID XX4 through XX8 which would be 100% to 300% sun levels.
            int value = X3Utils.PlaceDigitValue(0, s);      // Grab last digit of the sun ID.
            if (value < 4 || value > 8)                    // If out of range of 50-300% sun level, put it back in range.
            {
                s = s - value + m_Rand.Next(4, 9);
            }
        }

        public void SetSunLevel(int val)
        {
            StringBuilder sb = new StringBuilder(s.ToString());
            sb[2] = val.ToString()[0];
            s = int.Parse(sb.ToString());
        }

        public X3Sun(XmlNode node)
        {
            s = int.Parse(node.Attributes["s"]?.InnerText);
            x = int.Parse(node.Attributes["x"]?.InnerText);
            y = int.Parse(node.Attributes["y"]?.InnerText);
            z = int.Parse(node.Attributes["z"]?.InnerText);
            color = int.Parse(node.Attributes["color"]?.InnerText);
        }

        public void CreateXml(List<string> contents)
        {
            contents.Add($"\t<o t=\"3\" s=\"{s}\" x=\"{x}\" y=\"{y}\" z=\"{z}\" color=\"{color}\"/>");
        }
    }
}
