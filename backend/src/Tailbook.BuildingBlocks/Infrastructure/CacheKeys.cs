namespace Tailbook.BuildingBlocks.Infrastructure;

public static class CacheKeys
{
    public static string Throttle(string normalizedEmail)
    {
        return $"throttle:{normalizedEmail}";
    }

    public static string Idempotency(string idempotencyKey)
    {
        return $"idempotency:{idempotencyKey}";
    }

    public static string PriceRuleSetActive()
    {
        return "catalog:price-rule-set:active";
    }

    public static string DurationRuleSetActive()
    {
        return "catalog:duration-rule-set:active";
    }

    public static string RefreshTokenBlacklist(string tokenHash)
    {
        return $"refresh:blacklist:{tokenHash}";
    }

    public static string RateLimit(string clientIp, string method, string path, long windowStart)
    {
        return $"ratelimit:{clientIp}:{method}:{path}:{windowStart}";
    }

    public static string HealthCheck(Guid id)
    {
        return $"health:{id:N}";
    }

    public static string GroomerProfile(Guid groomerId)
    {
        return $"staff:groomer:{groomerId}:profile";
    }

    public static string GroomerSchedules(Guid groomerId)
    {
        return $"staff:groomer:{groomerId}:schedules";
    }

    public static string PetProfile(Guid petId)
    {
        return $"pets:profile:{petId:D}";
    }

    public static string InboxMessage(string messageId, string consumerName)
    {
        return $"inbox:{consumerName}:{messageId}";
    }
}
