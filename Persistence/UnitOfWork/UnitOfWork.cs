using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Persistence.Context;
using Persistence.Repositories;

namespace Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUserService;
    private IDbContextTransaction? _transaction;

    // Lazy initialization için private field'lar
    private IRepository<Users>? _users;
    private IRepository<Posts>? _posts;
    private IRepository<Threads>? _threads;
    private IRepository<Categories>? _categories;
    private IRepository<PostVotes>? _postVotes;
    private IRepository<Notifications>? _notifications;
    private IRepository<Reports>? _reports;
    private IRepository<UserBans>? _userBans;
    private IRepository<UserMutes>? _userMutes;

    public UnitOfWork(ApplicationDbContext context, ICurrentUserService? currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    // Lazy-loaded Repository'ler
    public IRepository<Users> Users => _users ??= new Repository<Users>(_context);
    public IRepository<Posts> Posts => _posts ??= new Repository<Posts>(_context);
    public IRepository<Threads> Threads => _threads ??= new Repository<Threads>(_context);
    public IRepository<Categories> Categories => _categories ??= new Repository<Categories>(_context);
    public IRepository<PostVotes> PostVotes => _postVotes ??= new Repository<PostVotes>(_context);
    public IRepository<Notifications> Notifications => _notifications ??= new Repository<Notifications>(_context);
    public IRepository<Reports> Reports => _reports ??= new Repository<Reports>(_context);
    public IRepository<UserBans> UserBans => _userBans ??= new Repository<UserBans>(_context);
    public IRepository<UserMutes> UserMutes => _userMutes ??= new Repository<UserMutes>(_context);



    // SaveChanges
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Mevcut kullanıcı ID'sini DbContext'e set et
        _context.CurrentUserId = _currentUserService?.GetCurrentUserId();

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
