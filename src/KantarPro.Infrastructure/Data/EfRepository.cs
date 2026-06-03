using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using KantarPro.Application.Abstractions;

namespace KantarPro.Infrastructure.Data
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        private readonly DbSet<T> _set;

        public EfRepository(DbContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _set = context.Set<T>();
        }

        public IQueryable<T> Query()
        {
            return _set;
        }

        public T SingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            return _set.SingleOrDefault(predicate);
        }

        public void Add(T entity)
        {
            _set.Add(entity);
        }

        public void Remove(T entity)
        {
            _set.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _set.RemoveRange(entities);
        }
    }
}
