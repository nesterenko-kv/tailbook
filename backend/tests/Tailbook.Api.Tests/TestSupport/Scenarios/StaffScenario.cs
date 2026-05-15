using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

internal sealed class StaffScenario(HttpClient client)
{
    public static StaffScenario For(HttpClient client)
        => new(client);

    public async Task<GroomerEnvelope> CreateGroomerAsync(
        string displayName,
        Guid? userId = null,
        bool? active = null)
    {
        var response = await client.PostAsJsonAsync("/api/admin/groomers", new
        {
            displayName,
            userId,
            active
        });
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<GroomerEnvelope>();
    }

    public async Task AddWorkingScheduleAsync(
        Guid groomerId,
        int weekday,
        string startLocalTime = "09:00",
        string endLocalTime = "18:00")
    {
        var response = await AddWorkingScheduleResponseAsync(groomerId, weekday, startLocalTime, endLocalTime);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HttpResponseMessage> AddWorkingScheduleResponseAsync(
        Guid groomerId,
        int weekday,
        string startLocalTime = "09:00",
        string endLocalTime = "18:00")
        => await client.PostAsJsonAsync($"/api/admin/groomers/{groomerId:D}/working-schedules", new
        {
            groomerId,
            weekday,
            startLocalTime,
            endLocalTime
        });

    public async Task<GroomerEnvelope> CreateSchedulableGroomerAsync(
        string displayName = "Schedulable Groomer",
        Guid? userId = null,
        IEnumerable<int>? weekdays = null,
        string startLocalTime = "09:00",
        string endLocalTime = "18:00")
    {
        var groomer = await CreateGroomerAsync(displayName, userId);

        foreach (var weekday in weekdays ?? Enumerable.Range(1, 7))
        {
            await AddWorkingScheduleAsync(groomer.Id, weekday, startLocalTime, endLocalTime);
        }

        return groomer;
    }

    public async Task<HttpResponseMessage> AddTimeBlockResponseAsync(Guid groomerId, DateTimeOffset startAt, DateTimeOffset endAt, string reasonCode, string? notes = null)
        => await client.PostAsJsonAsync($"/api/admin/groomers/{groomerId:D}/time-blocks", new
        {
            groomerId,
            startAt,
            endAt,
            reasonCode,
            notes
        });

    public async Task AddCapabilityAsync(
        Guid groomerId,
        Guid breedId,
        Guid offerId,
        string capabilityMode,
        int reservedDurationModifierMinutes)
    {
        var response = await AddCapabilityResponseAsync(groomerId, breedId, offerId, capabilityMode, reservedDurationModifierMinutes);
        response.EnsureSuccessStatusCode();
    }

    public async Task<HttpResponseMessage> AddCapabilityResponseAsync(
        Guid groomerId,
        Guid breedId,
        Guid offerId,
        string capabilityMode,
        int reservedDurationModifierMinutes)
        => await client.PostAsJsonAsync($"/api/admin/groomers/{groomerId:D}/capabilities", new
        {
            groomerId,
            breedId,
            offerId,
            capabilityMode,
            reservedDurationModifierMinutes
        });

    public async Task<HttpResponseMessage> CheckAvailabilityResponseAsync(
        Guid groomerId,
        Guid petId,
        DateTimeOffset startAt,
        int reservedMinutes,
        params Guid[] offerIds)
        => await client.PostAsJsonAsync($"/api/admin/groomers/{groomerId:D}/availability/check", new
        {
            groomerId,
            petId,
            startAt,
            reservedMinutes,
            offerIds
        });

    public async Task<AvailabilityEnvelope> CheckAvailabilityAsync(
        Guid groomerId,
        Guid petId,
        DateTimeOffset startAt,
        int reservedMinutes,
        params Guid[] offerIds)
    {
        var response = await CheckAvailabilityResponseAsync(groomerId, petId, startAt, reservedMinutes, offerIds);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<AvailabilityEnvelope>();
    }

    public async Task<GroomerScheduleEnvelope> GetScheduleAsync(Guid groomerId, string from, string to)
    {
        var response = await client.GetAsync($"/api/admin/groomers/{groomerId:D}/schedule?from={from}&to={to}");
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<GroomerScheduleEnvelope>();
    }
}
