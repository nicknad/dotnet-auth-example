using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Common.InputValidation;

#pragma warning disable CA1515 // Consider making public types internal
public sealed class StrongPasswordAttribute : ValidationAttribute
#pragma warning restore CA1515 // Consider making public types internal
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
        if (value is not string s)
            return new ValidationResult("Invalid password.");

        var result = PasswordValidator.Validate(s.AsSpan());

        return result.IsValid
            ? ValidationResult.Success
            : new ValidationResult(result.Error);
    }
}
