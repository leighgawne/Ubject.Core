using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using Ninject.Parameters;

namespace Ubject.Core.Database
{
    public class DbQueryExecutor
    {
        private IDbManager dbManager;
        private IDbConnection connection;
        private IDbCommand command;

        private string connectionString;

        public virtual string ConnectionString
        {
            get
            {
                return (connectionString);
            }
            set
            {
                connectionString = value;
            }
        }

        public DbQueryExecutor()
        {
        }

        public DbQueryExecutor(IDbManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public void ExecuteNonQuery(StringBuilder commandText, Dictionary<string, object> parameters = null)
        {
            CreateCommand(commandText, parameters);
            command.ExecuteNonQuery();
            Cleanup();
        }

        public object ExecuteScalar(StringBuilder commandText, Dictionary<string, object> parameters = null)
        {
            CreateCommand(commandText, parameters);
            object result = command.ExecuteScalar();
            Cleanup();

            return (result);
        }

        private void CreateCommand(StringBuilder commandText, Dictionary<string, object> parameters)
        {
            connection = Utilities.CreateDbConnection(dbManager);
            connection.Open();
            command = connection.CreateCommand();

            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> parameter in parameters)
                {
                    List<ConstructorArgument> sqlParameters = new List<ConstructorArgument>();
                    sqlParameters.Add(new ConstructorArgument("parameterName", parameter.Key, false));
                    sqlParameters.Add(new ConstructorArgument("value", parameter.Value, false));
                    IDataParameter dataParameter = UbjectDIBindings.Resolve<IDataParameter>(sqlParameters.ToArray());
                    command.Parameters.Add(dataParameter);
                }
            }

            command.CommandText = commandText.ToString();
        }

        private void Cleanup()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
