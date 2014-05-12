using System;

namespace VC
{
    public interface IHasId
    {
        string Id { get; }
    }

    public interface IHasGuid
    {
        string Guid { get; }
    }

    public interface IHasCaption
    {
        string Caption { get; }
    }

    public interface IHasKey<T>
    {
        T Key { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ActionAttribute : Attribute, IHasCaption
    {
        public string Caption { get; set; }

        public ActionAttribute()
        {
            
        }
        public ActionAttribute(string caption)
            : this()
        {
            Caption = caption;
        }
    }
    
    [AttributeUsage(AttributeTargets.All)]
    public class DisplayAttribute : Attribute, IHasCaption
    {
        public string Caption { get; set; }
        public bool IsVisible { get; set; }
        /// <summary>
        /// Hidden meaning not available to be added to a grid
        /// </summary>
        public bool IsHidden { get; set; }
        /// <summary>
        /// Display priority.  Default = 0. Usually 100 is highest priority, but will rank on any integer value.
        /// </summary>
        public int Priority { get; set; }
        public string FormatString { get; set; }
        public bool FixedColumn { get; set;}
        public int FixedWidth { get; set; }
        public bool ReadOnly { get; set; }
        public System.Windows.Forms.SortOrder Sort { get; set; }

        public DisplayAttribute()
        {
            IsVisible = true;
            IsHidden = false;
        }
        public DisplayAttribute(bool isVisible, string caption = null)
            : this()
        {
            Caption = caption;
            IsVisible = isVisible;
        }
        public DisplayAttribute(string caption, bool isVisible = true)
            : this(isVisible, caption)
        {
        }
    }
}
