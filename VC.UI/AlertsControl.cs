using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VC.UI
{
    public class AlertsControl : BasicUserControl
    {
        protected readonly DxGridControl dxgAlerts;
        private int lastCount = 0;

        public AlertsControl()
        {
            dxgAlerts = new DxGridControl();
            dxgAlerts.Dock = DockStyle.Fill;
            Controls.Add(dxgAlerts);

            Load += AlertsControl_Load;
        }

        void AlertsControl_Load(object sender, EventArgs e)
        {
            dxgAlerts.DataSource = Alerts.GetAllAlerts();

            StartDisplayUpdates();
        }

        protected override void DisplayUpdate()
        {
            base.DisplayUpdate();

            dxgAlerts.RefreshDataSource();

            var cnt = Alerts.Count;

            if (lastCount != cnt)
            {
                lastCount = cnt;

                var sb = new StringBuilder("Alerts ");

                int errors = Alerts.GetCount(AlertType.Error);
                if (errors > 0)
                {
                    sb.AppendFormat("Errors: {0} ", errors);
                }

                int warnings = Alerts.GetCount(AlertType.Warning);
                if (warnings > 0)
                {
                    sb.AppendFormat("Warnings: {0} ", warnings);
                }

                if (errors == 0 && warnings == 0)
                {
                    int infos = Alerts.GetCount(AlertType.Info);
                    if (infos > 0)
                    {
                        sb.AppendFormat("Infos: {0} ", warnings);
                    }
                    else
                    {
                        sb.AppendFormat("None");
                    }
                }

                Text = sb.ToString();
            }
        }
    }
}
