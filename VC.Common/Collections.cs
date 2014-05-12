using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public static class CollectionExtensions
    {
        public static TV GetOrDefault<TK, TV>(this Dictionary<TK, TV> dictionary, TK key) 
            where TV:class
        {
            TV val;
            if (dictionary.TryGetValue(key, out val))
            {
                return val;
            }
            else
            {
                return null;
            }
        }
    }
}
