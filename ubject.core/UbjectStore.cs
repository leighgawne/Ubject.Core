using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Nest;

using Ubject.Core.Database;

namespace Ubject.Core
{
    public class UbjectStore
    {
        private const string parentTableKey = "ParentTableKey";
        private const string parentObjectKey = "ParentObjectKey";
        private const string childTableKey = "ChildTableKey";

        private string dbTenant;
        private IDbManager dbManager;

        public bool ExplicitInclude { get; set; } = false;

        private string CustomConnectionString { get; set; }

        private IDbManager DbManager
        {
            get
            {
                if (dbManager == null)
                {
                    dbManager = Utilities.CreateDbManager(dbTenant);
                    dbManager.CustomConnectionString = CustomConnectionString;
                    dbManager.InitDatabaseSchema();
                }

                return (dbManager);
            }
        }

        public UbjectStore()
            : this("ubject")
        {
        }

        public UbjectStore(string dbTenant)
        {
            this.dbTenant = dbTenant;
        }

        public UbjectStore(string dbTenant, string customConnectionString)
        {
            this.dbTenant = dbTenant;
            CustomConnectionString = customConnectionString;
        }

        public Tuple<string, string> PersistObject(object data, List<string> filterProperties = null)
        {
            string primaryKey = string.Empty;

            IDbTableManager databaseTable = DbManager.GetDbTableManagerForObject(data.GetType());

            if (databaseTable != null)
            {
                bool objectUpdated = false;
                var objectAnatomyDefinition = GetObjectAnatomyDefinition(data);

                if (filterProperties != null)
                {
                    var objects = databaseTable.GetObjects(data.GetType(), CreateCompleteFilterFromPropertyAndData(data, filterProperties));

                    if (objects.Count == 1)
                    {
                        databaseTable.UpdateObject(objectAnatomyDefinition.PropertiesAndValues, objects.Keys.First().PrimaryKey);
                        objectUpdated = true;
                    }
                    else if (objects.Count != 0)
                    {
                        throw new Exception(string.Format("Multiple existing objects found while attempting to persist object. Filter: {0} Number of objects returned: {1}", 
                                                          string.Join(", ", filterProperties), 
                                                          objects.Count));
                    }
                }

                if (!objectUpdated)
                {
                    primaryKey = databaseTable.AddObject(objectAnatomyDefinition.PropertiesAndValues).ToString();
                }

                foreach (KeyValuePair<string, ICollection> collectionObject in objectAnatomyDefinition.CollectionReferenceTypeObjects)
                {
                    foreach (var collectionItem in collectionObject.Value)
                    {
                        Tuple<string, string> childObjectKey = PersistObject(collectionItem);
                        PersistObject(new UbjectMapping(databaseTable.TableName, primaryKey, childObjectKey.Item1, childObjectKey.Item2));
                    }
                }
            }

            return (new Tuple<string, string>(databaseTable.TableName, primaryKey));
        }

        public void RemoveAllObjects<T>()
        {
            IDbTableManager databaseTable = DbManager.GetDbTableManagerForObject(typeof(T));

            if (databaseTable != null)
            {
                databaseTable.DeleteAllObjects();
            }
        }

        public void RemoveObject(object data, List<string> filterProperties)
        {
            Dictionary<UbjectMetadata, object> objects = new Dictionary<UbjectMetadata, object>();

            IDbTableManager databaseTable = DbManager.GetDbTableManagerForObject(data.GetType());

            if (databaseTable != null)
            {
                objects = databaseTable.GetObjects(data.GetType(), CreateCompleteFilterFromPropertyAndData(data, filterProperties));

                if (objects.Count > 0)
                {
                    var objectAnatomyDefinition = GetObjectAnatomyDefinition(objects.Values.First());

                    foreach (var collectionObject in objectAnatomyDefinition.CollectionReferenceTypeObjects)
                    {
                        IEnumerator enumerator = collectionObject.Value.GetEnumerator();
                        Type collectionItemType = enumerator.GetType().GetGenericArguments()[0];
                        IDbTableManager mappingDatabaseTable = DbManager.GetDbTableManagerForObject(typeof(UbjectMapping), false);
                        IDbTableManager childDatabaseTable = DbManager.GetDbTableManagerForObject(collectionItemType, false);

                        if ((mappingDatabaseTable != null) && (childDatabaseTable != null))
                        {
                            foreach (KeyValuePair<UbjectMetadata, object> ubject in objects)
                            {
                                List<Tuple<string, string, string>> mappingFilterCondition = new List<Tuple<string, string, string>>();

                                mappingFilterCondition.Add(new Tuple<string, string, string>(parentTableKey, databaseTable.TableName, "="));
                                mappingFilterCondition.Add(new Tuple<string, string, string>(parentObjectKey, ubject.Key.PrimaryKey.ToString(), "="));
                                mappingFilterCondition.Add(new Tuple<string, string, string>(childTableKey, childDatabaseTable.TableName, "="));

                                Dictionary<UbjectMetadata, UbjectMapping> objectMappings = GetObjectsIncMetadata<UbjectMapping>(mappingFilterCondition);

                                List<UbjectMetadata> childObjectsMetadata = new List<UbjectMetadata>();

                                foreach (var objectMapping in objectMappings.Values)
                                {
                                    List<Tuple<string, string, string>> childObjectFilterCondition = new List<Tuple<string, string, string>>();
                                    childObjectFilterCondition.Add(new Tuple<string, string, string>(UbjectMetadata.PRIMARY_KEY, objectMapping.ChildObjectKey, "="));
                                    List<UbjectMetadata> ubjectMetadata = new List<UbjectMetadata>(GetObjectsIncMetadata(collectionItemType, childObjectFilterCondition).Keys);
                                    childObjectsMetadata.AddRange(ubjectMetadata);
                                }

                                childObjectsMetadata.ForEach(x => childDatabaseTable.DeleteObject(x));
                                objectMappings.Keys.ToList().ForEach(x => mappingDatabaseTable.DeleteObject(x));
                            }
                        }
                    }

                    objects.Keys.ToList().ForEach(x => databaseTable.DeleteObject(x));
                }
            }
        }

        public List<Tuple<string, string, string>> CreateCompleteFilterFromPropertyAndData(object data, List<string> filterProperties)
        {
            List<Tuple<string, string, string>> completefilterProperties = new List<Tuple<string, string, string>>();

            var propertyInfo = data.GetType().GetPropertiesEx();

            foreach (var property in propertyInfo)
            {
                if (filterProperties.ConvertAll(x => x.ToLower()).Contains(property.Name.ToLower()))
                {
                    completefilterProperties.Add(new Tuple<string, string, string>(property.Name, property.GetValue(data).ToString(), "="));
                }
            }

            return (completefilterProperties);
        }

        public List<T> GetObjects<T>(List<Tuple<string, string, string>> filterProperties = null)
        {
            return (GetObjectsIncMetadata<T>(filterProperties).Values.ToList());
        }

        public Dictionary<UbjectMetadata, T> GetObjectsIncMetadata<T>(List<Tuple<string, string, string>> filterProperties = null)
        {
            return (GetObjectsIncMetadata(typeof(T), filterProperties).ToDictionary(x => x.Key, x => (T)x.Value));
        }

        public Dictionary<UbjectMetadata, object> GetObjectsIncMetadata(Type type, List<Tuple<string, string, string>> filterProperties = null)
        {
            Dictionary<UbjectMetadata, object> ubjects = new Dictionary<UbjectMetadata, object>();

            IDbTableManager databaseTable = DbManager.GetDbTableManagerForObject(type, false);

            if (databaseTable != null)
            {
                ubjects = databaseTable.GetObjects(type, filterProperties);

                if (ubjects.Count > 0)
                {
                    var objectAnatomyDefinition = GetObjectAnatomyDefinition(ubjects.Values.First());

                    foreach (var collectionObject in objectAnatomyDefinition.CollectionReferenceTypeObjects)
                    {
                        IEnumerator enumerator = collectionObject.Value.GetEnumerator();
                        Type collectionItemType = enumerator.GetType().GetGenericArguments()[0];
                        IDbTableManager childDatabaseTable = DbManager.GetDbTableManagerForObject(collectionItemType, false);

                        if (childDatabaseTable != null)
                        {
                            foreach (KeyValuePair<UbjectMetadata, object> ubject in ubjects)
                            {
                                List<Tuple<string, string, string>> mappingFilterCondition = new List<Tuple<string, string, string>>();

                                mappingFilterCondition.Add(new Tuple<string, string, string>(parentTableKey, databaseTable.TableName, "="));
                                mappingFilterCondition.Add(new Tuple<string, string, string>(parentObjectKey, ubject.Key.PrimaryKey.ToString(), "="));
                                mappingFilterCondition.Add(new Tuple<string, string, string>(childTableKey, childDatabaseTable.TableName, "="));

                                List<UbjectMapping> ubjectMappings = GetObjects<UbjectMapping>(mappingFilterCondition);
                                List<object> childObjects = new List<object>();

                                foreach (var ubjectMapping in ubjectMappings)
                                {
                                    List<Tuple<string, string, string>> childObjectFilterCondition = new List<Tuple<string, string, string>>();
                                    childObjectFilterCondition.Add(new Tuple<string, string, string>(UbjectMetadata.PRIMARY_KEY, ubjectMapping.ChildObjectKey, "="));
                                    childObjects.AddRange(GetObjectsIncMetadata(collectionItemType, childObjectFilterCondition).Values);
                                }

                                PropertyInfo propertyInfo = type.GetProperty(collectionObject.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                MethodInfo methodInfo = propertyInfo.PropertyType.GetMethod("Add");
                                object collectionInstance = Activator.CreateInstance(propertyInfo.PropertyType);

                                foreach (object childObject in childObjects)
                                {
                                    methodInfo.Invoke(collectionInstance, new object[] { childObject });
                                }

                                propertyInfo.SetValue(ubject.Value, collectionInstance, null);
                            }
                        }
                    }
                }
            }

            return (ubjects);
        }

        private ObjectAnatomyDefinition GetObjectAnatomyDefinition(object data)
        {
            ObjectAnatomyDefinition objectAnatomyDefinition = new ObjectAnatomyDefinition();
            var propertyInfo = data.GetType().GetPropertiesEx();

            foreach (var property in propertyInfo)
            {
                string propertyValue = string.Empty;

                if ((property.PropertyType == typeof(string)) ||
                    (property.PropertyType == typeof(int)) ||
                    (property.PropertyType == typeof(long)) ||
                    (property.PropertyType == typeof(float)) ||
                    (property.PropertyType == typeof(double)) ||
                    (property.PropertyType == typeof(bool)) ||
                    (property.PropertyType == typeof(int?)) ||
                    (property.PropertyType == typeof(long?)) ||
                    (property.PropertyType == typeof(float?)) ||
                    (property.PropertyType == typeof(double?)) ||
                    (property.PropertyType == typeof(bool?)) ||
                    (property.PropertyType.IsEnum) ||
                    (property.PropertyType == typeof(DateTime)))

                {
                    objectAnatomyDefinition.PropertiesAndValues.Add(property.Name, property.GetValue(data));
                }
                else if (typeof(ICollection).IsAssignableFrom(property.PropertyType))
                {
                    ICollection collectionData = (ICollection)property.GetValue(data);

                    IEnumerator enumerator = collectionData.GetEnumerator();
                    Type collectionItemType = enumerator.GetType().GetGenericArguments()[0];

                    if ((collectionItemType == typeof(string)) ||
                        (collectionItemType == typeof(int)) ||
                        (collectionItemType == typeof(long)) ||
                        (collectionItemType == typeof(float)) ||
                        (collectionItemType == typeof(double)) ||
                        (collectionItemType == typeof(bool)) ||
                        (collectionItemType == typeof(int?)) ||
                        (collectionItemType == typeof(long?)) ||
                        (collectionItemType == typeof(float?)) ||
                        (collectionItemType == typeof(double?)) ||
                        (collectionItemType == typeof(bool?)) ||
                        (property.PropertyType.IsEnum) ||
                        (collectionItemType == typeof(DateTime)))
                    {
                        objectAnatomyDefinition.CollectionValueTypeObjects.Add(property.Name, collectionData);
                    }
                    else
                    {
                        objectAnatomyDefinition.CollectionReferenceTypeObjects.Add(property.Name, collectionData);
                    }
                }
                else if (!property.PropertyType.IsValueType)
                {
                    objectAnatomyDefinition.ReferenceTypeObjects.Add(property.Name, property.GetValue(data));
                }
            }

            return (objectAnatomyDefinition);
        }

        /*private ElasticClient GetElasticSearchClient<T>() where T : class
        {
            DatabaseTable databaseTable = GetDatabaseTableForObject<T>();

            var node = new Uri("http://localhost:9200");

            var descriptor = new CreateIndexDescriptor(databaseTable.TableName)
                .Mappings(ms => ms.Map<T>(m => m.AutoMap())

            return (new ElasticClient(settings));
        }*/

        private List<IDbTableManager> tableManagers;

        private string backupSuffix = "_backup";

        public void CreateClones()
        {
            if (tableManagers == null)
            {
                tableManagers = DbManager.GetAllDbTableManagers();
            }

            foreach (var tableManager in tableManagers)
            {
                if (Regex.Matches(tableManager.TableName, backupSuffix).Count > 1)
                {
                    tableManager.DeleteTable();
                }

                if (!tableManager.TableName.Contains(backupSuffix))
                {
                    string backupTableName = string.Format("{0}{1}", tableManager.TableName, backupSuffix);

                    if (!tableManagers.Any(x => x.TableName.Equals(backupTableName)))
                    {
                        tableManager.CloneTable();
                    }
                }

                if (tableManager.TableName.Contains("_backup"))
                {
                    tableManager.RestoreBackup();
                }
            }

            foreach (var tableManager in tableManagers)
            {
                if (!tableManager.TableName.Contains("_backup"))
                {
                    tableManager.AddTrustMode();
                }
            }
        }
    }
}
