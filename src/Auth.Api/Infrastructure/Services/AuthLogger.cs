using Auth.Api.Abstractions;

namespace Auth.Api.Infrastructure.Services;

internal class AuthLogger : IAuthLogger
{
    private readonly Serilog.ILogger _logger;

    public AuthLogger(Serilog.ILogger logger) {
        _logger = logger;
    }

    public void LogInformation(string messageTemplate, params object[] args) =>
        _logger.Information(messageTemplate, args);

    public void LogWarning(string messageTemplate, params object[] args) =>
        _logger.Warning(messageTemplate, args);
}
