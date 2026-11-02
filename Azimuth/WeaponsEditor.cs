using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Azimuth
{
    public partial class WeaponsEditor : Form
    {
        private List<string> m_Bullets = new List<string>();
        private List<string> m_Ships = new List<string>();
        private static readonly Random m_Rand = new Random();

        private enum EAdjust {ERandom, ERaise, ELower};
        public string MayhemFolder;

        private Dictionary<string, int> racedd = new Dictionary<string, int>
        {
            {"Argon", 1 },
            {"Boron", 2 },
            {"Split", 3 },
            {"Paranid", 4 },
            {"Teladi", 5 },
            {"Terran", 18 }
        };

        public WeaponsEditor()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void WeaponsEditor_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(MayhemFolder))
            {
                try
                {
                    Directory.CreateDirectory(MayhemFolder);
                }
                catch(Exception)
                {
                    MessageBox.Show("Unable to create " + MayhemFolder + ".  This is likely a permissions issue.  You will need to create this folder.");
                    Close();
                    return;
                }
            }

            //cbRace.SelectedItem = "Argon";
            lblNote.Text = "";

            cbRace.DisplayMember = "Key";
            cbRace.ValueMember = "Value";
            cbRace.DataSource = racedd.Where(item => item.Value != 0).ToList();

        }

        private void rbRandomizeAllWeapons_Click(object sender, EventArgs e)
        {
            lblNote.Text = "All weapon damage values (Hull, Shield, and Lifetime/Range) are modified by a random value between -20% to +20% or -30% to +30% computed uniquely for EACH weapon";
        }

        private void rbAllShipStats_Click(object sender, EventArgs e)
        {
            lblNote.Text = "Primary ship statistics are modified by a single unique random value between -20% to +20% or -30% to +30% generated for each ship";
        }

        private void rbAllShipStatsForRace_Click(object sender, EventArgs e)
        {
            lblNote.Text = "Primary ship statistics FOR THE SELECTED RACE are each modified by a single unique random value between -20% to +20% or -30% to +30% generated for each ship";
        }

        private void rbLowerShipStatsForRace_Click(object sender, EventArgs e)
        {
            lblNote.Text = "Primary ship statistics FOR THE SELECTED RACE are each LOWERED by a single unique random value between -10% to -20% or -10% to -30% generated for each ship";
        }

        private void rbRaiseShipStatsForRace_Click(object sender, EventArgs e)
        {
            lblNote.Text = "Primary ship statistics FOR THE SELECTED RACE are each RAISED by a single unique random value between 10% to 20% or 10% to 30% generated for each ship";
        }


        private void btnDoIt_Click(object sender, EventArgs e)
        {
            try
            {
                m_Bullets = File.ReadAllLines("TBullets.txt").ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load TBullets.txt file.  Error: " + ex.Message);
                return;
            }

            try
            {
                m_Ships = File.ReadAllLines("TShips.txt").ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load TShips.txt file.  Error: " + ex.Message);
                return;
            }


            // What's selected.
            if (rbRandomizeAllWeapons.Checked)
            {
                RandomizeWeapons();
            }
            else if (rbAllShipStats.Checked)
            {
                RandomizeShipStats(EAdjust.ERandom);
            }
            else if (rbAllShipStatsForRace.Checked)
            {
                RandomizeShipStats(EAdjust.ERandom, (int)cbRace.SelectedValue);
            }
            else if (rbLowerShipStatsForRace.Checked)
            {
                RandomizeShipStats(EAdjust.ELower, (int)cbRace.SelectedValue);
            }
            else if (rbRaiseShipStatsForRace.Checked)
            {
                RandomizeShipStats(EAdjust.ERaise, (int)cbRace.SelectedValue);
            }

        }

        private void RandomizeShipStats(EAdjust adjust, int iRace = 0)
        {
            List<string> newShips = new List<string>();
            int count = 0;
            int countmodified = 0;

            foreach (string src in m_Ships)
            {
                count++;
                if (count < 4)
                {
                    newShips.Add(src);
                    continue;        // First 3 lines are junk.
                }
                if (src.Length < 5) break;      // end of line or some other barf.


                int adjustvalue;
                int range = rbTwentypercent.Checked ? 20 : 30;


                if (adjust == EAdjust.ERandom)
                {
                    adjustvalue = m_Rand.Next(-range, range + 1);
                }
                else if (adjust == EAdjust.ELower)
                {
                    adjustvalue = m_Rand.Next(-range, -9);
                }
                else
                {
                    adjustvalue = m_Rand.Next(10, range + 1);
                }

                bool modified = false;
                string mod = AdjustShip(src, adjustvalue, iRace, ref modified);                 // Every ship gets it's own random adjustment.
                if (modified) countmodified++;
                newShips.Add(mod);
            }

            m_Ships = newShips;
            File.WriteAllLines(MayhemFolder + "TShips.txt", m_Ships);

            MessageBox.Show($"{countmodified} ships were modified successfully. Adjustments made to Cargo space, Yaw, Pitch, Roll, Hull Strength, Power, Speed, Shield Count, Acceleration and Weapons Energy.");
        }


        private string AdjustShip(string src, int adjustvalue, int iRace, ref bool modified)
        {
            // Limit range from -10 to -20  or +10 to +20
            if (adjustvalue < 0)
            {
                if (adjustvalue > -10) adjustvalue = -10;
            }
            else
            {
                if (adjustvalue < 10) adjustvalue = 10;
            }

            float adjust = (100f + adjustvalue) / 100f;
            string[] items = src.Split(';');

            if (iRace != 0)
            {
                if (int.Parse(items[45]) != iRace)
                {
                    return src;     // sRace specified, but this ship isn't of that race.
                }
            }

            /*
            string name = items[16];
            if (name.Contains("Mercury_Hauler"))
            {
                int x = 5;
            }
            */

            // Modify stats here.
            // Removed adjustment of cargo just so a TL would always have enough cargo space to hold everything it needs.  
            // 6th item is the ID.  "SG_SH_TL" for example.
            if (!(items[5].EndsWith("_TL")))
            {
                items[28] = ((int)(adjust * float.Parse(items[28]))).ToString();                // Cargo Min
                items[29] = ((int)(adjust * float.Parse(items[29]))).ToString();                // Cargo Max
            }

            items[2] = ((adjust * float.Parse(items[2], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);   // Yaw, Pitch and Roll, floating point
            items[3] = ((adjust * float.Parse(items[3], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);                           
            items[4] = ((adjust * float.Parse(items[4], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);                           
            items[46] = ((int)(adjust * float.Parse(items[46]))).ToString();                    // Hull Strength
            items[13] = ((int)(adjust * float.Parse(items[13]))).ToString();                    // Power
            items[7] = ((int)(adjust * float.Parse(items[7]))).ToString();                      // Speed
            items[8] = ((int)(adjust * float.Parse(items[8]))).ToString();                      // Acceleration
            //items[20] = ((int)(adjust * float.Parse(items[20]))).ToString();                    // Weapons Energy

            int we = ((int)(adjust * float.Parse(items[20])));
            // 1.78 Apparently there is a 2 million limit on Weapons energy.
            if (we >= 2000000)
            {
                we = 1995000;
            }
            items[20] = we.ToString();

            // items[23] is the amount of Shields.                                                 Shield Count
            // if a ship can equip 5 or more shields and random price modifier is +10% or more then add 1 shield.
            // If modifier is -10% or less than subtract one shield.
            int numshields = int.Parse(items[23]);
            if (numshields > 4)
            {
                if (adjustvalue >= 10)
                {
                    numshields++;
                    items[23] = numshields.ToString();
                }
                if (adjustvalue <= -10)
                {
                    numshields--;
                    items[23] = numshields.ToString();
                }
            }

            // There's a whole bunch of variable length shit in between the beginning and the end for turrets.
            // Easier to just parse back from the end to get these two values.
            int prodrelval1 = items.Length - 10;     // end - 10
            int prodrelval2 = items.Length - 6;     // end - 6
            items[prodrelval1] = ((int)(adjust * float.Parse(items[prodrelval1], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);  // Production Rel value NPC
            items[prodrelval2] = ((int)(adjust * float.Parse(items[prodrelval2], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);  // Production Rel value Player 

            modified = true;
            return string.Join(";", items);
        }

        /// <summary>
        /// Randomize key values in the m_Bullets collection and Save back to the Mayhem 3 addon/types/ folder as TBullets.txt
        /// </summary>
        private void RandomizeWeapons()
        {
            List<string> newBullets = new List<string>();
            int count = 0;
            foreach(string src in m_Bullets)
            {
                if (count < 3)
                {
                    newBullets.Add(src);
                    count++;
                    continue;        // First 3 lines are junk.
                }
                if (src.Length < 5) break;      // end of line or some other barf.

                string mod;
                if (rbTwentypercent.Checked)
                {
                    mod = AdjustBullet(src, m_Rand.Next(-20, 21));                 // Every weapon gets it's own random adjustment.
                }
                else
                {
                    mod = AdjustBullet(src, m_Rand.Next(-30, 31));
                }
                newBullets.Add(mod);
                count++;
            }

            m_Bullets = newBullets;

            File.WriteAllLines(MayhemFolder + "TBullets.txt", m_Bullets);
            MessageBox.Show($"{count} Weapons were modified successfully.  Each has a maximum change of +\\-20 or +\\-30% change applied to Hull and Shield damage as well as range.");
        }

        private string AdjustBullet(string line, int amount)
        {
            // effects\weapons\bullet_ebc;
            // 0;
            // 0;
            // 0;
            // 0;
            // -1;
            // 8003;
            // 1358;            (7: Shield Damage)
            // 0;
            // 302;
            // 1400;            (10: Lifetime in milliseconds. Laser Range)
            // 750000;
            // 0;
            // 255;
            // 255;
            // 255;
            // 2.0;
            // 2.0;
            // 5.0;
            // 0;
            // 202;
            // 108;
            // 452;             (22: Hull Damage)
            // 0;
            // 0;
            // 0;
            // 255;
            // 0;
            // 0;
            // 0;
            // 0;
            // 0;
            // 0;
            // 0;
            // 0;
            // 0;
            // 0;
            // 210;             (37: OOS Shield Damage
            // 70;              (38: OOS Hull Damage)
            // 128;
            // 10000;25;1;0;10000;-100000;0;0;SS_BULLET_IRE;

            // Limit to range of -10 to -20 or +10 to +20.
            if (amount < 0)
            {
                if (amount > -10) amount = -10;
            }
            else
            {
                if (amount < 10) amount = 10;
            }

            float adjust = (100f + amount) / 100f;
            string[] items = line.Split(';');

            // Now alter the weapon.

            // Shield damage
            items[7] = ((int)(adjust * float.Parse(items[7], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);
            // Lifetime in ms (laser range
            items[10] = ((int)(adjust * float.Parse(items[10], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);
            // Hull Damage
            items[22] = ((int)(adjust * float.Parse(items[22], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);
            // OOS Shield Damage
            items[37] = ((int)(adjust * float.Parse(items[37], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);
            // OOS Hull Damage
            items[38] = ((int)(adjust * float.Parse(items[38], NumberFormatInfo.InvariantInfo))).ToString(NumberFormatInfo.InvariantInfo);

            return string.Join(";", items);
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (File.Exists(MayhemFolder + "TBullets.txt"))
            {
                File.Delete(MayhemFolder + "TBullets.txt");
            }
            if (File.Exists(MayhemFolder + "TShips.txt"))
            {
                File.Delete(MayhemFolder + "TShips.txt");
            }
            MessageBox.Show("\\addon\\TBullets.txt + \\addon\\TShips.txt have been deleted and will no longer override your game's normal values");
        }

        private void btnHelpMain_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("HelpMain.txt");
        }
    }
}
