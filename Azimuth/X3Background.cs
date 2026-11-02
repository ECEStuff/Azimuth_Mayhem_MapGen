using System.Collections.Generic;
using System.Xml;

namespace Azimuth
{
    /// <summary>
    /// Type = 2
    /// <o t="2" s="15"/>
    /// 
    /// \mayhem_data\custom_backgrounds.txt contains indicies 0-81
    /// \mayhem_data\template_TBackgrounds.txt contains actual data for 81 backgrounds.
    /// 
    /// </summary>
    public class X3Background
    {
        public int s;     // represents sub-type which is the 0-index into types/TBackgrounds.pck

        public X3Background()
        {

        }

        public X3Background(XmlNode node)
        {
            s = int.Parse(node.Attributes["s"]?.InnerText);
        }

        public void CreateXml(List<string> contents)
        {
            contents.Add($"\t<o t=\"2\" s=\"{s}\"/>");
        }
    }
}
