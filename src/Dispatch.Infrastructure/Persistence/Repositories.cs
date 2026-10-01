using Dispatch.Application;
using Dispatch.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dispatch.Infrastructure.Persistence;

public sealed class EfJobRepository : IJobRepository
{
    private readonly DispatchDbContext _context;

    public EfJobRepository(DispatchDbContext context)
    {
        _context = context;
    }

    public Task<Job?> GetByIdAsync(Guid id)
    {
        return _context.Jobs.FirstOrDefaultAsync(j => j.Id == id, CancellationToken.None);
    }

    public async Task<IEnumerable<Job>> GetPendingAsync(JobType? type = null, JobPriority? priority = null)
    {
        var query = (IQueryable<Job>)_context.Jobs.Where(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Failed);
        if (type is not null) query = query.Where(j => j.Type == type);
        if (priority is not null) query = query.Where(j => j.Priority == priority);
        return await query.OrderByDescending(j => j.ScheduledAt).ToListAsync();
    }

    public Task AddAsync(Job job)
    {
        _context.Jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Job job)
    {
        _context.Jobs.Update(job);
        return Task.CompletedTask;
    }
}

public sealed class EfNotificationRepository : INotificationRepository
{
    private readonly DispatchDbContext _context;

    public EfNotificationRepository(DispatchDbContext context)
    {
        _context = context;
    }

    public Task<Notification?> GetByIdAsync(Guid id)
    {
        return _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, CancellationToken.None);
    }

    public async Task<IEnumerable<Notification>> GetPagedAsync(string? channel = null, bool? isSent = null, int page = 1, int pageSize = 50)
    {
        var query = (IQueryable<Notification>)_context.Notifications;
        if (channel is not null) query = query.Where(n => n.Channel == channel);
        if (isSent.HasValue) query = query.Where(n => n.IsSent == isSent.Value);
        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly DispatchDbContext _context;

    public EfUnitOfWork(DispatchDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
