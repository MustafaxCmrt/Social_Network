using System.Linq.Expressions;
using Domain.Common;
using Microsoft.EntityFrameworkCore.Query;

namespace Persistence.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    // READ OPERASYONLARI
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllWithIncludesAsync(
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    
    // CREATE OPERASYONLARI
    Task CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    
    // UPDATE OPERASYONLARI
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    
    // DELETE OPERASYONLARI
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
}
