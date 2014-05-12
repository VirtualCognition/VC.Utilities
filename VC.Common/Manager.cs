using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC
{
    public abstract class Manager<T, K>
        where T:class
    {
        protected readonly List<T> _members = new List<T>();
        protected readonly Dictionary<K, T> _memberDictionary = new Dictionary<K, T>();

        public IReadOnlyList<T> Members { get { return _members; } }


    }

}
