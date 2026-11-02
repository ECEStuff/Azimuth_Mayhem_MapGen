using System.Collections.Generic;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 17
    /// <o t="17" s="7" x="-7480000" y="4027500" z="-15130500" atype="1" aamount="17" a="42338" b="10784" g="27456"/>
    /// </summary>
    public class X3Asteroid
    {
        public const int t = 17;
        public int s;           // sub-type, which is index into types\TAsteroids.pc     0-9.  See 07.cat.
        public int x;
        public int y;
        public int z;
        public int atype;       // atype - 0-Ore, 1=Silicon, 2=Nividium, 3=Ice
        public int aamount;     // aamount = yield quantity
        public int a;
        public int b;
        public int g;           // rotational factors.

        public X3Asteroid(XmlNode node)
        {
            ParseXml(node);
        }

        public X3Asteroid(string rawxml)
        {
            rawxml = rawxml.Replace(" =", "=").Replace("= ", "=");
            s = int.Parse(X3Utils.GetXMLAttribute(rawxml, "s"));
            x = int.Parse(X3Utils.GetXMLAttribute(rawxml, "x"));
            y = int.Parse(X3Utils.GetXMLAttribute(rawxml, "y"));
            z = int.Parse(X3Utils.GetXMLAttribute(rawxml, "z"));
            atype = int.Parse(X3Utils.GetXMLAttribute(rawxml, "atype"));
            aamount = int.Parse(X3Utils.GetXMLAttribute(rawxml, "aamount"));
            a = int.Parse(X3Utils.GetXMLAttribute(rawxml, "a"));
            b = int.Parse(X3Utils.GetXMLAttribute(rawxml, "b"));
            g = int.Parse(X3Utils.GetXMLAttribute(rawxml, "g"));
        }


        public void ParseXml(XmlNode node)
        {
            s = int.Parse(node.Attributes["s"]?.InnerText);
            x = int.Parse(node.Attributes["x"]?.InnerText);
            y = int.Parse(node.Attributes["y"]?.InnerText);
            z = int.Parse(node.Attributes["z"]?.InnerText);
            atype = int.Parse(node.Attributes["atype"]?.InnerText);
            aamount = int.Parse(node.Attributes["aamount"]?.InnerText);
            a = int.Parse(node.Attributes["a"]?.InnerText);
            b = int.Parse(node.Attributes["b"]?.InnerText);
            g = int.Parse(node.Attributes["g"]?.InnerText);
        }

        public void CreateXml(List<string> contents)
        {
            contents.Add($"\t<o t=\"17\" s=\"{s}\" x=\"{x}\" y=\"{y}\" z=\"{z}\" atype=\"{atype}\" aamount=\"{aamount}\" a=\"{a}\" b=\"{b}\" g=\"{g}\"/>");
        }
    }
}
