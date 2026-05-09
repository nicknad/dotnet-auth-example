namespace Auth.Api.Common.InputValidation;

internal record struct PasswordValidationResult
{
    public bool IsValid { get; }
    public string? Error { get; }

    private PasswordValidationResult(bool isValid, string? error) {
        IsValid = isValid;
        Error = error;
    }

    public static PasswordValidationResult Success()
        => new(true, null);

    public static PasswordValidationResult Fail(string error)
        => new(false, error);
}
