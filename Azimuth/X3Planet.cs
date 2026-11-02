using System.Collections.Generic;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 4
    /// <o t="4" s="228" x="257613751" y="-31905822" z="-256982517"/>
    /// </summary>
    public class X3Planet
    {
        public const int t = 4;
        public int s;
        public int x;
        public int y;
        public int z;

        public X3Planet(string rawxml)
        {
            rawxml = rawxml.Replace(" =", "=").Replace("= ", "=");
            s = int.Parse(X3Utils.GetXMLAttribute(rawxml, "s"));
            x = int.Parse(X3Utils.GetXMLAttribute(rawxml, "x"));
            y = int.Parse(X3Utils.GetXMLAttribute(rawxml, "y"));
            z = int.Parse(X3Utils.GetXMLAttribute(rawxml, "z"));
        }

        public X3Planet(XmlNode node)
        {
            s = int.Parse(node.Attributes["s"]?.InnerText);
            x = int.Parse(node.Attributes["x"]?.InnerText);
            y = int.Parse(node.Attributes["y"]?.InnerText);
            z = int.Parse(node.Attributes["z"]?.InnerText);
        }

        public void CreateXml(List<string> contents)
        {
            contents.Add($"\t<o t=\"4\" s=\"{s}\" x=\"{x}\" y=\"{y}\" z=\"{z}\"/>");
        }
    }
}
