using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars.Docking2010.Views;
using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;

namespace VC.UI
{
    public class StandardUserControl : BasicUserControl
    {
        protected DxLayoutControl _layout;
        protected LayoutControlGroup _grpRoot;
        protected LayoutControlGroup _grpTop;
        protected EmptySpaceItem _itmTopEmptySpace;
        protected TabbedControlGroup _grpPrimary;

        public StandardUserControl()
            : base()
        {
            _layout = new DxLayoutControl()
                      {

                      };
            _layout.Dock = DockStyle.Fill;
            Controls.Add(_layout);

            _grpRoot = LayoutExtensions.CreateDefaultLayoutGroup("layoutControlGroupRoot");
            //_grpRoot.Location = new System.Drawing.Point(0, 0);
            //_grpRoot.Size = new System.Drawing.Size(555, 311);
            _layout.Root = _grpRoot;

            _grpTop = LayoutExtensions.CreateDefaultLayoutGroup("layoutControlGroupTop");

            _grpRoot.Items.AddRange(new BaseLayoutItem[] { _grpTop });

            _itmTopEmptySpace = LayoutExtensions.CreateDefaultEmptySpaceItem("itmTopEmptySpace");

            _grpTop.AddItem(_itmTopEmptySpace);

            _grpPrimary = _grpRoot.AddTabbedGroup(_grpTop, InsertType.Bottom);
            _grpPrimary.Padding = new DevExpress.XtraLayout.Utils.Padding(0);
            _grpPrimary.UpdateTabHeaders();
        }

        public DxButton AddButton(string label, EventHandler onClick)
        {
            var button = new DxButton()
                         {
                             Text = label
                         };
            button.Click += onClick;

            // For now
            button.AutoWidthInLayoutControl = true;

            Controls.Add(button);

            AddTopControl(button);

            return button;
        }

        public DxGridControl AddGrid(string label = null, object datasource = null)
        {
            var grid = new DxGridControl();

            AddPrimaryControl(label, grid);

            return grid;
        }

        protected void AddTopControl(Control control)
        {
            var itm = LayoutExtensions.CreateDefaultLayoutItem("itm" + control.Name);

            itm.Control = control;

            _grpTop.AddItem(itm, _itmTopEmptySpace, InsertType.Left);
        }

        protected void AddPrimaryControl(string title, Control control)
        {
            var itm = LayoutExtensions.CreateDefaultLayoutItem("itm" + control.Name);

            itm.Control = control;

            var grp = _grpPrimary.AddTab(title, control);
        }
    }
}
