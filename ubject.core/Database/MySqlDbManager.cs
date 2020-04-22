using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

using MySql.Data.MySqlClient;

using Ninject.Parameters;

namespace Ubject.Core.Database
{
    public class MySqlDbManager : BaseDbManager
    {
        private string ConnectionStringNoDb
        {
            get
            {
                return ("SERVER=localhost;UID=root;PASSWORD=root");
            }
        }

        public override string ConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomConnectionString))
                {
                    return (CustomConnectionString);
                }

                return (string.Format("SERVER=localhost;DATABASE={0};UID=root;PASSWORD=root", SchemaName));
            }
        }

        public override string ColumnCommandFieldName
        {
            get
            {
                return ("Field");
            }
        }

        public override string ShowTablesCommand
        {
            get
            {
                return ("SHOW FULL TABLES WHERE Table_Type = 'BASE TABLE';");
                //return ("SHOW TABLES;");
            }
        }

        public override string ShowColumnsCommand(string tableName)
        {
            return (string.Format("SHOW COLUMNS FROM `{0}`.`{1}`;", SchemaName, tableName));
        }

        public override void InitDatabaseSchema()
        {
            using (var connection = UbjectDIBindings.Resolve<IDbConnection>(new ConstructorArgument[] { new ConstructorArgument("connectionString", ConnectionStringNoDb, false) }))
            {
                StringBuilder commandText = new StringBuilder();
                commandText.Append("CREATE DATABASE IF NOT EXISTS `" + SchemaName + "`;");
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
            commandText.Append(string.Format("{0} `{1}`.`{2}`", "CREATE TABLE ", SchemaName, databaseTable.TableName));
            commandText.Append("(");
            commandText.Append(string.Format("`{0}` {1} {2} {3} {4}, ", UbjectMetadata.PRIMARY_KEY, "INT(11)", "UNIQUE", "NOT NULL", "AUTO_INCREMENT").Trim());
            commandText.Append(string.Format("`{0}` {1} {2} {3} {4}, ", UbjectMetadata.UBJECT_HASH, "VARCHAR(100)", "", "NOT NULL", "").Trim());
            commandText.Append(string.Format("`{0}` {1} {2} {3} {4}, ", UbjectMetadata.UBJECT_TIMESTAMP, "BIGINT", "", "NOT NULL", "").Trim());

            foreach (PropertyInfo property in properties)
            {
                if ((property.PropertyType == typeof(int)) ||
                    (property.PropertyType == typeof(int?)))
                {
                    commandText.Append(string.Format("`{0}` {1} {2}, ", property.Name.ToUpper(), "INT", "DEFAULT NULL").Trim());
                }
                else if ((property.PropertyType == typeof(long)) ||
                        (property.PropertyType == typeof(long?)))
                {
                    commandText.Append(string.Format("`{0}` {1} {2}, ", property.Name.ToUpper(), "BIGINT", "DEFAULT NULL").Trim());
                }
                else if ((property.PropertyType == typeof(float)) ||
                        (property.PropertyType == typeof(float?)))
                {
                    commandText.Append(string.Format("`{0}` {1} {2}, ", property.Name.ToUpper(), "FLOAT", "DEFAULT NULL").Trim());
                }
                else
                {
                    commandText.Append(string.Format("`{0}` {1} {2}, ", property.Name.ToUpper(), "VARCHAR(" + VarCharFieldSize + ")", "DEFAULT NULL").Trim());
                }
                databaseTable.Columns.Add(property.Name.ToUpper());
            }

            commandText.Append(string.Format("{0}(`{1}`), ", "PRIMARY KEY", UbjectMetadata.PRIMARY_KEY));

            List<PropertyInfo> indexedProperties = properties.GetIndexedProperties();

            foreach (PropertyInfo property in indexedProperties)
            {
                commandText.Append(string.Format("{0} `IDX_{1}` (`{2}` ASC), ", "INDEX", property.Name.ToUpper(), property.Name.ToUpper()));
            }

            commandText.Append(string.Format("{0} `IDX_{1}` (`{2}` ASC) ", "INDEX", UbjectMetadata.UBJECT_TIMESTAMP, UbjectMetadata.UBJECT_TIMESTAMP));
            commandText.Append(")");
            commandText.Append("ENGINE = InnoDB DEFAULT CHARSET = utf8;");

            dbQueryExecutor.ExecuteNonQuery(commandText);

            return (databaseTable);
        }
    }
}
