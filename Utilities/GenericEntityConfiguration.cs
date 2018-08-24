using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using NostreetsExtensions.Extend.Data;

namespace NostreetsEntities.Utilities
{
    class GenericEntityConfiguration<T> : EntityTypeConfiguration<T> where T : class
    {
        public GenericEntityConfiguration()
        {
            List<PropertyInfo> allProps = typeof(T).GetProperties().ToList();
            List<PropertyInfo> excludedProps = typeof(T).GetPropertiesByNotMappedAttribute();
            List<PropertyInfo> includedProps = allProps.Where(a => excludedProps.Any(b => b.Name != a.Name)).ToList();

            ToTable(typeof(T).Name + "s");

            //Map(a => a.Properties<T>());



        }


    }
}
