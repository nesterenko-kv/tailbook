namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record CoatTypeView(Guid Id, Guid? AnimalTypeId, string Code, string Name);