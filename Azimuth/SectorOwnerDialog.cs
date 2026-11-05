using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Azimuth
{
    internal sealed class SectorOwnerDialog : Form
    {
        private readonly ComboBox owners;
        internal int SelectedOwner { get { return (int)owners.SelectedValue; } }
        internal SectorOwnerDialog(IEnumerable<int> validOwners)
        {
            Text = X3Utils.Ui("Edit.AddTitle");
            Width = 340; Height = 150;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            var label = new Label { Text = X3Utils.Ui("Edit.ValidOwners"), Left = 12, Top = 12, Width = 300 };
            owners = new ComboBox { Left = 12, Top = 38, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Key", ValueMember = "Value", DataSource = validOwners.Select(i => new KeyValuePair<string, int>(OwnerName(i), i)).ToList() };
            var ok = new Button { Text = X3Utils.Ui("Edit.Add"), Left = 150, Top = 72, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = X3Utils.Ui("Edit.Cancel"), Left = 237, Top = 72, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { label, owners, ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }
        internal static string OwnerName(int id)
        {
            string key = id == 1 ? "Argon" : id == 2 ? "Boron" : id == 3 ? "Split" : id == 4 ? "Paranid"
                : id == 5 ? "Teladi" : id == 6 ? "Xenon" : id == 18 ? "Terran" : "Unknown";
            return X3Utils.GetLocalizedText("{" + key + "}");
        }
    }
}
