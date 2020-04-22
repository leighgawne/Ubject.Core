using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core
{
    public class UbjectQueryHelper
    {
        public static List<Tuple<string, string, string>> AfterTimestampQuery(long timestamp)
        {
            var query = new List<Tuple<string, string, string>>();
            query.Add(new Tuple<string, string, string>(UbjectMetadata.UBJECT_TIMESTAMP, timestamp.ToString(), ">"));
            return (query);
        }

        public static List<Tuple<string, string, string>> MatchFieldQuery(string field, string value)
        {
            var query = new List<Tuple<string, string, string>>();
            query.Add(new Tuple<string, string, string>(field, value, "="));
            return (query);
        }
    }
}
