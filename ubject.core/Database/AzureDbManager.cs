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
using Ninject.Parameters;

namespace Ubject.Core.Database
{
    public class AzureDbManager : BaseDbManager
    {
        private string ConnectionStringNoDb
        {
            get
            {
                //return ("Server=localhost\\SQLEXPRESS;Trusted_Connection=True;");
                return ("Server=tcp:diaryofanengineer.database.windows.net,1433;Persist Security Info=False;User ID=leighgawne;Password=Sarah9096!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            }
        }

        public override string ConnectionString
        {
            get
            {
                //return string.Format("Server=localhost\\SQLEXPRESS;Database={0};Trusted_Connection=True;", SchemaName);
                return string.Format("Server=tcp:diaryofanengineer.database.windows.net,1433;Initial Catalog={0};Persist Security Info=False;User ID=leighgawne;Password=Sarah9096!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;", SchemaName);
            }
        }

        public override string ShowTablesCommand
        {
            get
            {
                return ("SELECT name FROM sys.tables;");
            }
        }

        public override string ColumnCommandFieldName
        {
            get
            {
                return ("column_name");
            }
        }

        public override string ShowColumnsCommand(string tableName)
        {
            return (string.Format("SELECT column_name FROM {0}.information_schema.columns WHERE table_name = '{1}'", SchemaName, tableName));
        }

        public override void InitDatabaseSchema()
        {
            using (var connection = UbjectDIBindings.Resolve<IDbConnection>(new ConstructorArgument[] { new ConstructorArgument("connectionString", ConnectionStringNoDb, false) }))
            {
                StringBuilder commandText = new StringBuilder();
                commandText.Append("IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '" + SchemaName + "')");
                commandText.Append("CREATE DATABASE " + SchemaName + ";");
                connection.Open();
                IDbCommand command = connection.CreateCommand();
                command.CommandText = commandText.ToString();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        public override IDbTableManager CreateDatabaseTable(string tableName, List<PropertyInfo> properties)
        {
            IDbTableManager databaseTable = Utilities.CreateDbTableManager(this);
            databaseTable.TableName = (string.IsNullOrEmpty(tableName) ? Guid.NewGuid().ToString().ToUpper() : tableName);

            StringBuilder commandText = new StringBuilder();
            commandText.Append(string.Format("{0} {1}.dbo.\"{2}\"", "CREATE TABLE ", SchemaName, databaseTable.TableName));
            commandText.Append("(");
            commandText.Append(string.Format("{0} {1} {2} {3} {4}, ", UbjectMetadata.PRIMARY_KEY, "INT", "PRIMARY KEY", "IDENTITY", "(1, 1)").Trim());
            commandText.Append(string.Format("{0} {1} {2} {3} {4}, ", UbjectMetadata.UBJECT_HASH, "VARCHAR(100)", "", "NOT NULL", "").Trim());
            commandText.Append(string.Format("{0} {1} {2} {3} {4}, ", UbjectMetadata.UBJECT_TIMESTAMP, "BIGINT", "", "NOT NULL", "").Trim());

            foreach (PropertyInfo property in properties)
            {
                if ((property.PropertyType == typeof(int)) ||
                    (property.PropertyType == typeof(int?)))
                {
                    commandText.Append(string.Format("{0} {1} {2}, ", property.Name.ToUpper(), "INT", "DEFAULT(null)").Trim());
                }
                else if ((property.PropertyType == typeof(long)) ||
                        (property.PropertyType == typeof(long?)))
                {
                    commandText.Append(string.Format("{0} {1} {2}, ", property.Name.ToUpper(), "BIGINT", "DEFAULT(null)").Trim());
                }
                else if ((property.PropertyType == typeof(float)) ||
                        (property.PropertyType == typeof(float?)))
                {
                    commandText.Append(string.Format("{0} {1} {2}, ", property.Name.ToUpper(), "FLOAT", "DEFAULT(null)").Trim());
                }
                else
                {
                    commandText.Append(string.Format("{0} {1} {2}, ", property.Name.ToUpper(), "VARCHAR(" + VarCharFieldSize + ")", "DEFAULT(null)").Trim());
                }
                databaseTable.Columns.Add(property.Name.ToUpper());
            }

            List<PropertyInfo> indexedProperties = properties.GetIndexedProperties();

            foreach (PropertyInfo property in indexedProperties)
            {
                //commandText.Append(string.Format("{0} `IDX_{1}` (`{2}` ASC), ", "INDEX", property.Name.ToUpper(), property.Name.ToUpper()));
            }

            //commandText.Append(string.Format("{0} `IDX_{1}` (`{2}` ASC) ", "INDEX", UbjectMetadata.UBJECT_TIMESTAMP, UbjectMetadata.UBJECT_TIMESTAMP));
            commandText.Append(")");

            dbQueryExecutor.ExecuteNonQuery(commandText);

            return (databaseTable);
        }
    }
}
