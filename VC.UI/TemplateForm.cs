using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraLayout;

namespace VC.UI
{
    public class TemplateForm : BasicForm
    {
        protected DxLayoutControl ctrlLayoutPrimary;
        protected TabbedGroup ctrlPrimaryTabGroup;
        protected BarManager barManager;
        protected Bar barMainMenu;
        protected BarSubItem mnuOptions;
        protected BarSubItem mnuDebug;
        protected DockManager dockManager;
        protected AlertsControl ctrlAlerts;
        protected DockPanel dockPanelAlerts;

        public TemplateForm()
        {
            ctrlLayoutPrimary = new DxLayoutControl();
            ctrlLayoutPrimary.Dock = DockStyle.Fill;
            ctrlLayoutPrimary.Root.Padding = new DevExpress.XtraLayout.Utils.Padding(0);
            Controls.Add(ctrlLayoutPrimary);

            ctrlPrimaryTabGroup = ctrlLayoutPrimary.AddTabbedGroup();
            ctrlPrimaryTabGroup.Padding = new DevExpress.XtraLayout.Utils.Padding(0);

            barManager = new BarManager();
            barManager.Form = this;
            barManager.BeginUpdate();

            barManager.AllowCustomization = false;

            barMainMenu = new Bar(barManager);
            barMainMenu.Text = "Main Menu";
            barMainMenu.DockStyle = BarDockStyle.Top;
            barMainMenu.OptionsBar.AllowQuickCustomization = false;
            barMainMenu.OptionsBar.AllowDelete = false;
            barMainMenu.OptionsBar.DisableCustomization = true;
            barMainMenu.OptionsBar.DisableClose = true;
            barMainMenu.OptionsBar.DrawDragBorder = false;
            barMainMenu.OptionsBar.DrawBorder = false;
            barManager.Bars.Add(barMainMenu);
            barManager.MainMenu = barMainMenu;

            AddStandardMenuItems();

            barManager.EndUpdate();

            dockManager = new DockManager();
            dockManager.Form = this;
            dockManager.Docking += dockManager_Docking;
            dockManager.DockingOptions.HideImmediatelyOnAutoHide = true;
            dockManager.DockingOptions.FloatOnDblClick = false;
            dockManager.PopupMenuShowing += (sender, args) => args.Cancel = true;

            ctrlAlerts = new AlertsControl();
            ctrlAlerts.Dock = DockStyle.Fill;

            dockPanelAlerts = dockManager.AddPanel(DockingStyle.Bottom);
            dockPanelAlerts.Text = "Alerts";
            dockPanelAlerts.Options.AllowDockAsTabbedDocument = false;
            dockPanelAlerts.Options.AllowFloating = false;
            dockPanelAlerts.Options.ShowCloseButton = false;
            dockPanelAlerts.Options.ShowMaximizeButton = false;
            dockPanelAlerts.Visibility = DockVisibility.AutoHide;
            dockPanelAlerts.Controls.Add(ctrlAlerts);

            ctrlAlerts.TextChanged += (sender, args) => dockPanelAlerts.Text = ctrlAlerts.Text;
        }
        
        protected T AddControl<T>(string name, params object[] args)
            where T : UserControl
        {
            T ctrl = Util.Create<T>(args);

            if (ctrl == null)
            {
                throw new InvalidOperationException("Failed to create control:" + typeof(T).ToString());
            }

            ctrlPrimaryTabGroup.AddTab(name, ctrl);

            return ctrl;
        }

        public void AddStandardMenuItems()
        {
            mnuOptions = new BarSubItem(barManager, "Options");
            barManager.Items.Add(mnuOptions);
            barMainMenu.ItemLinks.Add(mnuOptions);

            mnuDebug = new BarSubItem(barManager, "Debug");
            mnuDebug.Visibility = App.IsDebug ? BarItemVisibility.Always : BarItemVisibility.Never;
            barManager.Items.Add(mnuDebug);
            barMainMenu.ItemLinks.Add(mnuDebug);

            AddCheckMenuItem("Console Visible", (visible) => WinConsole.WinConsole.Visible = visible, () => WinConsole.WinConsole.Visible, "Options");
            AddCheckMenuItem("Show Debug Menu", (visible) => mnuDebug.Visibility = visible ? BarItemVisibility.Always : BarItemVisibility.Never, () => mnuDebug.Visibility == BarItemVisibility.Always, "Options");

            AddMainMenuItem("Test Errors", mnuTestErrors, "Debug", "Utils");
        }

        /// <summary>
        /// Find the relevant BarItemLinkCollection and add any necessary subgroups
        /// </summary>
        /// <param name="parent">If relevant, the parent sub item.  Otherwise null.</param>
        /// <returns></returns>
        private BarItemLinkCollection GetSubMenu(BarItemLinkCollection items, IList<string> groups, out BarSubItem parent)
        {
            if (groups.Count == 0)
            {
                parent = null;
                return items;
            }

            var caption = groups[0];
            groups.RemoveAt(0);

            BarSubItem subMenu = null;

            foreach (BarItemLink link in items)
            {
                var item = link.Item;

                if (Util.IsEquivalent(caption, item.Caption))
                {
                    var subMenuItem = item as BarSubItem;

                    if (subMenuItem != null)
                    {
                        subMenu = subMenuItem;
                        break;
                    }
                }
            }

            if (subMenu == null)
            {
                subMenu = new BarSubItem(barManager, caption);
                barManager.Items.Add(subMenu);
                items.Add(subMenu);
            }

            var subItems = GetSubMenu(subMenu.ItemLinks, groups, out parent);

            if (parent == null)
            {
                // Better to have a parent two levels up if it came to that..
                parent = subMenu;
            }

            return subItems;
        }
        
        public void AddMainMenuItem(string caption, Action onClick, params string[] groups)
        {
            BarSubItem subMenu;

            var itemLinks = GetSubMenu(barMainMenu.ItemLinks, groups.ToList(),out subMenu);

            if (itemLinks == null)
            {
                throw new ArgumentException("Invalid menu groups!");
            }

            var item = new BarButtonItem(barManager, caption);
            barManager.Items.Add(item);
            itemLinks.Add(item);
            item.ItemClick += (sender, args) => onClick();
        }

        public void AddCheckMenuItem(string caption, Action<bool> onClick, Func<bool> isChecked, params string[] groups)
        {
            BarSubItem subMenu;

            var itemLinks = GetSubMenu(barMainMenu.ItemLinks, groups.ToList(), out subMenu);

            if (itemLinks == null)
            {
                throw new ArgumentException("Invalid menu groups!");
            }

            var item = new BarCheckItem(barManager);
            item.Caption = caption;
            item.Checked = isChecked();
            barManager.Items.Add(item);
            itemLinks.Add(item);
            item.ItemClick += (sender, args) => onClick(item.Checked);
            subMenu.Popup += (sender, args) =>
                             {
                                 try
                                 {
                                     item.Checked = isChecked();
                                 }
                                 catch (Exception ex)
                                 {
                                     Alerts.Warning("UI Exception For MenuItem IsChecked() Call");
                                 }
                             };
        }

        void dockManager_Docking(object sender, DockingEventArgs e)
        {
            if (e.TargetPanel == dockPanelAlerts)
            {
                e.Cancel = true;
            }
        }

        #region Debug Functions

        protected void mnuTestErrors()
        {
            Alerts.Error("Test", "1");
            Alerts.Warning("Test", "2");
            Alerts.Error("Test", "3");
        }

        #endregion
    }
}
