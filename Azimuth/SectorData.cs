// Per-sector Mayhem attributes. The 9970 prefix selects the attribute and the XXYY suffix identifies the sector.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azimuth
{
    /// <summary>
    /// Unique Mayhem 3 data for each sector found in last section of addon\t\9970-L044.xml.
    /// <t id="1031802">Black Hole Sun \(x:1, y:17\)\n\nDejure owner: \033Anone\nCompany: \033Anone\nAbnormal signals: \033G0 %\nTerran memory: \033Ano\n\nMax. population: 76\nStation support: 10\n
    /// Research rate: \033W77 %\n\nOutpost production: Fish\nOutpost consumption: Fruit\n</t>
    /// </summary>
    public class SectorData
    {
        public int DejureOwner;
        public int Smugglers;
        public int Xenons;
        public int Terrans;
        public int Research;
        public int Support;
        public int Manpower;
        public int Cluster = -1;
        public int Production;
        public int Consumption;
        public int Company = (int)ECompany.None;

        
        // Section 2 of the 9970 has what's DISPLAYED in the game for a sector.
        // Stored in <page id="19" title="Sector description" descr="Long descriptions of all sectors" voice="no">
        // Dejure owner:        \033Anone\n     (displayed as "none", the rest is formatting.)
        // Company              \033Anone\n         ""
        // Abnormal signals     \033G0 %\n      (displayed as "0 %")
        // Terran memory:       \033Ano\n\n     (displayed as "no")
        // Max.population:      76\n
        // Station support:     10\n
        // Research rate:       \033W77 %\n\n   (displayed as "77 %")
        // Outpost production:  Fish\n
        // Outpost consumption: Fruit\n

        // Section 3 of the 9970 has what's ACTUALLY USED in the game.
        // Thse Values stored in bottom section of 9970-L044 under   <page id="9970" title="Mayhem Galaxy Generator">
        // For Each Sector:  (sector location is 1-based)
        //<t id="10000101">5</t>            // Dejure Owner:        id = 1000 + SectorLocation      25% chance for any system in galaxy
        //<t id="10010101">0</t>            // smugglers:           id = 1001 + SectorLocation      Not used
        //<t id="10020101">48</t>           // Xenons:              id = 1002 + SectorLocation      Abnormal signals.  0-60, average 22.
        //<t id="10030101">0</t>            // Terrans:             id = 1003 + "                   Terran Memory.
        //<t id="10040101">99</t>           // research:            id = 1004 + "                   40-100, average 70
        //<t id="10050101">9</t>            // support              id = 1005 + "                   Number of stations supported in system.  Normal rolls average 10; Saboteur rolls average 8.
        //<t id="10060101">107</t>          // manpower             id = 1006 + "                   Population of system (60-130, average 95)
        //<t id="10070101">-1</t>           // cluster              id = 1007 + "                   Not Used.  Set to -1 to duplicate old program.
        //<t id="10080101">3 </t >          // production           id = 1008 + "                   Type of Food produced.  See EFood above for enumeration.
        //<t id="10090101">1</t>            // consumption          id = 1009 + "                   Type of food consumed.  See EFood.
        //<t id="10100101">4</t>            // company              id = 1010 + "                   Which company has a station in that sector.     See ECompany above for enumeration.  20% of all systems have companies.
    }
}
