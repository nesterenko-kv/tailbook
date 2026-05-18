namespace Tailbook.Modules.Customer.Domain.Entities;

public sealed class ContactPerson
{
    private readonly List<ContactMethod> _methods = [];

    private ContactPerson()
    {
    }

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? Notes { get; private set; }
    public string TrustLevel { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<ContactMethod> Methods => _methods.AsReadOnly();

    internal static ContactPerson Create(
        Guid id,
        Guid clientId,
        string firstName,
        string? lastName,
        string? notes,
        string trustLevel,
        bool isActive,
        DateTimeOffset utcNow)
    {
        var timestamp = utcNow.ToUniversalTime();

        return new ContactPerson
        {
            Id = id,
            ClientId = clientId,
            FirstName = firstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            TrustLevel = trustLevel.Trim(),
            IsActive = isActive,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    internal ContactMethod AddContactMethod(
        string methodType,
        string rawValue,
        string displayValue,
        bool isPreferred,
        string verificationStatus,
        DateTimeOffset utcNow)
    {
        var method = ContactMethod.Create(
            Guid.NewGuid(),
            Id,
            methodType,
            rawValue,
            displayValue,
            isPreferred,
            verificationStatus,
            utcNow);

        _methods.Add(method);
        return method;
    }

    internal void ClearPreferredMethods(DateTimeOffset utcNow)
    {
        foreach (var method in _methods.Where(m => m.IsPreferred))
        {
            method.SetPreferred(false, utcNow);
        }
    }

    internal ContactMethod? GetMethod(string methodType, string rawValue)
    {
        var normalizedValue = ContactMethod.NormalizeContactValue(methodType, rawValue);
        return _methods.SingleOrDefault(m => m.MethodType == methodType && m.NormalizedValue == normalizedValue);
    }
}
