using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ubject.Core
{
    internal static class Extensions
    {
        public static string ToStringEx(this object stringObject)
        {
            if (stringObject == null)
            {
                return (string.Empty);
            }
            else
            {
                return (stringObject).ToString();
            }
        }

        public static string GetTableName(this Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            var propertyInfo = type.GetProperties();

            var customUbjectAttributes = type.GetCustomAttributes(false).ToList().Where(x => x.GetType() == typeof(UbjectAttribute)).FirstOrDefault();
            return (customUbjectAttributes != null) ? ((UbjectAttribute)customUbjectAttributes).TableName : string.Empty;
        }

        public static PropertyInfo[] GetPropertiesEx(this Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            var propertyInfo = type.GetProperties();

            var customUbjectAttributes = type.GetCustomAttributes(false).ToList().Where(x => x.GetType() == typeof(UbjectAttribute)).FirstOrDefault();
            bool explicitInclude = (customUbjectAttributes != null) ? ((UbjectAttribute)customUbjectAttributes).ExplicitInclude : false;

            foreach (var property in propertyInfo)
            {
                List<object> customAttribs = property.GetCustomAttributes(false).ToList();
                bool ignoreProperty = explicitInclude;

                if (customAttribs.Any(x => x.GetType() == typeof(UbjectAttribute)))
                {
                    if (explicitInclude)
                    {
                        ignoreProperty = (!((UbjectAttribute)customAttribs.First(x => x.GetType() == typeof(UbjectAttribute))).Include);
                    }
                    else
                    {
                        ignoreProperty = ((UbjectAttribute)customAttribs.First(x => x.GetType() == typeof(UbjectAttribute))).Ignore;
                    }
                }

                if (!ignoreProperty)
                {
                    properties.Add(property);
                }
            }

            return (properties.ToArray());
        }

        public static List<PropertyInfo> GetIndexedProperties(this List<PropertyInfo> properties)
        {
            List<PropertyInfo> indexedProperties = new List<PropertyInfo>();

            foreach (PropertyInfo property in properties)
            {
                List<object> customAttribs = property.GetCustomAttributes(false).ToList();

                if (customAttribs.Any(x => x.GetType() == typeof(UbjectAttribute)))
                {
                    if (((UbjectAttribute)customAttribs.First(x => x.GetType() == typeof(UbjectAttribute))).Index)
                    {
                        indexedProperties.Add(property);
                    }
                }
            }

            return (indexedProperties);
        }
    }
}
