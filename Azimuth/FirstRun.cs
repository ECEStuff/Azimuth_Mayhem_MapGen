using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Azimuth
{
    public partial class FirstRun : Form
    {
        public FirstRun()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FirstRun_Load(object sender, EventArgs e)
        {
            rtb1.AppendText("You are running the new X3 Map Generator (X3MG) for the very first time.\n\n");
            rtb1.AppendText("*** PLEASE MANUALLY BACKUP YOUR CURRENT SAVED GAMES BEFORE PROCEEDING. ***\n\n");
            rtb1.AppendText("If you don't need your current saved games, you can safely ignore this warning.\n\n\n");
            rtb1.AppendText("The new X3MG will properly backup your saved games when you switch maps, but ONLY after you first establish a \"current\" map in THIS application.  ");
            rtb1.AppendText("When running the new X3MG for the very first time, it will NOT recognize which map is \"current\" from the old map generator system written by the original Mayhem 3 author.\n");
            rtb1.AppendText(@"Note:  Saved games are normally stored in: C:\Users\Yourlogin\Documents\Egosoft\X3AP\save\");

        }
    }
}
