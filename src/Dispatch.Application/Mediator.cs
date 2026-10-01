using Dispatch.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Dispatch.Application;

public sealed class Mediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse>
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        return await handler.Handle(request);
    }
}
