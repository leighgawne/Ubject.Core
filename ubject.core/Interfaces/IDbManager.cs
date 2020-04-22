using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace Ubject.Core.Database
{
    public interface IDbManager
    {
        string ConnectionString { get; }
        string CustomConnectionString { get; set; }
        string SchemaName { get; set; }
        int VarCharFieldSize { get; }

        void InitDatabaseSchema();
        IDbTableManager CreateDatabaseTable(string tableName, List<PropertyInfo> properties);
        IDbTableManager GetDbTableManagerForObject(Type type, bool createTable = true);
        List<IDbTableManager> GetAllDbTableManagers();
    }
}