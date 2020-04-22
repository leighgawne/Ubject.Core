using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ubject.Core.Database
{
    public class MySqlDbTableManager : BaseDbTableManager
    {
        private List<string> columns = new List<string>();

        public override long AddObject(Dictionary<string, object> propertiesAndValues)
        {
            long primaryKey = 0;

            if (propertiesAndValues.Count > 0)
            {
                var parameters = CreateParameters(propertiesAndValues);
                var propertiesAndParameters = propertiesAndValues.Keys.Zip(parameters.Keys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

                string md5Hash = Utilities.MD5HashFromList(propertiesAndValues.Values.ToList());

                StringBuilder commandText = new StringBuilder();
                commandText.Append(string.Format("{0} {1}", "INSERT INTO", QueryTableName));
                commandText.Append("(");
                commandText.Append(string.Format("`{0}`, ", UbjectMetadata.UBJECT_HASH));
                commandText.Append(string.Format("`{0}`, ", UbjectMetadata.UBJECT_TIMESTAMP));
                commandText.Append(string.Join(", ", propertiesAndValues.Keys.ToList().ConvertAll(x => "`" + x + "`").ToList()));
                commandText.Append(") VALUES (");
                commandText.Append(string.Format("'{0}', ", md5Hash));
                commandText.Append(string.Format("'{0}', ", DateTime.UtcNow.Ticks.ToString()));
                commandText.Append(string.Join(", ", propertiesAndParameters.Values.ToList()));
                commandText.Append(");");
                commandText.Append(string.Format("{0} {1};", "SELECT", "LAST_INSERT_ID()"));

                var dbQueryExecutor = new DbQueryExecutor(dbManager);
                object data = dbQueryExecutor.ExecuteScalar(commandText, parameters);

                if (data != null)
                {
                    primaryKey = Convert.ToInt64(data);
                }
            }

            return (primaryKey);
        }

        public override void UpdateObject(Dictionary<string, object> propertiesAndValues, long primaryKey)
        {
            if (propertiesAndValues.Count > 0)
            {
                var parameters = CreateParameters(propertiesAndValues);
                var propertiesAndParameters = propertiesAndValues.Keys.Zip(parameters.Keys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

                string md5Hash = Utilities.MD5HashFromList(propertiesAndValues.Values.ToList());

                StringBuilder commandText = new StringBuilder();
                commandText.Append(string.Format("{0} {1} {2}", "UPDATE", QueryTableName, "SET"));
                commandText.Append(string.Format("`{0}` = '{1}', ", UbjectMetadata.UBJECT_HASH, md5Hash));
                commandText.Append(string.Format("`{0}` = '{1}', ", UbjectMetadata.UBJECT_TIMESTAMP, DateTime.UtcNow.Ticks.ToString()));
                commandText.Append(string.Join(", ", propertiesAndParameters.Select(x => "`" + x.Key + "` = " + x.Value).ToList()));
                commandText.Append(string.Format(" {0} `{1}` = '{2}';", "WHERE", UbjectMetadata.PRIMARY_KEY, primaryKey));

                var dbQueryExecutor = new DbQueryExecutor(dbManager);
                dbQueryExecutor.ExecuteNonQuery(commandText, parameters);
            }
        }

        private readonly string backupSuffix = "_backup";

        public override void RestoreBackup(string newTableName = null)
        {
            if (!TableName.Contains(backupSuffix))
            {
                throw new Exception("Table is not backup table!");
            }

            string originalTableName = TableName.Substring(0, TableName.Length - backupSuffix.Length);

            StringBuilder commandText = new StringBuilder();

            commandText.Append(string.Format("DROP TABLE IF EXISTS {0};", originalTableName));
            commandText.Append(string.Format("CREATE TABLE {0} LIKE {1};", originalTableName, TableName));
            commandText.Append(string.Format("INSERT {0} SELECT * FROM {1};", originalTableName, TableName));

            var dbQueryExecutor = new DbQueryExecutor(dbManager);
            dbQueryExecutor.ExecuteNonQuery(commandText);
        }

        public override void DeleteTable()
        {
            StringBuilder commandText = new StringBuilder();

            commandText.Append(string.Format("DROP TABLE IF EXISTS {0};", TableName));

            var dbQueryExecutor = new DbQueryExecutor(dbManager);
            dbQueryExecutor.ExecuteNonQuery(commandText);
        }

        public override void CloneTable(string newTableName = null)
        {
            if (string.IsNullOrEmpty(newTableName))
            {
                newTableName = string.Format("{0}_backup", TableName);
            }

            StringBuilder commandText = new StringBuilder();

            commandText.Append(string.Format("DROP TABLE IF EXISTS {0};", newTableName));
            commandText.Append(string.Format("CREATE TABLE {0} LIKE {1};", newTableName, TableName));
            commandText.Append(string.Format("INSERT {0} SELECT * FROM {1};", newTableName, TableName));

            var dbQueryExecutor = new DbQueryExecutor(dbManager);
            dbQueryExecutor.ExecuteNonQuery(commandText);
        }

        public override void AddTrustMode()
        {
            StringBuilder commandText = new StringBuilder();

            commandText.Append(string.Format("ALTER TABLE `{0}` ", TableName));
            commandText.Append(string.Format("ADD COLUMN `TRUSTMODE` INT NOT NULL DEFAULT 0;"));

            var dbQueryExecutor = new DbQueryExecutor(dbManager);
            dbQueryExecutor.ExecuteNonQuery(commandText);

            commandText.Clear();
            commandText.Append(string.Format("ALTER TABLE `{0}` ", TableName));
            commandText.Append("ADD INDEX `TRUSTMODE` (`TRUSTMODE` ASC);");

            dbQueryExecutor.ExecuteNonQuery(commandText);
        }
    }
}
