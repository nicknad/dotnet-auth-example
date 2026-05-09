using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Common.InputValidation;
#pragma warning disable CA1515 // Consider making public types internal
public sealed class NotOnlyWhitespaceAttribute : ValidationAttribute
#pragma warning restore CA1515 // Consider making public types internal
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
        if (value is not string s)
            return new ValidationResult("Invalid value type.");

        ReadOnlySpan<char> span = (value as string).AsSpan();

        foreach (var c in span) {
            if (!char.IsWhiteSpace(c))
                return ValidationResult.Success;
        }
        return new ValidationResult("Only whitespaces are an invalid input");


        
    }
}
