using MediatR;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        TResponse response = await next(cancellationToken);

        logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}
