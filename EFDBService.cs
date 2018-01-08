using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NostreetsExtensions;
using NostreetsExtensions.Interfaces;

namespace NostreetsEntities
{
    public class EFDBService<T> : IDBService<T> where T : class
    {

        public EFDBService()
        {
            if (!CheckIfTypeIsValid()) { throw new Exception("Type has to have a property called Id"); }
        }

        public EFDBService(string connectionKey)
        {
            if (!CheckIfTypeIsValid()) { throw new Exception("Type has to have a property called Id"); }

            _connectionKey = connectionKey;
        }

        private string _connectionKey = "DefaultConnection";
        private EFDBContext<T> _context = null;

        private bool CheckIfTypeIsValid()
        {
            return (typeof(T).GetProperties().FirstOrDefault(a => a.Name == "Id") != null) ? true : false;
        }

        public List<T> GetAll()
        {
            List<T> result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.ToList();
            }
            return result;
        }

        public T Get(object id)
        {
            Func<T, bool> predicate = a => a.GetType().GetProperty("Id").GetValue(a) == id;
            return Get(predicate);
        }

        public T Get(Func<T, bool> predicate)
        {
            T result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.FirstOrDefault(predicate);
            }
            return result;
        }

        public object Insert(T model)
        {
            object result = null;

            var firstProp = model.GetType().GetProperties()[0];

            if (firstProp.PropertyType.Name.Contains("Int"))
            {
                model.GetType().GetProperty(firstProp.Name).SetValue(model, GetAll().Count + 1);
            }
            else if (firstProp.PropertyType.Name == "GUID")
            {
                model.GetType().GetProperties().SetValue(Guid.NewGuid().ToString(), 0);
            }

            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                _context.Table.Add(model);

                if (_context.SaveChanges() == 0) { throw new Exception("DB changes not saved!"); }

                result = model.GetType().GetProperties().GetValue(0);
            }

            return result;
        }

        public void Delete(object id)
        {
            //Delete(ExpressionBuilder.GetPredicate<T>(new[] { new Filter("Id", Op.Equals, id) }));

            Func<T, bool> predicate = a => a.GetType().GetProperty("Id").GetValue(a) == id;
            Delete(predicate);
        }

        public void Delete(Func<T, bool> predicate)
        {
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                T obj = _context.Table.FirstOrDefault(predicate);

                _context.Table.Remove(obj);

                if (_context.SaveChanges() == 0) { throw new Exception("DB changes not saved!"); }
            }
        }

        public void Update(T model)
        {
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                _context.Table.Attach(model);
                _context.Entry(model).State = EntityState.Modified;

                if (_context.SaveChanges() == 0) { throw new Exception("DB changes not saved!"); }
            }
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            IEnumerable<T> result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.Where(predicate);
            }
            return result;
        }

        public IEnumerable<T> Where(Func<T, int, bool> predicate)
        {
            IEnumerable<T> result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.Where(predicate);
            }
            return result;
        }

    }

    public class EFDBService<T, IdType> : IDBService<T, IdType> where T : class
    {

        public EFDBService()
        {
            if (!CheckIfTypeIsValid()) { throw new Exception("Type has to have a property called Id"); }
        }

        public EFDBService(string connectionKey)
        {
            if (!CheckIfTypeIsValid()) { throw new Exception("Type has to have a property called Id"); }

            _connectionKey = connectionKey;
        }

        private string _connectionKey = "DefaultConnection";
        private EFDBContext<T> _context = null;

        private bool CheckIfTypeIsValid()
        {
            return (typeof(T).GetProperties().FirstOrDefault(a => a.Name == "Id") != null) ? true : false;
        }

        public List<T> GetAll()
        {
            List<T> result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.ToList();
            }
            return result;
        }

        public T Get(IdType id)
        {
            Func<T, bool> predicate = a => a.GetType().GetProperty("Id").GetValue(a) == (object)id;
            return Get(predicate);
        }

        public T Get(Func<T, bool> predicate)
        {
            T result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.FirstOrDefault(predicate);
            }
            return result;
        }

        public IdType Insert(T model)
        {
            IdType result = default(IdType);

            var firstProp = model.GetType().GetProperties()[0];

            if (firstProp.PropertyType.Name.Contains("Int"))
            {
                model.GetType().GetProperty(firstProp.Name).SetValue(model, GetAll().Count + 1);
            }
            else if (firstProp.PropertyType.Name == "GUID")
            {
                model.GetType().GetProperties().SetValue(Guid.NewGuid().ToString(), 0);
            }

            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                _context.Table.Add(model);

                if (_context.SaveChanges() == 0) { throw new Exception("DB changes not saved!"); }

                result = (IdType)model.GetType().GetProperties().GetValue(0);
            }

            return result;
        }

        public void Delete(IdType id)
        {
            //Delete(ExpressionBuilder.GetPredicate<T>(new[] { new Filter("Id", Op.Equals, id) }));

            Func<T, bool> predicate = a => a.GetType().GetProperty("Id").GetValue(a) == (object)id;
            Delete(predicate);
        }

        public void Delete(Func<T, bool> predicate)
        {
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                T obj = _context.Table.FirstOrDefault(predicate);

                _context.Table.Remove(obj);

                if (_context.SaveChanges() == 0) { throw new Exception("DB changes not saved!"); }
            }
        }

        public void Update(T model)
        {
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                _context.Table.Attach(model);
                _context.Entry(model).State = EntityState.Modified;

                if (_context.SaveChanges() == 0) { throw new Exception("DB changes not saved!"); }
            }
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            IEnumerable<T> result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.Where(predicate);
            }
            return result;
        }

        public IEnumerable<T> Where(Func<T, int, bool> predicate)
        {
            IEnumerable<T> result = null;
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                result = _context.Table.Where(predicate);
            }
            return result;
        }

    }

    public class EFDBContext<TContext> : DbContext where TContext : class
    {
        public EFDBContext()
            : base("DefaultConnection")
        { }

        public EFDBContext(string connectionKey)
            : base(connectionKey)
        { }

        public EFDBContext(string connectionKey, string tableName)
            : base(connectionKey)
        {
            OnModelCreating(new DbModelBuilder().HasDefaultSchema(tableName));
        }

        public IDbSet<TContext> Table { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //Func<Type, bool> modelConfigPredicate = type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>);

            //var typesToRegister = Assembly.GetExecutingAssembly().GetTypes().Where(modelConfigPredicate);

            //foreach (Type type in typesToRegister)
            //{
            //    dynamic configurationInstance = Activator.CreateInstance(type);
            //    modelBuilder.Configurations.Add(configurationInstance);
            //}

            List<string> columnNames = this.GetColumns(typeof(TContext));
            List<PropertyInfo> allProps = typeof(TContext).GetProperties().ToList();
            List<PropertyInfo> excludedProps = allProps.GetPropertiesByAttribute<NotMappedAttribute>(typeof(TContext));
            List<PropertyInfo> includedProps = allProps.Where(a => excludedProps.Any(b => b.Name != a.Name)).ToList();

            if (columnNames.Where(a => includedProps.Any(b => b.Name != a)) != null ||
                includedProps.Where(a => columnNames.Any(b => b != a.Name)) != null)
            {
            }

            



            base.OnModelCreating(modelBuilder);
        }

    }
}
