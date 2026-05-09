namespace Auth.Api.Abstractions;

internal interface IAuthLogger
{
    void LogInformation(string messageTemplate, params object[] propertyValues);
    void LogWarning(string messageTemplate, params object[] propertyValues);
}
