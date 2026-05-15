using System.ComponentModel.DataAnnotations;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class IdempotentRequest
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = IdempotentRequestStatuses.Processing;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public static class IdempotentRequestStatuses
{
    public const string Processing = "Processing";
    public const string Completed = "Completed";
}
