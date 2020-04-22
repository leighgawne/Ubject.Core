using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core
{
    public class UbjectMapping
    {
        private string parentTableKey;
        private string childTableKey;
        private string parentObjectKey;
        private string childObjectKey;

        public string ParentTableKey
        {
            get
            {
                return (parentTableKey);
            }
            set
            {
                parentTableKey = value;
            }
        }

        public string ChildTableKey
        {
            get
            {
                return (childTableKey);
            }
            set
            {
                childTableKey = value;
            }
        }

        public string ParentObjectKey
        {
            get
            {
                return (parentObjectKey);
            }
            set
            {
                parentObjectKey = value;
            }
        }

        public string ChildObjectKey
        {
            get
            {
                return (childObjectKey);
            }
            set
            {
                childObjectKey = value;
            }
        }

        public UbjectMapping()
        {
        }

        public UbjectMapping(string parentTableKey, string parentObjectKey, string childTableKey, string childObjectKey)
        {
            ParentTableKey = parentTableKey;
            ParentObjectKey = parentObjectKey;
            ChildTableKey = childTableKey;
            ChildObjectKey = childObjectKey;
        }
    }
}
