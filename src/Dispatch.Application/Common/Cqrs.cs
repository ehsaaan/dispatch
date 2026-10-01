namespace Dispatch.Application.Common;

public interface IRequest<TResponse> { }

public interface IRequestHandler<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request);
}

public sealed class Unit
{
    private Unit() { }
    public static readonly Unit Value = new();
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
