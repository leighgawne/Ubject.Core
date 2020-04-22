using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;

namespace Ubject.Core.Database
{
    public class SqLiteDbManager : BaseDbManager
    {
        private string DbFile
        {
            get
            {
                return (Path.Combine(DbConfigHelper.LocalDBFolder, SchemaName + ".db"));
            }
        }

        public override string ConnectionString
        {
            get
            {
                return string.Format("Data Source={0};Version=3;Pooling=False", DbFile);
            }
        }

        public override string ShowTablesCommand
        {
            get
            {
                return ("SELECT name FROM sqlite_master WHERE type = \"table\";");
            }
        }

        public override string ColumnCommandFieldName
        {
            get
            {
                return ("name");
            }
        }

        public override string ShowColumnsCommand(string tableName)
        {
            return (string.Format("PRAGMA table_info('{0}');", tableName));
        }

        public override void InitDatabaseSchema()
        {
            if (!File.Exists(DbFile))
            {
                SQLiteConnection.CreateFile(DbFile);
            }
        }

        public override IDbTableManager CreateDatabaseTable(string tableName, List<PropertyInfo> properties)
        {
            IDbTableManager databaseTable = Utilities.CreateDbTableManager(this);
            databaseTable.TableName = (string.IsNullOrEmpty(tableName) ? Guid.NewGuid().ToString().ToUpper() : tableName);

            StringBuilder commandText = new StringBuilder();
            commandText.Append(string.Format("{0} '{1}'", "CREATE TABLE", databaseTable.TableName));
            commandText.Append("(");
            commandText.Append(string.Format("{0} {1} {2} {3},", UbjectMetadata.PRIMARY_KEY, "INTEGER", "PRIMARY KEY", "AUTOINCREMENT").Trim());
            commandText.Append(string.Format("{0} {1} {2} {3},", UbjectMetadata.UBJECT_HASH, "CHAR(100)", "", "NOT NULL").Trim());
            commandText.Append(string.Format("{0} {1} {2} {3},", UbjectMetadata.UBJECT_TIMESTAMP, "BIGINT", "", "NOT NULL").Trim());
            commandText.Append(string.Join(", ", properties.Select(x => string.Format("{0} {1}", x.Name.ToUpper(), "TEXT"))).Trim());
            properties.ForEach(x => databaseTable.Columns.Add(x.Name.ToUpper()));
            commandText.Append(");");
            dbQueryExecutor.ExecuteNonQuery(commandText);

            commandText.Clear();
            commandText.Append(
                string.Format(
                    "CREATE INDEX IDX_{0}_{1} ON '{2}' ({3});", 
                    UbjectMetadata.UBJECT_TIMESTAMP,
                    databaseTable.TableName.Replace("-", ""),
                    databaseTable.TableName,
                    UbjectMetadata.UBJECT_TIMESTAMP));

            foreach (PropertyInfo property in properties.GetIndexedProperties())
            {
                commandText.Append(
                    string.Format(
                        "{0} IDX_{1}_{2} ON '{3}' ({4});",
                        "CREATE INDEX",
                        property.Name.ToUpper(),
                        databaseTable.TableName.Replace("-", ""),
                        databaseTable.TableName,
                        property.Name.ToUpper()));
            }

            dbQueryExecutor.ExecuteNonQuery(commandText);

            return (databaseTable);
        }
    }
}
