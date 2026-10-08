using UniProject.Share.Application.Pagination;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace UniProject.Share.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                return new PagedResult<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>(items, totalItems, pageNumber, pageSize);
        }

        public static async Task<PagedResult<TDest>> ToPagedResultAsync<TSrc, TDest>(
            this IQueryable<TSrc> query,
            int pageNumber,
            int pageSize,
            Expression<Func<TSrc, TDest>> selector,
            CancellationToken cancellationToken = default)
        {
            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                return new PagedResult<TDest>(Array.Empty<TDest>(), 0, pageNumber, pageSize);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TDest>(items, totalItems, pageNumber, pageSize);
        }

        public static async Task<PagedResult<TDest>> ToPagedResultWithMapperAsync<TSrc, TDest>(
            this IQueryable<TSrc> query,
            int pageNumber,
            int pageSize,
            Func<TSrc, TDest> mapper,
            CancellationToken cancellationToken = default)
        {
            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                return new PagedResult<TDest>(Array.Empty<TDest>(), 0, pageNumber, pageSize);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var projected = items.Select(mapper).ToList();

            return new PagedResult<TDest>(projected, totalItems, pageNumber, pageSize);
        }
    }
}
