namespace Auth.Api.Common.InputValidation;

internal static class PasswordValidator
{
    public static PasswordValidationResult Validate(ReadOnlySpan<char> password) {
        // 1. Length check
        if (password.Length < 8)
            return PasswordValidationResult.Fail("Password must be at least 8 characters.");

        if (password.Length > 128)
            return PasswordValidationResult.Fail("Password exceeds maximum length.");

        // 2. Reject whitespace-only
        if (IsWhiteSpaceOnly(password))
            return PasswordValidationResult.Fail("Password cannot be empty or whitespace.");

        if (IsCommonPassword(password))
            return PasswordValidationResult.Fail("Password is too common.");

        return PasswordValidationResult.Success();
    }

    private static bool IsWhiteSpaceOnly(ReadOnlySpan<char> span) {
        foreach (var c in span) {
            if (!char.IsWhiteSpace(c))
                return false;
        }
        return true;
    }

    private static bool IsCommonPassword(ReadOnlySpan<char> span) {
        // stub – replace with common password list
        return span.SequenceEqual("password".AsSpan()) ||
               span.SequenceEqual("12345678".AsSpan());
    }
}
