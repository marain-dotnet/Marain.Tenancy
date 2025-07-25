// <copyright file="TenantServiceResult.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Services;

/// <summary>
/// Represents the result of a tenant service operation.
/// </summary>
public sealed record TenantServiceResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantServiceResult"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful.</param>
    /// <param name="errorType">The type of error if operation failed.</param>
    /// <param name="errorMessage">The error message if operation failed.</param>
    private TenantServiceResult(bool isSuccess, TenantServiceErrorType? errorType = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error type if the operation failed.
    /// </summary>
    public TenantServiceErrorType? ErrorType { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static TenantServiceResult Success() => new(true);

    /// <summary>
    /// Creates a not found result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A not found result.</returns>
    public static TenantServiceResult NotFound(string message) => new(false, TenantServiceErrorType.NotFound, message);

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A conflict result.</returns>
    public static TenantServiceResult Conflict(string message) => new(false, TenantServiceErrorType.Conflict, message);

    /// <summary>
    /// Creates a validation error result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A validation error result.</returns>
    public static TenantServiceResult ValidationError(string message) => new(false, TenantServiceErrorType.ValidationError, message);

    /// <summary>
    /// Creates a not modified result.
    /// </summary>
    /// <returns>A not modified result.</returns>
    public static TenantServiceResult NotModified() => new(false, TenantServiceErrorType.NotModified);
}

/// <summary>
/// Represents the result of a tenant service operation with data.
/// </summary>
/// <typeparam name="T">The type of data returned.</typeparam>
public sealed record TenantServiceResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantServiceResult{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation was successful.</param>
    /// <param name="data">The data if operation was successful.</param>
    /// <param name="errorType">The type of error if operation failed.</param>
    /// <param name="errorMessage">The error message if operation failed.</param>
    /// <param name="etag">The ETag value for the resource.</param>
    private TenantServiceResult(bool isSuccess, T? data = default, TenantServiceErrorType? errorType = null, string? errorMessage = null, string? etag = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
        ETag = etag;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error type if the operation failed.
    /// </summary>
    public TenantServiceErrorType? ErrorType { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the data if the operation was successful.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Gets the ETag value for the resource.
    /// </summary>
    public string? ETag { get; }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <param name="etag">Optional ETag value.</param>
    /// <returns>A successful result with data.</returns>
    public static TenantServiceResult<T> Success(T data, string? etag = null) => new(true, data, etag: etag);

    /// <summary>
    /// Creates a not found result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A not found result.</returns>
    public static TenantServiceResult<T> NotFound(string message) => new(false, errorType: TenantServiceErrorType.NotFound, errorMessage: message);

    /// <summary>
    /// Creates a conflict result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A conflict result.</returns>
    public static TenantServiceResult<T> Conflict(string message) => new(false, errorType: TenantServiceErrorType.Conflict, errorMessage: message);

    /// <summary>
    /// Creates a validation error result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A validation error result.</returns>
    public static TenantServiceResult<T> ValidationError(string message) => new(false, errorType: TenantServiceErrorType.ValidationError, errorMessage: message);

    /// <summary>
    /// Creates a not modified result.
    /// </summary>
    /// <returns>A not modified result.</returns>
    public static TenantServiceResult<T> NotModified() => new(false, errorType: TenantServiceErrorType.NotModified);
}

/// <summary>
/// Defines the types of errors that can occur in tenant service operations.
/// </summary>
public enum TenantServiceErrorType
{
    /// <summary>
    /// The requested tenant was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// There was a conflict with the request.
    /// </summary>
    Conflict,

    /// <summary>
    /// The request had validation errors.
    /// </summary>
    ValidationError,

    /// <summary>
    /// The resource was not modified (for conditional requests).
    /// </summary>
    NotModified
}