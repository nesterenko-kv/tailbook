namespace Tailbook.Modules.Customer.Application.Customer.Commands;

public sealed record UpdateClientContactPreferencesInput(IReadOnlyCollection<UpdateClientContactMethodInput> Methods);
public sealed record UpdateClientContactMethodInput(string MethodType, string Value, bool IsPreferred, string? Notes);
