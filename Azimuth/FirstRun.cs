// Shows the first-run save ownership and backup instructions before switching galaxies.

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
            X3Utils.SetControlChildrenText(this);
            Text = X3Utils.Ui("FirstRun.Title");
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FirstRun_Load(object sender, EventArgs e)
        {
            rtb1.Text = X3Utils.Ui("FirstRun.Warning");

        }
    }
}
