using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Common;

internal class AuthApiResult
{
    public bool Succeeded { get; protected set; }
    public IReadOnlyList<string> Errors { get; }

    public static AuthApiResult Success { get; } = new AuthApiResult(true);

    protected AuthApiResult(bool succeeded) {
        Succeeded = succeeded;
        Errors = Array.Empty<string>();
    }

    protected AuthApiResult(IEnumerable<string> errors) {
        Succeeded = false;
        Errors = errors?.ToList() ?? new List<string>();
    }

    public static AuthApiResult Failed(params string[] errors)
        => new AuthApiResult(errors);

    public static AuthApiResult Failed(IEnumerable<string> errors)
        => new AuthApiResult(errors);

    public override string ToString()
        => Succeeded
            ? "Succeeded"
            : $"Failed: {string.Join(", ", Errors)}";
}

