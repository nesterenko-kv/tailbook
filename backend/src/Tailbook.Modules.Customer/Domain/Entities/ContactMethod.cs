using Tailbook.Modules.Customer.Contracts;

namespace Tailbook.Modules.Customer.Domain.Entities;

public sealed class ContactMethod
{
    private ContactMethod()
    {
    }

    public Guid Id { get; private set; }
    public Guid ContactPersonId { get; private set; }
    public string MethodType { get; private set; } = string.Empty;
    public string NormalizedValue { get; private set; } = string.Empty;
    public string DisplayValue { get; private set; } = string.Empty;
    public bool IsPreferred { get; private set; }
    public string VerificationStatus { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    internal static ContactMethod Create(
        Guid id,
        Guid contactPersonId,
        string methodType,
        string rawValue,
        string displayValue,
        bool isPreferred,
        string verificationStatus,
        DateTimeOffset utcNow)
    {
        var timestamp = utcNow.ToUniversalTime();
        var normalizedValue = NormalizeContactValue(methodType, rawValue);

        return new ContactMethod
        {
            Id = id,
            ContactPersonId = contactPersonId,
            MethodType = methodType.Trim(),
            NormalizedValue = normalizedValue,
            DisplayValue = (displayValue ?? string.Empty).Trim(),
            IsPreferred = isPreferred,
            VerificationStatus = verificationStatus.Trim(),
            IsActive = true,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    internal void SetPreferred(bool preferred, DateTimeOffset utcNow)
    {
        IsPreferred = preferred;
        UpdatedAt = utcNow.ToUniversalTime();
    }

    internal void UpdateDetails(string displayValue, string? notes, DateTimeOffset utcNow)
    {
        DisplayValue = (displayValue ?? string.Empty).Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = utcNow.ToUniversalTime();
    }

    internal static string NormalizeContactValue(string methodType, string value)
    {
        var trimmed = value.Trim();
        return methodType switch
        {
            ContactMethodTypes.Phone => System.Text.RegularExpressions.Regex.Replace(trimmed, "[^0-9+]", string.Empty),
            ContactMethodTypes.Instagram => trimmed.TrimStart('@').ToLowerInvariant(),
            ContactMethodTypes.Email => trimmed.ToLowerInvariant(),
            _ => trimmed
        };
    }
}
