using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace Tailbook.BuildingBlocks.Infrastructure.Http;

public static class ErrorOrHttpMapper
{
    public static IResult ToHttpResult<T>(this ErrorOr<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        return result.IsError
            ? result.Errors.ToHttpResult()
            : Results.Json(result.Value, statusCode: successStatusCode);
    }

    public static IResult ToHttpResult(this IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return Problem(StatusCodes.Status500InternalServerError, "Unexpected error",
                "The operation failed without error details.", errors);

        var firstError = errors[0];
        var description = firstError.Description;

        return firstError.Type switch
        {
            ErrorType.Validation =>
                ValidationProblem(StatusCodes.Status400BadRequest, "Validation failed", description, errors),
            ErrorType.NotFound =>
                Problem(StatusCodes.Status404NotFound, "Resource not found", description, errors),
            ErrorType.Conflict =>
                Problem(StatusCodes.Status409Conflict, "Conflict", description, errors),
            ErrorType.Unauthorized =>
                Problem(StatusCodes.Status401Unauthorized, "Unauthorized", description, errors),
            ErrorType.Forbidden =>
                Problem(StatusCodes.Status403Forbidden, "Forbidden", description, errors),
            ErrorType.Unexpected =>
                Problem(StatusCodes.Status500InternalServerError, "Unexpected error", description, errors),
            _ => Problem(StatusCodes.Status400BadRequest, "Request failed", description, errors)
        };
    }

    private static IResult ValidationProblem(int statusCode, string title, string detail, IReadOnlyList<Error> errors)
    {
        var groupedErrors = errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.Code) ? "General" : error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return Results.ValidationProblem(
            groupedErrors,
            title: title,
            detail: detail,
            statusCode: statusCode
        );
    }

    private static IResult Problem(int statusCode, string title, string detail, IReadOnlyList<Error> errors)
    {
        Dictionary<string, object?> extensions = new()
        {
            {
                "errors", errors.Select(error => new
                {
                    error.Code,
                    error.Description,
                    Type = error.Type.ToString()
                }).ToArray()
            }
        };

        return Results.Problem(detail, title: title, statusCode: statusCode, extensions: extensions);
    }
}
