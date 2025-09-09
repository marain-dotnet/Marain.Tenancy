// <copyright file="ErrorHandlingExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.ErrorHandling;

using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Extension methods for error handling in minimal APIs.
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Creates a problem details response for a bad request.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>A bad request problem details response.</returns>
    public static ProblemHttpResult BadRequestProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for a not found error.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>A not found problem details response.</returns>
    public static ProblemHttpResult NotFoundProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for an unprocessable entity erorr.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>An unprocessable entity problem details response.</returns>
    public static ProblemHttpResult UnprocessableEntityProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Unprocessable Entity",
            type: "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for a conflict error.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>A conflict problem details response.</returns>
    public static ProblemHttpResult ConflictProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for an internal server error.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>An internal server error problem details response.</returns>
    public static ProblemHttpResult InternalServerErrorProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for unsupported media type.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>An unsupported media type problem details response.</returns>
    public static ProblemHttpResult UnsupportedMediaTypeProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status415UnsupportedMediaType,
            title: "Unsupported Media Type",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.13",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for unauthorized access.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>An unauthorized problem details response.</returns>
    public static ProblemHttpResult UnauthorizedProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            type: "https://tools.ietf.org/html/rfc7235#section-3.1",
            instance: instance);

    /// <summary>
    /// Creates a problem details response for forbidden access.
    /// </summary>
    /// <param name="detail">The detailed error message.</param>
    /// <param name="instance">The instance URI.</param>
    /// <returns>A forbidden problem details response.</returns>
    public static ProblemHttpResult ForbiddenProblem(string detail, string? instance = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            instance: instance);
}