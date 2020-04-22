using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core
{
    public class UbjectMetadata
    {
        public static readonly List<string> allMetadataFields;
        public static readonly string PRIMARY_KEY = "PRIMARYKEY";
        public static readonly string UBJECT_HASH = "UBJECTHASH";
        public static readonly string UBJECT_TIMESTAMP = "UBJECTTIMESTAMP";

        private long primaryKey;
        private string ubjectHash;
        private long ubjectTimestamp;

        static UbjectMetadata()
        {
            allMetadataFields = new List<string>() { PRIMARY_KEY.ToLower(), UBJECT_HASH.ToLower(), UBJECT_TIMESTAMP.ToLower() };
        }

        public long PrimaryKey
        {
            get { return (primaryKey); }
            set { primaryKey = value; }
        }

        public string UbjectHash
        {
            get { return (ubjectHash); }
            set { ubjectHash = value; }
        }

        public long UbjectTimestamp
        {
            get { return (ubjectTimestamp); }
            set { ubjectTimestamp = value; }
        }

        public UbjectMetadata()
        {
        }

        public UbjectMetadata(long primaryKey, string ubjectHash, long ubjectTimestamp)
        {
            PrimaryKey = primaryKey;
            UbjectHash = ubjectHash;
            UbjectTimestamp = ubjectTimestamp;
        }

        public void SetPropertyValue(string name, object value)
        {
            PropertyInfo propertyInfo = GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            propertyInfo.SetValue(this, Utilities.ChangeType(value, propertyInfo.PropertyType), null);
        }

        public static bool IsMetadataField(string fieldName)
        {
            return (allMetadataFields.Contains(fieldName.ToLower()));
        }
    }
}
