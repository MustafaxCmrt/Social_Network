using Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Persistence.Context;
using Persistence.Repositories;

namespace Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy initialization için private field'lar
    private IRepository<Users>? _users;
    private IRepository<Posts>? _posts;
    private IRepository<Threads>? _threads;
    private IRepository<Categories>? _categories;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lazy-loaded Repository'ler
    public IRepository<Users> Users => _users ??= new Repository<Users>(_context);
    public IRepository<Posts> Posts => _posts ??= new Repository<Posts>(_context);
    public IRepository<Threads> Threads => _threads ??= new Repository<Threads>(_context);
    public IRepository<Categories> Categories => _categories ??= new Repository<Categories>(_context);

    // SaveChanges
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    // Transaction yönetimi
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction!.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // Dispose pattern
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
