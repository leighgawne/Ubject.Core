using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core.Database
{
    public interface IDbTableManager
    {
        IDbManager DbManager { get; set; }
        List<string> Columns { get; }
        string TableName { get; set; }

        long AddObject(Dictionary<string, object> propertiesAndValues);
        void DeleteAllObjects();
        void DeleteObject(UbjectMetadata ubjectMetadata);
        void DeleteObject(object data, List<string> filterProperties);
        Dictionary<UbjectMetadata, object> GetObjects(Type type, List<Tuple<string, string, string>> filterProperties = null);
        void UpdateObject(Dictionary<string, object> propertiesAndValues, long primaryKey);
        void DeleteTable();
        void CloneTable(string newTableName = null);
        void RestoreBackup(string newTableName = null);
        void AddTrustMode();
    }
}
