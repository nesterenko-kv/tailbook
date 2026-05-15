using System.Text;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    private static readonly HashSet<string> IdempotentMethods = ["POST", "PUT", "PATCH"];

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore idempotencyStore)
    {
        var method = context.Request.Method;
        if (!IdempotentMethods.Contains(method))
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var acquireResult = await idempotencyStore.TryAcquireAsync(idempotencyKey, context.RequestAborted);
        if (acquireResult.IsError)
        {
            await next(context);
            return;
        }

        var result = acquireResult.Value;

        if (result.IsCompleted && result.ExistingStatusCode.HasValue)
        {
            context.Response.StatusCode = result.ExistingStatusCode.Value;
            context.Response.Headers["Idempotency-Key"] = "Replayed";

            if (!string.IsNullOrWhiteSpace(result.ExistingResponseBody))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(result.ExistingResponseBody, Encoding.UTF8, context.RequestAborted);
            }

            logger.LogDebug("Idempotency key {Key} replayed with status {StatusCode}", idempotencyKey, result.ExistingStatusCode.Value);
            return;
        }

        if (!result.IsNew && !result.IsCompleted)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(
                """{"type":"about:blank","title":"A request with this idempotency key is already in progress.","status":409}""",
                Encoding.UTF8,
                context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        using var captureStream = new MemoryStream();
        context.Response.Body = captureStream;

        try
        {
            await next(context);

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                captureStream.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(captureStream, Encoding.UTF8).ReadToEndAsync(context.RequestAborted);
                captureStream.Seek(0, SeekOrigin.Begin);
                await captureStream.CopyToAsync(originalBody, context.RequestAborted);

                await idempotencyStore.CompleteAsync(idempotencyKey, context.Response.StatusCode, body, context.RequestAborted);
                logger.LogDebug("Idempotency key {Key} completed with status {StatusCode}", idempotencyKey, context.Response.StatusCode);
            }
            else
            {
                captureStream.Seek(0, SeekOrigin.Begin);
                await captureStream.CopyToAsync(originalBody, context.RequestAborted);
            }
        }
        catch
        {
            captureStream.Seek(0, SeekOrigin.Begin);
            await captureStream.CopyToAsync(originalBody, context.RequestAborted);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
