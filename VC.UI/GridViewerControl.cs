using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VC.UI
{
    public class GridViewerControl : BasicUserControl
    {
        private readonly DxGridControl dxgGrid;

        public DxGridControl Grid
        {
            get { return dxgGrid; }
        }

        public GridViewerControl()
        {
            dxgGrid = new DxGridControl();
            dxgGrid.Dock = DockStyle.Fill;
            Controls.Add(dxgGrid);
        }

        public GridViewerControl(object dataSource)
            : this()
        {
            dxgGrid.DataSource = dataSource;
        }

        public static void Show(object dataSource)
        {
            BasicForm.ShowUserControl<GridViewerControl>("", dataSource);
        }
    }
}
