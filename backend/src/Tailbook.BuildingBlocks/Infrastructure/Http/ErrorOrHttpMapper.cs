using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        {
            return Problem(StatusCodes.Status500InternalServerError, "Unexpected error", "The operation failed without error details.", errors);
        }

        return errors[0].Type switch
        {
            ErrorType.Validation => ValidationProblem(errors),
            ErrorType.NotFound => Problem(StatusCodes.Status404NotFound, "Resource not found", errors[0].Description, errors),
            ErrorType.Conflict => Problem(StatusCodes.Status409Conflict, "Conflict", errors[0].Description, errors),
            ErrorType.Unauthorized => Problem(StatusCodes.Status401Unauthorized, "Unauthorized", errors[0].Description, errors),
            ErrorType.Forbidden => Problem(StatusCodes.Status403Forbidden, "Forbidden", errors[0].Description, errors),
            ErrorType.Unexpected => Problem(StatusCodes.Status500InternalServerError, "Unexpected error", errors[0].Description, errors),
            _ => Problem(StatusCodes.Status400BadRequest, "Request failed", errors[0].Description, errors)
        };
    }

    private static IResult ValidationProblem(IReadOnlyList<Error> errors)
    {
        var groupedErrors = errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.Code) ? "General" : error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        var problemDetails = new ValidationProblemDetails(groupedErrors)
        {
            Title = "Validation failed",
            Detail = errors[0].Description,
            Status = StatusCodes.Status400BadRequest
        };

        return Results.Json(problemDetails, statusCode: problemDetails.Status, contentType: "application/problem+json");
    }

    private static IResult Problem(int statusCode, string title, string detail, IReadOnlyList<Error> errors)
    {
        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode
        };
        problemDetails.Extensions["errors"] = errors.Select(error => new
        {
            error.Code,
            error.Description,
            Type = error.Type.ToString()
        }).ToArray();

        return Results.Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json");
    }
}
