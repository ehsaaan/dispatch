using Dispatch.Application.Common;
using Dispatch.Domain;

namespace Dispatch.Application.Jobs;

public sealed record ScheduleJobRequest(
    string Name,
    string Payload,
    JobType Type,
    JobPriority Priority = JobPriority.Normal,
    int MaxRetries = 3,
    DateTime? ScheduledAt = null) : IRequest<ScheduleJobResponse>;

public sealed record ScheduleJobResponse(Guid JobId);

public sealed record GetPendingJobsRequest(
    JobType? Type = null,
    JobPriority? Priority = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedJobResponse>;

public sealed record PagedJobResponse(IEnumerable<JobSummary> Items, int Page, int PageSize, int Total);

public sealed record GetJobByIdRequest(Guid Id) : IRequest<JobDto?>;

public sealed record CancelJobRequest(Guid Id) : IRequest<CancelJobResponse>;

public sealed record CancelJobResponse(Guid JobId);

public sealed record JobDto(
    Guid Id,
    string Name,
    string Payload,
    string Type,
    string Priority,
    string Status,
    int RetryCount,
    int MaxRetries,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    TimeSpan? Duration);

public sealed record JobSummary(
    Guid Id,
    string Name,
    string Type,
    string Priority,
    string Status);

public static class JobMappings
{
    public static JobDto ToDto(this Job job)
    {
        return new JobDto(
            job.Id,
            job.Name,
            job.Payload,
            job.Type.ToString(),
            job.Priority.ToString(),
            job.Status.ToString(),
            job.RetryCount,
            job.MaxRetries,
            job.ScheduledAt,
            job.StartedAt,
            job.CompletedAt,
            job.Duration);
    }

    public static JobSummary ToSummary(this Job job)
    {
        return new JobSummary(
            job.Id,
            job.Name,
            job.Type.ToString(),
            job.Priority.ToString(),
            job.Status.ToString());
    }
}

public sealed class ScheduleJobHandler : IRequestHandler<ScheduleJobRequest, ScheduleJobResponse>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleJobHandler(IJobRepository jobRepository, IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ScheduleJobResponse> Handle(ScheduleJobRequest request)
    {
        var job = Job.Create(Guid.NewGuid(), request.Name, request.Payload, request.Type, request.Priority, request.MaxRetries, request.ScheduledAt);
        await _jobRepository.AddAsync(job);
        await _unitOfWork.SaveChangesAsync();
        return new ScheduleJobResponse(job.Id);
    }
}

public sealed class GetPendingJobsHandler : IRequestHandler<GetPendingJobsRequest, PagedJobResponse>
{
    private readonly IJobRepository _jobRepository;

    public GetPendingJobsHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<PagedJobResponse> Handle(GetPendingJobsRequest request)
    {
        var jobs = await _jobRepository.GetPendingAsync(request.Type, request.Priority);
        var paged = jobs.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);
        return new PagedJobResponse(paged.Select(j => j.ToSummary()), request.Page, request.PageSize, jobs.Count());
    }
}

public sealed class GetJobByIdHandler : IRequestHandler<GetJobByIdRequest, JobDto?>
{
    private readonly IJobRepository _jobRepository;

    public GetJobByIdHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobDto?> Handle(GetJobByIdRequest request)
    {
        var job = await _jobRepository.GetByIdAsync(request.Id);
        return job?.ToDto();
    }
}

public sealed class CancelJobHandler : IRequestHandler<CancelJobRequest, CancelJobResponse>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelJobHandler(IJobRepository jobRepository, IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CancelJobResponse> Handle(CancelJobRequest request)
    {
        var job = await _jobRepository.GetByIdAsync(request.Id);
        if (job is null)
            throw new NotFoundException($"Job with ID {request.Id} not found.");

        job.Cancel();
        await _jobRepository.UpdateAsync(job);
        await _unitOfWork.SaveChangesAsync();
        return new CancelJobResponse(job.Id);
    }
}
