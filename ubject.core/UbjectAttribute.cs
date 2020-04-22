using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class UbjectAttribute : Attribute
    {
        private bool explicitInclude = false;
        private bool ignore = false;
        private bool index = false;
        private bool include = false;
        private string tableName = string.Empty;

        public UbjectAttribute(bool explicitInclude = false, bool ignore = false, bool index = false, bool include = false, string tableName = "")
        {
            this.explicitInclude = explicitInclude;
            this.ignore = ignore;
            this.index = index;
            this.include = include;
            this.tableName = tableName;
        }

        public bool ExplicitInclude
        {
            get
            {
                return (explicitInclude);
            }
        }

        public bool Include
        {
            get
            {
                return (include);
            }
        }

        public bool Ignore
        {
            get
            {
                return (ignore);
            }
        }

        public bool Index
        {
            get
            {
                return (index);
            }
        }

        public string TableName
        {
            get
            {
                return (tableName);
            }
        }
    }
}
