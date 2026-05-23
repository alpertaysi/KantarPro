using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using KantarPro.Application.Abstractions;

namespace KantarPro.Application.Tests.Fakes
{
    public class InMemoryRepository<T> : IRepository<T> where T : class
    {
        private readonly List<T> _items;

        public InMemoryRepository(List<T> items)
        {
            _items = items;
        }

        public IQueryable<T> Query()
        {
            return _items.AsQueryable();
        }

        public T SingleOrDefault(Expression<Func<T, bool>> predicate)
        {
            return _items.AsQueryable().SingleOrDefault(predicate);
        }

        public void Add(T entity)
        {
            _items.Add(entity);
        }
    }
}

