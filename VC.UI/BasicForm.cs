using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VC.UI
{
    public class BasicForm : Form
    {
        public BasicForm()
        {
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public static void ShowUserControl<T>(string title, params object[] args)
            where T : UserControl
        {
            ShowUserControl<T>(title, null, args);
        }

        public static void ShowUserControl<T>(string title, Action<T> initAction, params object[] args)
            where T:UserControl
        {
            Util.RunThreadedSta(() =>
                             {
                                 try
                                 {
                                     var form = new BasicForm();

                                     var ctrl = Util.Create<T>(args);

                                     ctrl.Dock = DockStyle.Fill;

                                     form.Controls.Add(ctrl);

                                     if (initAction != null)
                                     {
                                         initAction(ctrl);
                                     }

                                     Application.Run(form);
                                 }
                                 catch (Exception ex)
                                 {
                                     ExceptionHandler.HandleException(ex, "Top Level Form.ShowUserControl Unhandled Exception:", true);
                                 }
                             });
        }

        public static T ShowUserControlInline<T>(string title, IWin32Window owner, params object[] args)
            where T : UserControl
        {
            var form = new BasicForm();

            var ctrl = Util.Create<T>(args);

            ctrl.Dock = DockStyle.Fill;

            form.Controls.Add(ctrl);

            form.Show(owner);

            return ctrl;
        }
    }
}
