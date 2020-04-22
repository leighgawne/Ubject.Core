using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Ubject.Core.Database;

namespace Ubject.Core
{
    public class Utilities
    {
        public static string MD5HashFromList(List<object> inputList)
        {
            return (MD5HashFromString(string.Join("", inputList)));
        }

        public static string MD5HashFromString(string inputString)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(inputString);
            byte[] hash = MD5.Create().ComputeHash(inputBytes);

            return (BitConverter.ToString(hash).Replace("-", ""));
        }

        public static IDbConnection CreateDbConnection(IDbManager dbManager)
        {
            var dbConnection = UbjectDIBindings.Resolve<IDbConnection>();
            dbConnection.ConnectionString = dbManager.ConnectionString;
            return (dbConnection);
        }

        public static IDbManager CreateDbManager(string dbContext)
        {
            var dbManager = UbjectDIBindings.Resolve<IDbManager>();
            dbManager.SchemaName = dbContext;
            return (dbManager);
        }

        public static IDbTableManager CreateDbTableManager(IDbManager dbManager)
        {
            var dbTableManager = UbjectDIBindings.Resolve<IDbTableManager>();
            dbTableManager.DbManager = dbManager;
            return (dbTableManager);
        }

        public static T ChangeType<T>(object value)
        {
            var t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default(T);
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (T)Convert.ChangeType(value, t);
        }

        public static object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return Convert.ChangeType(value, t);
        }
    }
}
