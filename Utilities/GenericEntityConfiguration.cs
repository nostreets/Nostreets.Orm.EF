using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NostreetsExtensions;
using System.ComponentModel.DataAnnotations;

namespace NostreetsEntities.Utilities
{
    class GenericEntityConfiguration<T> : EntityTypeConfiguration<T> where T : class
    {
        public GenericEntityConfiguration()
        {
            List<PropertyInfo> allProps = typeof(T).GetProperties().ToList();
            List<PropertyInfo> excludedProps = allProps.GetPropertiesByAttribute<NotMappedAttribute>(typeof(T));
            List<PropertyInfo> includedProps = allProps.Where(a => excludedProps.Any(b => b.Name != a.Name)).ToList();

            ToTable(typeof(T).Name + "s");

            //Map(a => a.Properties<T>());



            


        }


    }
}
