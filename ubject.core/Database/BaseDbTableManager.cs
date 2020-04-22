using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core.Database
{
    public abstract class BaseDbTableManager : IDbTableManager
    {
        protected IDbManager dbManager;

        private List<string> columns = new List<string>();

        public string TableName { get; set; }

        public virtual string QueryTableName
        {
            get
            {
                return (string.Format("`{0}`.`{1}`", dbManager.SchemaName, TableName));
            }
        }

        public List<string> Columns
        {
            get
            {
                return (columns);
            }
        }

        public IDbManager DbManager
        {
            get
            {
                return dbManager;
            }

            set
            {
                dbManager = value;
            }
        }

        public BaseDbTableManager()
        {
        }

        public virtual long AddObject(Dictionary<string, object> propertiesAndValues)
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateObject(Dictionary<string, object> propertiesAndValues, long primaryKey)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteAllObjects()
        {
            StringBuilder commandText = new StringBuilder();
            commandText.Append(string.Format("{0} {1};", "DELETE FROM", QueryTableName));

            var dbQueryExecutor = new DbQueryExecutor(dbManager);
            dbQueryExecutor.ExecuteNonQuery(commandText);
        }

        public virtual void DeleteObject(UbjectMetadata ubjectMetadata)
        {
            DeleteObjectFromTable(BuildWhereClauses(null, null, null, ubjectMetadata));
        }

        public virtual void DeleteObject(object data, List<string> filterProperties)
        {
            DeleteObjectFromTable(BuildWhereClauses(data, data.GetType().GetPropertiesEx(), filterProperties, null));
        }

        public virtual void DeleteObjectFromTable(string whereClause)
        {
            StringBuilder commandText = new StringBuilder();
            commandText.Append(string.Format("{0} {1} ", "DELETE FROM", QueryTableName));
            commandText.Append(string.Format("{0} {1} {2}", "WHERE", whereClause, "LIMIT 1"));
            commandText.Append(";");

            var dbQueryExecutor = new DbQueryExecutor(dbManager);
            dbQueryExecutor.ExecuteNonQuery(commandText);
        }

        public virtual Dictionary<UbjectMetadata, object> GetObjects(Type type, List<Tuple<string, string, string>> filterProperties = null)
        {
            var connection = Utilities.CreateDbConnection(dbManager);
            IDbCommand command = connection.CreateCommand();
            StringBuilder commandText = new StringBuilder();

            var propertyInfo = type.GetPropertiesEx();

            commandText.Append(string.Format("{0} {1} {2} {3}", "SELECT", "*", "FROM", QueryTableName));

            string whereClause = BuildWhereClauses(type.GetPropertiesEx(), filterProperties);

            if (whereClause != string.Empty)
            {
                commandText.Append(string.Format(" {0} {1}", "WHERE", whereClause));
            }

            commandText.Append(";");
            connection.Open();

            Dictionary<UbjectMetadata, object> newObjects = new Dictionary<UbjectMetadata, object>();

            try
            {
                command.CommandText = commandText.ToString();
                IDataReader tableReader = command.ExecuteReader();

                while (tableReader.Read())
                {
                    object newObject = null;

                    if (type.IsInterface)
                    {
                        newObject = UbjectDIBindings.Resolve(type);
                    }
                    else
                    {
                        newObject = Activator.CreateInstance(type);
                    }

                    var ubjectMetadata = new UbjectMetadata();

                    for (int fieldIndex = 0; fieldIndex < tableReader.FieldCount; fieldIndex++)
                    {
                        string fieldName = tableReader.GetName(fieldIndex).ToString();
                        object fieldValue = tableReader.GetValue(fieldIndex);

                        if (UbjectMetadata.IsMetadataField(fieldName))
                        {
                            ubjectMetadata.SetPropertyValue(fieldName, fieldValue);
                        }
                        else
                        {
                            foreach (var property in propertyInfo)
                            {
                                if (fieldName.ToLower() == property.Name.ToLower())
                                {
                                    if ((fieldValue != DBNull.Value) && (fieldValue != null))
                                    {
                                        if (property.PropertyType.IsEnum)
                                        {
                                            property.SetValue(newObject, Enum.Parse(property.PropertyType, fieldValue.ToString()));
                                        }
                                        else
                                        {
                                            property.SetValue(newObject, Convert.ChangeType(fieldValue, property.PropertyType));
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    newObjects.Add(ubjectMetadata, newObject);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                connection.Close();
            }

            return (newObjects);
        }

        protected virtual string BuildWhereClauses(object data, PropertyInfo[] propertyInfo, List<string> filterProperties, UbjectMetadata ubjectMetadata)
        {
            List<string> whereClauses = new List<string>();

            if ((data != null) && (propertyInfo != null) && (filterProperties != null))
            {
                foreach (var property in propertyInfo)
                {
                    bool filterOnProperty = true;

                    if (!filterProperties.ConvertAll(x => x.ToLower()).Contains(property.Name.ToLower()))
                    {
                        filterOnProperty = false;
                    }

                    if (filterOnProperty)
                    {
                        if (property.GetValue(data).ToStringEx() != string.Empty)
                        {
                            whereClauses.Add(property.Name + " = '" + property.GetValue(data) + "'");
                        }
                    }
                }
            }

            if (ubjectMetadata != null)
            {
                whereClauses.Add(UbjectMetadata.PRIMARY_KEY + " = '" + ubjectMetadata.PrimaryKey + "'");
            }

            return (string.Join(" AND ", whereClauses).Trim());
        }

        protected virtual string BuildWhereClauses(PropertyInfo[] propertyInfo, List<Tuple<string, string, string>> filterConditions)
        {
            List<string> whereClauses = new List<string>();
            List<Tuple<string, string, string>> processedFilterConditions = new List<Tuple<string, string, string>>();

            if (filterConditions != null)
            {
                foreach (var property in propertyInfo)
                {
                    bool filterOnProperty = false;

                    if (filterConditions.Select(x => x.Item1).ToList().ConvertAll(x => x.ToLower()).Contains(property.Name.ToLower()))
                    {
                        filterOnProperty = true;
                    }

                    if (filterOnProperty)
                    {
                        Tuple<string, string, string> filterCondition = filterConditions.Where(x => x.Item1.ToLower() == property.Name.ToLower()).First();

                        if (filterCondition.Item2 != string.Empty)
                        {
                            processedFilterConditions.Add(filterCondition);
                            whereClauses.Add(property.Name + " " + filterCondition.Item3 + " '" + EscapeQueryArgument(filterCondition.Item2) + "'");
                        }
                    }
                }

                List<Tuple<string, string, string>> remainingFilterConditions = filterConditions.Except(processedFilterConditions).ToList();

                foreach (Tuple<string, string, string> remainingFilterCondition in remainingFilterConditions)
                {
                    if (UbjectMetadata.IsMetadataField(remainingFilterCondition.Item1))
                    {
                        whereClauses.Add(remainingFilterCondition.Item1 + " " + remainingFilterCondition.Item3 + " '" + EscapeQueryArgument(remainingFilterCondition.Item2) + "'");
                    }
                }
            }

            return (string.Join(" AND ", whereClauses).Trim());
        }

        private string EscapeQueryArgument(string argument)
        {
            return argument.Replace("'", "\\'").Replace("\"", "\\\"");
        }

        protected Dictionary<string, object> CreateParameters(Dictionary<string, object> propertiesAndValues)
        {
            var parameters = new Dictionary<string, object>();
            int parameterCount = 0;

            foreach (KeyValuePair<string, object> propertyAndValue in propertiesAndValues)
            {
                try
                {
                    if (propertyAndValue.Value == null)
                    {
                        parameters.Add(string.Format("@param{0}", parameterCount++), DBNull.Value);
                    }
                    else
                    {
                        var valueType = propertyAndValue.Value.GetType();

                        if ((valueType == typeof(int)) ||
                            (valueType == typeof(long)) ||
                            (valueType == typeof(float)) ||
                            (valueType == typeof(double)) ||
                            (valueType == typeof(bool)) ||
                            (valueType == typeof(int?)) ||
                            (valueType == typeof(long?)) ||
                            (valueType == typeof(float?)) ||
                            (valueType == typeof(double?)) ||
                            (valueType == typeof(bool?)))
                        {
                            parameters.Add(string.Format("@param{0}", parameterCount++), propertyAndValue.Value);
                        }
                        else
                        {
                            string value = propertyAndValue.Value.ToString();
                            parameters.Add(string.Format("@param{0}", parameterCount++), value.Substring(0, Math.Min(dbManager.VarCharFieldSize, value.Length)));
                        }
                    }
                }
                catch (Exception ex)
                {
                }

            }

            return (parameters);
        }

        public virtual void DeleteTable()
        {
            throw new NotImplementedException();
        }

        public virtual void CloneTable(string newTableName = null)
        {
            throw new NotImplementedException();
        }

        public virtual void RestoreBackup(string newTableName = null)
        {
            throw new NotImplementedException();
        }

        public virtual void AddTrustMode()
        {
            throw new NotImplementedException();
        }
    }
}
