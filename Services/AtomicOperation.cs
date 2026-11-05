using HealthTrack.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HealthTrack.Services;

// Wraps work in a database transaction, or a savepoint when one is already open.
public sealed class AtomicOperation : IAsyncDisposable
{
    private readonly HealthTrackDbContext _context;
    private readonly IDbContextTransaction _transaction;
    private readonly string? _savepoint;
    private bool _committed;
    private AtomicOperation(HealthTrackDbContext context, IDbContextTransaction transaction, string? savepoint)
        => (_context, _transaction, _savepoint) = (context, transaction, savepoint);

    // Starts a transaction or savepoint.
    public static async Task<AtomicOperation> BeginAsync(HealthTrackDbContext context)
    {
        if (context.Database.CurrentTransaction is { } existing)
        {
            var name = "op_" + Guid.NewGuid().ToString("N")[..24];
            await existing.CreateSavepointAsync(name);
            return new(context, existing, name);
        }
        return new(context, await context.Database.BeginTransactionAsync(), null);
    }

    // Commits the work.
    public async Task CommitAsync()
    {
        // SQL Server savepoints need no release command.
        if (_savepoint == null) await _transaction.CommitAsync();
        _committed = true;
    }

    // Rolls back if the work was not committed.
    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            if (_savepoint != null) await _transaction.RollbackToSavepointAsync(_savepoint);
            else await _transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
        }
        if (_savepoint == null) await _transaction.DisposeAsync();
    }
}
