using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ubject.Core.Database
{
    public class SqLiteDbTableManager : BaseDbTableManager, IDbTableManager
    {
        private List<string> columns = new List<string>();
        private Dictionary<Tuple<string, string, string>, string> dataFieldEncapsultedParameters = new Dictionary<Tuple<string, string, string>, string>();

        public override string QueryTableName
        {
            get
            {
                return (string.Format("`{0}`", TableName));
            }
        }

        public override long AddObject(Dictionary<string, object> propertiesAndValues)
        {
            long primaryKey = 0;

            if (propertiesAndValues.Count > 0)
            {
                var dbQueryExecutor = new DbQueryExecutor(dbManager);
                var parameters = CreateParameters(propertiesAndValues);
                var propertiesAndParameters = propertiesAndValues.Keys.Zip(parameters.Keys, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

                string md5Hash = Utilities.MD5HashFromList(propertiesAndValues.Values.ToList());

                StringBuilder commandText = new StringBuilder();
                commandText.Append(string.Format("{0} '{1}'", "INSERT INTO", TableName));
                commandText.Append("(");
                commandText.Append(string.Format("{0}, ", UbjectMetadata.UBJECT_HASH));
                commandText.Append(string.Format("{0}, ", UbjectMetadata.UBJECT_TIMESTAMP));
                commandText.Append(string.Join(", ", propertiesAndValues.Keys.ToList().ConvertAll(x => x.ToUpper()).ToList()));
                commandText.Append(") VALUES (");
                commandText.Append(string.Format("'{0}', ", md5Hash));
                commandText.Append(string.Format("'{0}', ", DateTime.UtcNow.Ticks.ToString()));
                commandText.Append(string.Join(", ", propertiesAndParameters.Values.ToList()));
                commandText.Append(");");

                dbQueryExecutor.ExecuteNonQuery(commandText, parameters);

                commandText.Clear();
                commandText.Append(string.Format("SELECT SEQ FROM SQLITE_SEQUENCE WHERE NAME = '{0}';", TableName));

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
                commandText.Append(string.Format("{0} '{1}' {2} ", "UPDATE", TableName, "SET"));
                commandText.Append(string.Format("{0} = '{1}', ", UbjectMetadata.UBJECT_HASH, md5Hash));
                commandText.Append(string.Format("{0} = '{1}', ", UbjectMetadata.UBJECT_TIMESTAMP, DateTime.UtcNow.Ticks.ToString()));
                commandText.Append(string.Join(", ", propertiesAndParameters.Select(x => "`" + x.Key + "` = " + x.Value).ToList()));
                commandText.Append(string.Format(" {0} {1} = '{2}';", "WHERE", UbjectMetadata.PRIMARY_KEY, primaryKey));
                var dbQueryExecutor = new DbQueryExecutor(dbManager);
                dbQueryExecutor.ExecuteNonQuery(commandText, parameters);
            }
        }

        public override Dictionary<UbjectMetadata, object> GetObjects(Type type, List<Tuple<string, string, string>> filterProperties = null)
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

            foreach (var dataFieldEncapsultedParameter in dataFieldEncapsultedParameters)
            {
                command.Parameters.Add(new SQLiteParameter(dataFieldEncapsultedParameter.Value, dataFieldEncapsultedParameter.Key.Item2));
            }

            connection.Open();
            command.CommandText = commandText.ToString();

            IDataReader tableReader = command.ExecuteReader();

            Dictionary<UbjectMetadata, object> newObjects = new Dictionary<UbjectMetadata, object>();

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
                                        property.SetValue(newObject, Utilities.ChangeType(fieldValue, property.PropertyType));
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                newObjects.Add(ubjectMetadata, newObject);
            }

            connection.Close();

            return (newObjects);
        }

        protected override string BuildWhereClauses(PropertyInfo[] propertyInfo, List<Tuple<string, string, string>> filterConditions)
        {
            List<string> whereClauses = new List<string>();
            List<Tuple<string, string, string>> processedFilterConditions = new List<Tuple<string, string, string>>();

            if (filterConditions != null)
            {
                dataFieldEncapsultedParameters.Clear();
                int parameterCount = 1;

                foreach (Tuple<string, string, string> filterCondition in filterConditions)
                {
                    dataFieldEncapsultedParameters.Add(filterCondition, "@param" + (parameterCount++).ToString());
                }

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
                            whereClauses.Add(
                                string.Format(
                                    "{0} {1} {2}", 
                                    property.Name, 
                                    filterCondition.Item3, 
                                    dataFieldEncapsultedParameters[filterCondition]));
                        }
                    }
                }

                var remainingFilterConditions = filterConditions.Except(processedFilterConditions).ToList();

                foreach (Tuple<string, string, string> remainingFilterCondition in remainingFilterConditions)
                {
                    if (UbjectMetadata.IsMetadataField(remainingFilterCondition.Item1))
                    {
                        whereClauses.Add(
                            string.Format(
                                "{0} {1} {2}", 
                                remainingFilterCondition.Item1, 
                                remainingFilterCondition.Item3, 
                                dataFieldEncapsultedParameters[remainingFilterCondition]));
                    }
                }
            }

            return (string.Join(" AND ", whereClauses).Trim());
        }
    }
}
