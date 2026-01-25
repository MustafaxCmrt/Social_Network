using Domain.Entities;
using Persistence.Repositories;

namespace Persistence.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    // Entity bazlı repository'ler
    IRepository<Users> Users { get; }
    IRepository<Posts> Posts { get; }
    IRepository<Threads> Threads { get; }
    IRepository<Categories> Categories { get; }
    IRepository<PostVotes> PostVotes { get; }
    IRepository<Notifications> Notifications { get; }
    IRepository<Reports> Reports { get; }
    IRepository<UserBans> UserBans { get; }
    IRepository<UserMutes> UserMutes { get; }
    IRepository<PasswordResetTokens> PasswordResetTokens { get; }
    IRepository<AuditLogs> AuditLogs { get; }
    
    // Transaction yönetimi
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
