using Dispatch.Domain;

namespace Dispatch.Application;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id);
    Task<IEnumerable<Job>> GetPendingAsync(JobType? type = null, JobPriority? priority = null);
    Task AddAsync(Job job);
    Task UpdateAsync(Job job);
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Notification>> GetPagedAsync(string? channel = null, bool? isSent = null, int page = 1, int pageSize = 50);
    Task AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
