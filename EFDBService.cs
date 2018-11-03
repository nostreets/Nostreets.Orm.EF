using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using NostreetsExtensions.Extend.Basic;
using NostreetsExtensions.Extend.Data;
using NostreetsExtensions.Interfaces;

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

        public static void Migrate(string connectionString)
        {

            EFDBContext<T>.ConnectionString = connectionString;

            //+SetInitializer
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<EFDBContext<T>, GenericMigrationConfiguration<T>>()
            );


            using (EFDBContext<T> context = new EFDBContext<T>(connectionString, typeof(T).Name))
            {
                DbMigrator migrator = new DbMigrator(new GenericMigrationConfiguration<T>());

                if (!context.Database.CompatibleWithModel(false))
                    migrator.Update();
            }

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

        public static void Migrate(string connectionString)
        {
            EFDBContext<T>.ConnectionString = connectionString;

            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<EFDBContext<T>, GenericMigrationConfiguration<T>>()
            );
        }
    }

    public class EFDBContext<TContext> : DbContext where TContext : class
    {
        public EFDBContext()
            : base(ConnectionString)
        {
            ConnectionString = Database.Connection.ConnectionString;
        }

        public EFDBContext(string connectionKey)
            : base(connectionKey ?? ConnectionString)
        {
            ConnectionString = Database.Connection.ConnectionString;
        }

        public EFDBContext(string connectionKey, string tableName)
            : base(connectionKey ?? ConnectionString)
        {
            ConnectionString = Database.Connection.ConnectionString;

            if (tableName != null)
                OnModelCreating(new DbModelBuilder().HasDefaultSchema(tableName));
        }

        internal static string ConnectionString { get; set; } = "DefaultConnection";

        public IDbSet<TContext> Table { get; set; }
    }

    public class GenericMigrationConfiguration<TContext> : DbMigrationsConfiguration<EFDBContext<TContext>> where TContext : class
    {
        public GenericMigrationConfiguration()
        {
            AutomaticMigrationDataLossAllowed = false;
            AutomaticMigrationsEnabled = true;
            ContextType = typeof(EFDBContext<TContext>);
            ContextKey = "NostreetsEntities.EFDBContext`1[" + typeof(TContext) + "]";
            TargetDatabase = new DbConnectionInfo(
                 connectionString: EFDBContext<TContext>.ConnectionString ?? throw new ArgumentNullException("EFDBContext<TContext>.ConnectionString"),
                 providerInvariantName: "System.Data.SqlClient"
            //"The name of the provider to use for the connection. Use 'System.Data.SqlClient' for SQL Server."
            );

        }
    }

    public class TableWatcher<T> where T : class
    {
        public TableWatcher(Action<T> onChange, Predicate<T> predicate = null, string connectionKey = null, string tableName = null)
        {
            _onChange = onChange ?? throw new ArgumentNullException("onChange");
            _context = new EFDBContext<T>(connectionKey, tableName);

        }



        private EFDBContext<T> _context;
        private Predicate<T> _predicate;
        private Action<T> _onChange;

        public void CheckChanges()
        {
            DbChangeTracker changeTracker = _context.ChangeTracker;
            IEnumerable<DbEntityEntry<T>> entries = changeTracker.Entries<T>();

            foreach (DbEntityEntry<T> entry in entries)
            {
                T entity = entry.Entity;
                if (_predicate == null)
                    _onChange(entity);
                else
                {
                    if (_predicate(entity))
                        _onChange(entity);
                }
            }
        }
    }

   
    

}