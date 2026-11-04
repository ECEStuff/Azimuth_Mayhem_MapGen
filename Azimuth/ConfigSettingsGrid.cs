// Shares named columns between runtime settings and Designer preview rows. Visual column order can change.

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Azimuth
{
    [ToolboxItem(false)]
    public class ConfigSettingsGrid : DataGridView
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode ||
                (FindForm() != null && FindForm().Site != null && FindForm().Site.DesignMode))
                BeginInvoke(new MethodInvoker(ShowDesignRows));
        }

        internal DataGridViewRow AddSettingRow(string id, string value, string section,
            string title, string defaultValue, string status = null)
        {
            var row = Rows[Rows.Add()];
            row.Cells["ID"].Value = id;
            row.Cells["Value"].Value = value;
            row.Cells["Section"].Value = section;
            row.Cells["Title"].Value = title;
            row.Cells["Default"].Value = defaultValue;
            row.Cells["Status"].Value = status;
            return row;
        }

        private void ShowDesignRows()
        {
            if (IsDisposed || Rows.Count != 0 || Columns.Count != 6) return;
            AddSettingRow("224", "2", "Combat", "Station attack behavior", "2", "Default");
            AddSettingRow("229", "1", "Combat", "Fighter combat maneuvers", "1", "Default");
            AddSettingRow("280", "10", "Combat", "Fighter turret firing delay (ms)", "10", "Default");
        }
    }
}
