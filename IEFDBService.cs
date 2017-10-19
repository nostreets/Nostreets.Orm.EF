using NostreetsORM.Interfaces;
using System;
using System.Collections.Generic;

namespace NostreetsEntities
{
    public interface IEFDBService<T> : IDBService<T>
    {
        new List<T> GetAll();
        T Get(Func<T, bool> predicate);
        new object Insert(T model);
        void Delete(Func<T, bool> predicate);
        void Update(Func<T, bool> predicate, T model);
    }

    public interface IEFDBService<T, IdType> : IDBService<T, IdType>
    {
        new List<T> GetAll();
        T Get(Func<T, bool> predicate);
        new IdType Insert(T model);
        void Delete(Func<T, bool> predicate);
        void Update(Func<T, bool> predicate, T model);
    }
}
