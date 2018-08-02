using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Configuration;
using NostreetsExtensions;
using NostreetsExtensions.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NostreetsEntities
{
    public class EFDBService<T> : IDBService<T> where T : class
    {

        public EFDBService()
        {
            _pkName = GetPKName(typeof(T), out string output);

            if (output != "")
                throw new Exception(output);
        }

        public EFDBService(string connectionKey)
        {
            _pkName = GetPKName(typeof(T), out string output);

            if (output != "")
                throw new Exception(output);

            _connectionKey = connectionKey;
        }

        private string _connectionKey = "DefaultConnection";
        private EFDBContext<T> _context = null;
        private string _pkName = null;

        private void BackupDB(string path)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[_connectionKey].ConnectionString);
            string query = "BACKUP DATABASE {0} TO DISK = '{1}'".FormatString(builder.InitialCatalog, path);
            _context.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, query);

        }

        private string GetPKName(Type type, out string output)
        {
            output = "";
            PropertyInfo pk = type.GetPropertiesByKeyAttribute()?.FirstOrDefault() ?? type.GetProperties()[0];

            if (!type.IsClass)
                output = "Generic Type has to be a custom class...";

            else if (!pk.Name.ToLower().Contains("id") && !(pk.PropertyType == typeof(int) || pk.PropertyType == typeof(Guid) || pk.PropertyType == typeof(string)))
                output = "Primary Key must be the data type of Int32, Guid, or String and the Name needs ID in it...";

            return pk.Name;

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

        public T Get(object id, Converter<T, T> converter)
        {
            Func<T, bool> predicate = a => a.GetType().GetProperty(_pkName).GetValue(a) == id;
            return (converter == null) ? FirstOrDefault(predicate) : converter(FirstOrDefault(predicate));
        }

        public T Get(object id)
        {
            return Get(id);
        }

        public object Insert(T model)
        {
            object result = null;

            PropertyInfo pk = model.GetType().GetProperty(_pkName);

            if (pk.PropertyType.Name.Contains("Int"))
                model.GetType().GetProperty(pk.Name).SetValue(model, GetAll().Count + 1);

            else if (pk.PropertyType.Name == "GUID")
                model.GetType().GetProperties().SetValue(Guid.NewGuid().ToString(), 0);

            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                _context.Table.Add(model);

                if (_context.SaveChanges() == 0)
                    throw new Exception("DB changes not saved!");

                result = model.GetType().GetProperty(_pkName).GetValue(model);
            }

            return result;
        }

        public object Insert(T model, Converter<T, T> converter)
        {
            model = converter(model) ?? throw new NullReferenceException("converter");

            return Insert(model);
        }

        public object[] Insert(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new NullReferenceException("collection");

            List<object> result = new List<object>();

            foreach (T item in collection)
                result.Add(Insert(item));


            return result.ToArray();

        }

        public object[] Insert(IEnumerable<T> collection, Converter<T, T> converter)
        {
            if (collection == null)
                throw new NullReferenceException("collection");

            if (converter == null)
                throw new NullReferenceException("converter");

            List<object> result = new List<object>();

            foreach (T item in collection)
                result.Add(Insert(converter(item)));


            return result.ToArray();
        }

        public void Delete(object id)
        {
            Func<T, bool> predicate = a => a.GetType().GetProperty(_pkName).GetValue(a) == id;
            Delete(predicate);
        }

        public void Delete(Func<T, bool> predicate)
        {
            using (_context = new EFDBContext<T>(_connectionKey, typeof(T).Name))
            {
                T obj = _context.Table.FirstOrDefault(predicate);

                _context.Table.Remove(obj);

                if (_context.SaveChanges() == 0)
                    throw new Exception("DB changes not saved!");
            }
        }

        public void Delete(IEnumerable<object> ids)
        {
            if (ids == null)
                throw new NullReferenceException("ids");

            foreach (object id in ids)
                Delete(id);
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

        public void Update(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new NullReferenceException("collection");

            foreach (T item in collection)
                Update(item);
        }

        public void Update(IEnumerable<T> collection, Converter<T, T> converter)
        {
            if (collection == null)
                throw new NullReferenceException("collection");

            if (converter == null)
                throw new NullReferenceException("converter");

            foreach (T item in collection)
                Update(converter(item));
        }

        public void Update(T model, Converter<T, T> converter)
        {
            if (converter == null)
                throw new NullReferenceException("converter");

            Update(converter(model));
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

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            return Where(predicate).FirstOrDefault();
        }

        public void Backup(string path = null)
        {
            BackupDB(path);
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

        private void BackupDB(string path)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings[_connectionKey].ConnectionString);
            string query = "BACKUP DATABASE {0} TO DISK = '{1}'".FormatString(builder.InitialCatalog, path);
            _context.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, query);

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

        public T Get(IdType id, Converter<T, T> converter)
        {
            Func<T, bool> predicate = a => a.GetType().GetProperty("Id").GetValue(a) == (object)id;
            return (converter == null) ? FirstOrDefault(predicate) : converter(FirstOrDefault(predicate));
        }

        public T Get(IdType id)
        {
            return Get(id);
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

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            return Where(predicate).FirstOrDefault();
        }

        public IdType Insert(T model, Converter<T, T> converter)
        {
            throw new NotImplementedException();
        }

        public IdType[] Insert(IEnumerable<T> collection)
        {
            throw new NotImplementedException();
        }

        public IdType[] Insert(IEnumerable<T> collection, Converter<T, T> converter)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<T> collection)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<T> collection, Converter<T, T> converter)
        {
            throw new NotImplementedException();
        }

        public void Update(T model, Converter<T, T> converter)
        {
            throw new NotImplementedException();
        }

        public void Delete(IEnumerable<IdType> ids)
        {
            throw new NotImplementedException();
        }

        public void Backup(string path = null)
        {
            BackupDB(path);
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

        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    //Func<Type, bool> modelConfigPredicate = type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>);

        //    //var typesToRegister = Assembly.GetExecutingAssembly().GetTypes().Where(modelConfigPredicate);

        //    //foreach (Type type in typesToRegister)
        //    //{
        //    //    dynamic configurationInstance = Activator.CreateInstance(type);
        //    //    modelBuilder.Configurations.Add(configurationInstance);
        //    //}

        //    List<string> columnNames = this.GetColumns(typeof(TContext));
        //    List<PropertyInfo> allProps = typeof(TContext).GetProperties().ToList();
        //    List<PropertyInfo> excludedProps = Extend.GetPropertiesByNotMappedAttribute(typeof(TContext));
        //    List<PropertyInfo> includedProps = allProps.Where(a => excludedProps.Any(b => b.Name != a.Name)).ToList();

        //    if (columnNames.Where(a => includedProps.Any(b => b.Name != a)) != null ||
        //        includedProps.Where(a => columnNames.Any(b => b != a.Name)) != null)
        //    {
        //    }



        //    base.OnModelCreating(modelBuilder);
        //}

    }
}
