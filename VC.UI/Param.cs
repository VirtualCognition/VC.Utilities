using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public abstract class Param
    {
        
    }

    public class Param<T> : Param
    {
        public T Value { get; set; }

        public Param()
        {
            
        }
        public Param(string name)
            : this()
        {
            
        }
        public Param(string name, T defaultValue)
            : this(name)
        {
            Value = defaultValue;
        }
    }
}
