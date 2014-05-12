using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace VC.UI
{
    [ToolboxItem(true)]
    public class DxButton : SimpleButton
    {

    }

    [ToolboxItem(true)]
    public class DxTextEdit : TextEdit
    {
        
    }


    [ToolboxItem(true)]
    public class DxTextDisplayEdit : TextEdit
    {
        public DxTextDisplayEdit()
        {
            Properties.ReadOnly = true;
        }
    }

    [ToolboxItem(true)]
    public class DxLabel : LabelControl
    {

    }

    [ToolboxItem(true)]
    public class DxTestDisplayEdit : TextEdit
    {
        public DxTestDisplayEdit()
        {
            Properties.ReadOnly = true;
        }
    }

    [ToolboxItem(true)]
    public class DxComboBox : ComboBoxEdit
    {
        //public ComboBoxItemCollection Items
        //{
        //    get
        //    {
        //        return Properties.Items;
        //    }
        //}

        public void AddItem(object value)
        {
            Properties.Items.Add(value);
        }

        public void AddEnumValues(Type enumType)
        {
            foreach (object ttype in Enum.GetValues(enumType))
            {
                Properties.Items.Add(ttype);
            }
        }
    }

    [ToolboxItem(true)]
    public class DxMemoEdit : MemoEdit
    {
        
    }

    [ToolboxItem(true)]
    public class DxDateEdit : DateEdit
    {

    }

    [ToolboxItem(true)]
    public class DxCheckedListBoxControl : CheckedListBoxControl
    {

    }
}
