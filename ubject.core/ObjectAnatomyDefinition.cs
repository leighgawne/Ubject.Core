using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core
{
    public class ObjectAnatomyDefinition
    {
        private Dictionary<string, object> propertiesAndValues = new Dictionary<string, object>();
        private Dictionary<string, ICollection> collectionReferenceTypeObjects = new Dictionary<string, ICollection>();
        private Dictionary<string, ICollection> collectionValueTypeObjects = new Dictionary<string, ICollection>();
        private Dictionary<string, object> referenceTypeObjects = new Dictionary<string, object>();

        public Dictionary<string, object> PropertiesAndValues
        {
            get
            {
                return (propertiesAndValues);
            }
        }

        public Dictionary<string, ICollection> CollectionReferenceTypeObjects
        {
            get
            {
                return (collectionReferenceTypeObjects);
            }
        }

        public Dictionary<string, ICollection> CollectionValueTypeObjects
        {
            get
            {
                return (collectionValueTypeObjects);
            }
        }

        public Dictionary<string, object> ReferenceTypeObjects
        {
            get
            {
                return (referenceTypeObjects);
            }
        }
    }
}
