namespace Tailbook.Modules.Customer.Application.Customer.Commands;

public sealed record UpdateClientContactPreferencesCommand(IReadOnlyCollection<UpdateClientContactMethodCommand> Methods);
public sealed record UpdateClientContactMethodCommand(string MethodType, string Value, bool IsPreferred, string? Notes);
