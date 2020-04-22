using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Ubject.Core;

namespace Ubject.Core.Database
{
    public abstract class BaseDbManager : IDbManager
    {
        protected readonly DbQueryExecutor dbQueryExecutor;

        private string connectionString = string.Empty;
        private string schemaName = "ubject";

        public virtual string ConnectionString
        {
            get
            {
                return (connectionString);
            }
        }

        public virtual string CustomConnectionString { get; set; } = string.Empty;

        public string SchemaName
        {
            get
            {
                return (schemaName);
            }
            set
            {
                schemaName = value;
            }
        }

        public int VarCharFieldSize
        {
            get
            {
                return (1000);
            }
        }

        public virtual string ShowTablesCommand
        {
            get
            {
                return (string.Empty);
            }
        }

        public virtual string ColumnCommandFieldName
        {
            get
            {
                return (string.Empty);
            }
        }

        public virtual string ShowColumnsCommand(string tableName)
        {
            return (string.Empty);
        }


        public BaseDbManager()
        {
            dbQueryExecutor = new DbQueryExecutor(this);
        }

        public virtual void InitDatabaseSchema()
        {
            throw new NotImplementedException();
        }

        public virtual List<IDbTableManager> GetAllDbTableManagers()
        {
            List<IDbTableManager> dbTableManagers = new List<IDbTableManager>();

            using (var tableConnection = Utilities.CreateDbConnection(this))
            {
                IDbCommand showTableCommand = tableConnection.CreateCommand();
                showTableCommand.CommandText = ShowTablesCommand;
                tableConnection.Open();

                IDataReader tableReader = showTableCommand.ExecuteReader();

                using (var columnsConnection = Utilities.CreateDbConnection(this))
                {
                    columnsConnection.Open();

                    while (tableReader.Read())
                    {
                        for (int tableFieldIndex = 0; tableFieldIndex < 1 /*tableReader.FieldCount*/; tableFieldIndex++)
                        {
                            IDbTableManager dbTableManager = Utilities.CreateDbTableManager(this);
                            dbTableManager.TableName = tableReader.GetValue(tableFieldIndex).ToString();

                            IDbCommand showColumnsCommand = columnsConnection.CreateCommand();
                            showColumnsCommand.CommandText = ShowColumnsCommand(dbTableManager.TableName);

                            IDataReader schemaReader = showColumnsCommand.ExecuteReader();

                            while (schemaReader.Read())
                            {
                                for (int schemaFieldIndex = 0; schemaFieldIndex < schemaReader.FieldCount; schemaFieldIndex++)
                                {
                                    string fieldName = schemaReader.GetName(schemaFieldIndex).ToString();

                                    if (fieldName == ColumnCommandFieldName)
                                    {
                                        string columnName = schemaReader.GetValue(schemaFieldIndex).ToString();

                                        if (!UbjectMetadata.IsMetadataField(columnName))
                                        {
                                            dbTableManager.Columns.Add(columnName);
                                        }

                                        break;
                                    }
                                }
                            }

                            schemaReader.Close();
                            dbTableManagers.Add(dbTableManager);
                        }
                    }

                    columnsConnection.Close();
                }

                tableReader.Close();
                tableConnection.Close();
            }

            return (dbTableManagers);
        }

        public virtual IDbTableManager CreateDatabaseTable(string tableName, List<PropertyInfo> properties)
        {
            throw new NotImplementedException();
        }

        public IDbTableManager GetDbTableManagerForObject(Type type, bool createTable = true)
        {
            List<PropertyInfo> properties = GetProperties(type);
            List<IDbTableManager> databaseTables = GetAllDbTableManagers();

            foreach (IDbTableManager databaseTable in databaseTables)
            {
                List<string> databaseColumns = databaseTable.Columns.Except(new List<string>() { UbjectMetadata.PRIMARY_KEY, UbjectMetadata.UBJECT_HASH, UbjectMetadata.UBJECT_TIMESTAMP }).ToList();

                if (databaseColumns.Count == properties.Count)
                {
                    if ((databaseColumns.ConvertAll(x => x.ToLower()).Except(properties.ConvertAll(x => x.Name.ToLower()))).ToList().Count == 0)
                    {
                        return (databaseTable);
                    }
                }
            }

            if (createTable)
            {
                // No database table matches for the object, create a new one
                return (CreateDatabaseTable(type.GetTableName(), properties));
            }

            return (null);
        }

        protected List<PropertyInfo> GetProperties(Type type)
        {
            return (type.GetPropertiesEx().ToList());
        }
    }
}
