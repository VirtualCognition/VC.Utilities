using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VC;

namespace VC.UI
{
    public class BasicUserControl : UserControl
    {
        protected Timer _displayUpdateTimer;

        public BasicUserControl()
        {
            Load += BasicUserControl_Load;
        }
        
        protected override void Dispose(bool disposing)
        {
            Util.Dispose(ref _displayUpdateTimer);

            base.Dispose(disposing);
        }
        
        void BasicUserControl_Load(object sender, EventArgs e)
        {

        }

        protected void StartDisplayUpdates(int refreshRateMs = 1000)
        {
            if (_displayUpdateTimer != null) return;
            
            _displayUpdateTimer = new Timer();
            _displayUpdateTimer.Tick += _displayUpdateTimer_Tick;
            _displayUpdateTimer.Interval = refreshRateMs;
            _displayUpdateTimer.Start();
        }

        protected void StopDisplayUpdates()
        {
            Util.Dispose(ref _displayUpdateTimer);
        }

        void _displayUpdateTimer_Tick(object sender, EventArgs e)
        {
            DisplayUpdate();
        }

        protected virtual void DisplayUpdate()
        {
            if (DesignMode) return;


        }

        public void RunThreadedOperation(Func<bool> operation, string name, Action cleanupAction = null)
        {
            Alerts.Status("Running {0}...", name);

            BeginInvoke((MethodInvoker)(() =>
                        {
                            try
                            {
                                var success = operation();

                                if (success)
                                {
                                    Alerts.Status("{0} completed successfully.", name);
                                }
                                else
                                {
                                    Alerts.Status("{0} failed!", name);
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandler.HandleException(ex, "Exception during " + name + ":");

                                Alerts.Status("Exception while executing: " + name);
                            }

                            if (cleanupAction != null)
                            {
                                try
                                {
                                    cleanupAction();
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandler.HandleException(ex, "Exception during cleanup for " + name + ":");
                                }
                            }
                        }));
        }
    }
}
