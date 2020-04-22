using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ubject.Core.Database
{
    public class AzureDbTableManager : BaseDbTableManager
    {
        private List<string> columns = new List<string>();

        public override string QueryTableName
        {
            get
            {
                return (string.Format("{0}.dbo.\"{1}\"", dbManager.SchemaName, TableName));
            }
        }

        public override long AddObject(Dictionary<string, object> propertiesAndValues)
        {
            long primaryKey = 0;

            try
            {
                if (propertiesAndValues.Count > 0)
                {
                    var parameters = CreateParameters(propertiesAndValues);
                    var propertiesAndParameters = propertiesAndValues.Keys.Zip(parameters.Keys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

                    string md5Hash = Utilities.MD5HashFromList(propertiesAndValues.Values.ToList());

                    StringBuilder commandText = new StringBuilder();
                    commandText.Append(string.Format("{0} {1}", "INSERT INTO", QueryTableName));
                    commandText.Append("(");
                    commandText.Append(string.Format("{0}, ", UbjectMetadata.UBJECT_HASH));
                    commandText.Append(string.Format("{0}, ", UbjectMetadata.UBJECT_TIMESTAMP));
                    commandText.Append(string.Join(", ", propertiesAndValues.Keys.ToList().ConvertAll(x => x).ToList()));
                    commandText.Append(") VALUES (");
                    commandText.Append(string.Format("'{0}', ", md5Hash));
                    commandText.Append(string.Format("'{0}', ", DateTime.UtcNow.Ticks.ToString()));
                    commandText.Append(string.Join(", ", propertiesAndParameters.Values.ToList()));
                    commandText.Append(");");
                    commandText.Append(string.Format("{0}{1}{2};", "SELECT IDENT_CURRENT('", QueryTableName, "')"));

                    var dbQueryExecutor = new DbQueryExecutor(dbManager);
                    object data = dbQueryExecutor.ExecuteScalar(commandText, parameters);

                    if (data != null)
                    {
                        primaryKey = Convert.ToInt64(data);
                    }
                }                
            }
            catch (Exception ex)
            {
            }

            return (primaryKey);
        }

        public override void UpdateObject(Dictionary<string, object> propertiesAndValues, long primaryKey)
        {
            StringBuilder commandText = new StringBuilder();

            try
            {
                if (propertiesAndValues.Count > 0)
                {
                    var parameters = CreateParameters(propertiesAndValues);
                    var propertiesAndParameters = propertiesAndValues.Keys.Zip(parameters.Keys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

                    string md5Hash = Utilities.MD5HashFromList(propertiesAndValues.Values.ToList());

                    commandText.Append(string.Format("{0} {1} {2} ", "UPDATE", QueryTableName, "SET"));
                    commandText.Append(string.Format("{0} = '{1}', ", UbjectMetadata.UBJECT_HASH, md5Hash));
                    commandText.Append(string.Format("{0} = '{1}', ", UbjectMetadata.UBJECT_TIMESTAMP, DateTime.UtcNow.Ticks.ToString()));
                    commandText.Append(string.Join(", ", propertiesAndParameters.Select(x => x.Key + " = " + x.Value).ToList()));
                    commandText.Append(string.Format(" {0} {1} = '{2}';", "WHERE", UbjectMetadata.PRIMARY_KEY, primaryKey));

                    var dbQueryExecutor = new DbQueryExecutor(dbManager);
                    dbQueryExecutor.ExecuteNonQuery(commandText, parameters);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
