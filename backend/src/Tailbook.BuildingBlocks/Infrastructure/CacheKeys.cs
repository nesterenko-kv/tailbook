namespace Tailbook.BuildingBlocks.Infrastructure;

public static class CacheKeys
{
    public static string Throttle(string normalizedEmail) => $"throttle:{normalizedEmail}";

    public static string Idempotency(string idempotencyKey) => $"idempotency:{idempotencyKey}";

    public static string PriceRuleSetActive() => "catalog:price-rule-set:active";

    public static string DurationRuleSetActive() => "catalog:duration-rule-set:active";

    public static string RefreshTokenBlacklist(string tokenHash) => $"refresh:blacklist:{tokenHash}";

    public static string RateLimit(string clientIp, string method, string path, long windowStart) =>
        $"ratelimit:{clientIp}:{method}:{path}:{windowStart}";

    public static string HealthCheck(Guid id) => $"health:{id:N}";

    public static string GroomerProfile(Guid groomerId) => $"staff:groomer:{groomerId}:profile";

    public static string GroomerSchedules(Guid groomerId) => $"staff:groomer:{groomerId}:schedules";

    public static string PetProfile(Guid petId) => $"pets:profile:{petId:D}";
}
