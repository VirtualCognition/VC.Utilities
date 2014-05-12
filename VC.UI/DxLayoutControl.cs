using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraLayout;
using Padding = DevExpress.XtraLayout.Utils.Padding;

namespace VC.UI
{
    [ToolboxItem(true)]
    public class DxLayoutControl : DevExpress.XtraLayout.LayoutControl
    {
        public DxLayoutControl()
        {
            Dock = DockStyle.Fill;
        }
    }

    public static class LayoutExtensions
    {
        public static LayoutControlGroup CreateDefaultLayoutGroup(string name)
        {
            var grp = new LayoutControlGroup();
            grp.Text = grp.CustomizationFormText = grp.Name = name;
            grp.EnableIndentsWithoutBorders = DefaultBoolean.True;
            grp.GroupBordersVisible = false;
            grp.Padding = new DevExpress.XtraLayout.Utils.Padding(0, 0, 0, 0);
            grp.TextVisible = false;
            return grp;
        }

        public static EmptySpaceItem CreateDefaultEmptySpaceItem(string name)
        {
            var itm = new EmptySpaceItem();
            itm.AllowHotTrack = false;
            itm.CustomizationFormText = itm.Text = itm.Name = name;
            //itm.Location = new System.Drawing.Point(40, 0);
            //itm.Size = new System.Drawing.Size(471, 27);
            itm.TextSize = new System.Drawing.Size(0, 0);
            return itm;
        }

        public static LayoutControlItem CreateDefaultLayoutItem(string name)
        {
            var itm = new LayoutControlItem();

            itm.CustomizationFormText = itm.Text = itm.Name = name;
            itm.TextSize = new System.Drawing.Size(0, 0);
            itm.TextToControlDistance = 0;
            itm.TextVisible = false;

            return itm;
        }

        public static LayoutGroup AddTab(this TabbedGroup group, string name, Control ctrl)
        {
            var grp = @group.AddTabPage(name);

            grp.Padding = new Padding(2);

            var item = grp.AddItem() as LayoutControlItem;

            Debug.Assert(item != null, "item != null");

            item.Control = ctrl;
            item.TextVisible = false;
            item.Padding = new Padding(0);

            grp.Add(item);

            UpdateTabHeaders(@group);

            return grp;
        }

        public static void UpdateTabHeaders(this TabbedGroup group)
        {
            if (@group.TabPages.Count == 1)
            {
                @group.ShowTabHeader = DefaultBoolean.False;
            }
            else if (@group.ShowTabHeader != DefaultBoolean.True)
            {
                @group.ShowTabHeader = DefaultBoolean.True;
            }
        }
    }
}
