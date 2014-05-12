using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using System.Windows.Forms;
using DevExpress.XtraGrid.Columns;
using DevExpress.Utils;
using System.Drawing;
using System.Reflection;
using System.Collections;
using DevExpress.XtraGrid.Registrator;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Base.ViewInfo;
using DevExpress.Utils.Menu;
using DevExpress.XtraGrid.Menu;
using VC;

namespace VC.UI
{
    [ToolboxItem(true)]
    public class DxGridControl : GridControl
    {
        public string Id { get; set; }
        public DxGridView View
        {
            get
            {
                return MainView as DxGridView;
            }
        }

        public DxGridControl()
        {
            
        }

        protected override void RegisterAvailableViewsCore(InfoCollection collection)
        {
            base.RegisterAvailableViewsCore(collection);

            collection.Add(new MyGridViewInfoRegistrator());
        }

        protected override BaseView CreateDefaultView()
        {
            return CreateView(DxGridView.DxGridViewName);
        }

        public void Set(object dataSource)
        {
            if (dataSource == DataSource)
            {
                View.RefreshData();
            }
            else
            {
                DataSource = dataSource;
            }
        }
    }

    public class MyGridViewInfoRegistrator : GridInfoRegistrator
    {
        public override string ViewName { get { return DxGridView.DxGridViewName; } }

        public override BaseView CreateView(GridControl grid)
        {
            return new DxGridView(grid);
        }
    }

    public class DxGridView : GridView
    {
        public const string DxGridViewName = "DxGridView";

        protected List<Tuple<string, Action<DxGridView>>> _rowMenuItems = new List<Tuple<string,Action<DxGridView>>>();

        public bool DoUseFirstRowForType { get; set; }

        public event Action<DxGridView, GridViewMenu> MenuShowing;

        public DxGridView() : this(null) 
        {
            
        }
        public DxGridView(GridControl gc)
            : base(gc)
        {
            OptionsView.ShowGroupPanel = false;
            OptionsView.ColumnAutoWidth = false;
            OptionsSelection.MultiSelect = true;
            OptionsSelection.MultiSelectMode = GridMultiSelectMode.RowSelect;
            OptionsBehavior.AllowIncrementalSearch = true;
            BestFitMaxRowCount = 20;
            Appearance.HeaderPanel.TextOptions.WordWrap = WordWrap.Wrap;
            Appearance.HeaderPanel.Options.UseTextOptions = true;

            PopupMenuShowing += DxGridView_PopupMenuShowing;
        }

        protected override string ViewName { get { return DxGridViewName; } }

        public override void PopulateColumns()
        {
            base.PopulateColumns();

            if (GridControl.DataSource == null)
            {
                return;
            }

            Type type = Util.GetDataSourceType(DataSource);

            if (type == null)
            {
                Util.Msg("UI Error: Can't determine element type for data source");
            }

            var colList = new List<GridColumn>();
            var priDict = new Dictionary<GridColumn, int>(Columns.Count);

            for (int i = 0; i < Columns.Count; i++)
            {
                GridColumn col = Columns[i];

                col.Caption = Util.PrettyParse(col.Caption);

                colList.Add(col);
                priDict[col] = 0;

                if (col.FieldName == null || type == null)
                {
                    continue;
                }

                PropertyInfo info = type.GetProperty(col.FieldName);

                if (info == null)
                {
                    continue;
                }

                var format = Util.GetDefaultFormatString(info.PropertyType);

                if (!string.IsNullOrWhiteSpace(format))
                {
                    col.DisplayFormat.FormatString = format;
                    col.DisplayFormat.FormatType = FormatType.Custom;
                }

                DisplayAttribute[] props = (DisplayAttribute[])info.GetCustomAttributes(typeof(DisplayAttribute), true);
                if (props != null && props.Length > 0)
                {
                    foreach (DisplayAttribute prop in props)
                    {
                        if (prop.IsVisible)
                        {
                            col.Visible = true;
                        }
                        else
                        {
                            col.Visible = false;

                            if (prop.IsHidden) 
                            {
                                col.OptionsColumn.ShowInCustomizationForm = false;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(prop.Caption))
                        {
                            col.Caption = prop.Caption;
                        }
                        if (prop.Priority != 0)
                        {
                            priDict[col] = prop.Priority;
                        }
                        if (prop.FixedColumn)
                        {
                            col.Fixed = FixedStyle.Left;
                        }
                        if (prop.FixedWidth > 0)
                        {
                            col.OptionsColumn.FixedWidth = true;
                            col.Width = prop.FixedWidth;
                        }
                        if (prop.ReadOnly)
                        {
                            col.OptionsColumn.ReadOnly = true;
                        }
                        if (prop.FormatString != null)
                        {
                            col.DisplayFormat.FormatString = prop.FormatString;
                            col.DisplayFormat.FormatType = FormatType.Custom;
                        }
                    }
                }
                
                if (col.OptionsColumn.ReadOnly)
                {
                    col.OptionsColumn.AllowEdit = false;
                }
            }

            // Ok, now we have any assigned priorities - let's rank the columns

            var colVisible = colList.Where(c => c.Visible);

            var colSet = new HashSet<GridColumn>(colVisible);

            {
                var oddList = colVisible.Where(c => c.VisibleIndex < 0);

                if (oddList.Any())
                {
                    Util.BreakDebug();
                    Util.Msg("UI Error: Inconsistent Column VisibleIndex");
                }
            }

            var sortedList = colVisible.OrderByDescending(c => priDict[c]).ThenBy(c => c.VisibleIndex);

            var idxDisplay = 0;

            foreach (var col in sortedList)
            {
                var idx = idxDisplay++;

                if (idx != col.VisibleIndex)
                {
                    col.VisibleIndex = idx;
                }

                colSet.Remove(col);
            }

            foreach (var col in colSet)
            {
                if (col.Visible)
                {
                    // Well how did that happen?

                    Util.BreakDebug();

                    col.VisibleIndex = idxDisplay++;
                }
            }
        }

        public IReadOnlyList<object> GetSelectedItems()
        {
            var list = new List<object>();

            var indicies = this.GetSelectedRows();

            if (indicies == null || indicies.Length == 0)
            {
                return list;
            }

            foreach (int idx in indicies)
            {
                var obj = GetRow(idx);

                list.Add(obj);
            }

            return list;
        } 

        public void RegisterRowMenuItem(string caption, Action<DxGridView> del)
        {
            _rowMenuItems.Add(new Tuple<string,Action<DxGridView>>(caption, del));
        }

        public void RegisterRowMenuItem(string caption, Action<IReadOnlyList<object>> del)
        {
            _rowMenuItems.Add(new Tuple<string, Action<DxGridView>>(caption, view => del(GetSelectedItems())));
        }

        void DxGridView_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            if (e.MenuType == GridMenuType.Column)
            {
                var colMenu = e.Menu as GridViewColumnMenu;

                if (colMenu == null) throw new InvalidOperationException("Unexpected menu type");

                colMenu.Items.Add(new DXMenuItem("Toggle Filter Row", 
                    (s, args) => { OptionsView.ShowAutoFilterRow = !OptionsView.ShowAutoFilterRow; })
                {
                    BeginGroup = true,
                });

                colMenu.Items.Add(new DXMenuItem("Toggle Footer",
                    (s, args) => { OptionsView.ShowFooter = !OptionsView.ShowFooter; })
                {
                    BeginGroup = true,
                });
            }
            else
            {
                var isFirst = true;

                for (int i = 0; i < _rowMenuItems.Count; i++)
                {
                    var entry = _rowMenuItems[i];

                    var item = new DXMenuItem(entry.Item1, (s, args) => entry.Item2(this))
                               {
                                   BeginGroup = isFirst,
                               };

                    e.Menu.Items.Add(item);

                    isFirst = false;
                }
            }

            if (MenuShowing != null)
            {
                MenuShowing(this, e.Menu);
            }
        }

    }
}
