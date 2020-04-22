using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core.Database
{
    public class DbConfigHelper
    {
        private static string localDBFolderCache = null;

        public static string LocalDBFolder
        {
            get
            {
                if (localDBFolderCache == null)
                {
                    localDBFolderCache = string.Empty;
#if DEBUG
                    //string appStorageFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                    //string appStorageFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                    string appStorageFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
#else
                    string appStorageFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
#endif

                    if (appStorageFolder.StartsWith("file:\\"))
                    {
                        appStorageFolder = appStorageFolder.Substring("file:\\".Length);
                    }

                    localDBFolderCache = Path.Combine(appStorageFolder, "ubject");

                    if (!Directory.Exists(localDBFolderCache))
                    {
                        Directory.CreateDirectory(localDBFolderCache);
                    }
                }

                return (localDBFolderCache);
            }
        }
    }
}
