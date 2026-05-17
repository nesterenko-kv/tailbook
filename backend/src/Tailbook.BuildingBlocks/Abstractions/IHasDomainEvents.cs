namespace Tailbook.BuildingBlocks.Abstractions;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}
