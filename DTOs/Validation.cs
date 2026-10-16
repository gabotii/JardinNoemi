using System.ComponentModel.DataAnnotations;
using JardinNoemi.Models;

namespace JardinNoemi.DTOs;

/// <summary>
/// Base para validaciones que solo aplican cuando otra propiedad tiene un valor dado
/// (p. ej. dirección solo si el método de entrega es despacho). Funciona tanto en la
/// validación por campo de Blazor como en la validación completa del servidor.
/// </summary>
public abstract class ConditionalValidationAttribute(string otherProperty, object expectedValue) : ValidationAttribute
{
    protected bool ConditionMet(ValidationContext context)
    {
        var prop = context.ObjectType.GetProperty(otherProperty);
        var actual = prop?.GetValue(context.ObjectInstance);
        return Equals(actual, expectedValue);
    }

    protected sealed override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (!ConditionMet(context)) return ValidationResult.Success;
        return Check(value)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage, context.MemberName is { } m ? [m] : null);
    }

    protected abstract bool Check(object? value);
}

public sealed class RequiredWhenAttribute(string otherProperty, object expectedValue)
    : ConditionalValidationAttribute(otherProperty, expectedValue)
{
    protected override bool Check(object? value) => value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;
}

public sealed class RegionWhenAttribute(string otherProperty, object expectedValue)
    : ConditionalValidationAttribute(otherProperty, expectedValue)
{
    protected override bool Check(object? value) => Regions.IsValid(value as string);
}

public sealed class CardNumberWhenAttribute(string otherProperty, object expectedValue)
    : ConditionalValidationAttribute(otherProperty, expectedValue)
{
    protected override bool Check(object? value)
    {
        var digits = new string((value as string ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length is >= 13 and <= 19 && CardValidation.PassesLuhn(digits);
    }
}

public sealed class CardExpiryWhenAttribute(string otherProperty, object expectedValue)
    : ConditionalValidationAttribute(otherProperty, expectedValue)
{
    protected override bool Check(object? value) =>
        CardValidation.TryParseExpiry(value as string, out var lastDay) && lastDay >= DateOnly.FromDateTime(DateTime.UtcNow);
}

public sealed class CvcWhenAttribute(string otherProperty, object expectedValue)
    : ConditionalValidationAttribute(otherProperty, expectedValue)
{
    protected override bool Check(object? value) =>
        value is string s && s.Length is 3 or 4 && s.All(char.IsDigit);
}
