using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC.UI
{
    public class ParamsForm : BasicForm
    {
        public static bool ShowDialog(string message, params Param[] paramArgs)
        {
            return false;
        }
    }
}
