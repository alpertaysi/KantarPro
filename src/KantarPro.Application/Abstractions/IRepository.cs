using System;
using System.Linq;
using System.Linq.Expressions;

namespace KantarPro.Application.Abstractions
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        T SingleOrDefault(Expression<Func<T, bool>> predicate);
        void Add(T entity);
    }
}

