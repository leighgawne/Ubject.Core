using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Ninject;
using Ninject.Modules;
using Ninject.Parameters;

using Ubject.Core.Database;

using MySql.Data.MySqlClient;
using System.Data.SQLite;
using System.Data.SqlClient;

namespace Ubject.Core
{
    public enum E_DbEngine : int
    {
        mysql = 0,
        sqlite = 1,
        azure = 2
    }

    public class UbjectDIBindings : NinjectModule
    {
        static public Func<E_DbEngine> GetDBEngine;

        private E_DbEngine DBEngine
        {
            get
            {
                if (GetDBEngine != null)
                {
                    return (GetDBEngine());
                }

                return (E_DbEngine.azure);
            }
        }

        public override void Load()
        {
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            switch (DBEngine)
            {
                case E_DbEngine.mysql:
                {
                    Bind<IDbManager>().To<MySqlDbManager>();
                    Bind<IDbTableManager>().To<MySqlDbTableManager>();
                    Bind<IDbConnection>().To<MySqlConnection>();
                    Bind<IDbCommand>().To<MySqlCommand>();
                    Bind<IDataParameter>().To<MySqlParameter>();
                    break;
                }
                case E_DbEngine.sqlite:
                {
                    Bind<IDbManager>().To<SqLiteDbManager>();
                    Bind<IDbTableManager>().To<SqLiteDbTableManager>();
                    Bind<IDbConnection>().To<SQLiteConnection>();
                    Bind<IDbCommand>().To<SQLiteCommand>();
                    Bind<IDataParameter>().To<SQLiteParameter>();
                    break;
                }
                case E_DbEngine.azure:
                {
                    Bind<IDbManager>().To<AzureDbManager>();
                    Bind<IDbTableManager>().To<AzureDbTableManager>();
                    Bind<IDbConnection>().To<SqlConnection>();
                    Bind<IDbCommand>().To<SqlCommand>();
                    Bind<IDataParameter>().To<SqlParameter>();
                    break;
                }
            }

        }

        public static IKernel ExternalProvider { get; set; }

        private static IKernel internalProvider;

        private static IKernel InternalProvider
        {
            get
            {
                if (internalProvider == null)
                {
                    internalProvider = new StandardKernel();
                    internalProvider.Load(Assembly.GetExecutingAssembly());
                }

                return (internalProvider);
            }
        }

        public static T Resolve<T>(ConstructorArgument[] parameters = null)
        {
            IKernel provider = null;

            if (InternalProvider.CanResolve<T>())
            {
                provider = InternalProvider;
            }
            else if ((ExternalProvider != null) && (ExternalProvider.CanResolve<T>()))
            {
                provider = ExternalProvider;
            }

            return ((T)(Resolve(provider, typeof(T), parameters)));
        }

        public static object Resolve(Type type, ConstructorArgument[] parameters = null)
        {
            IKernel provider = null;

            if ((bool)InternalProvider.CanResolve(type))
            {
                provider = InternalProvider;
            }
            else if ((bool)ExternalProvider.CanResolve(type))
            {
                provider = ExternalProvider;
            }

            return (Resolve(provider, type, parameters));
        }

        private static object Resolve(IKernel provider, Type type, ConstructorArgument[] parameters = null)
        {
            if (provider != null)
            {
                if (parameters != null)
                {
                    return (provider.Get(type, parameters));
                }

                return (provider.Get(type));
            }

            throw new Exception("Unabled to resolve binding with available DI containers");
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Equals("System.Data.SQLite, Version=1.0.82.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139"))
            {
                if (IntPtr.Size > 4)
                {
                    string assemblyLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x64/System.Data.SQLite.dll");
                    return (System.Reflection.Assembly.LoadFile(assemblyLocation));
                }
                else
                {
                    string assemblyLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "x86/System.Data.SQLite.dll");
                    return (System.Reflection.Assembly.LoadFile(assemblyLocation));
                }
            }

            return (null);
        }
    }
}
