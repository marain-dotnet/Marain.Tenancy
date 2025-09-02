// <copyright file="ClientBase.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

#pragma warning disable CA1822 // Mark members as static - protected members don't use 'this' today but might in the future

namespace Marain.Clients;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base class for the clients.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="ClientBase"/> class.
/// </remarks>
/// <param name="httpClient">The client to use for API requests.</param>
public abstract class ClientBase(HttpClient httpClient, JsonSerializerOptions serializerOptions)
{
    /// <summary>
    /// Gets the HTTP client.
    /// </summary>
    public HttpClient Client { get; } = httpClient;

    /// <summary>
    /// Gets the serialization options that will be used to serialize and deserialize data.
    /// </summary>
    protected JsonSerializerOptions SerializerOptions { get; } = serializerOptions;

    /// <summary>
    /// Maps a <see cref="HttpResponseHeaders"/> to an <see cref="IImmutableDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="headers">The headers to map.</param>
    /// <returns>An <see cref="IImmutableDictionary{TKey, TValue}"/> containing the mapped headers.</returns>
    protected static IImmutableDictionary<string, string?> MapHttpResponseHeadersToDictionary(HttpResponseHeaders headers) =>
        headers.ToImmutableDictionary(
            x => x.Key,
            x => x.Value.FirstOrDefault(),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets data from the API using a link returned from a previous request.
    /// </summary>
    /// <typeparam name="T">The expected type of the response body.</typeparam>
    /// <param name="relativePath">The Url to request.</param>
    /// <param name="configureRequestMessage">A callback that can be used to add additional configuration to the request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    protected Task<ApiResponse<T>> GetPathAsync<T>(
        string relativePath,
        Action<HttpRequestMessage>? configureRequestMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            throw new ArgumentNullException(nameof(relativePath));
        }

        var requestUri = new Uri(relativePath, UriKind.Relative);

        return this.GetPathAsync<T>(requestUri, configureRequestMessage, cancellationToken);
    }

    /// <summary>
    /// Gets data from the API using the given link.
    /// </summary>
    /// <typeparam name="T">The expected type of the response body.</typeparam>
    /// <param name="requestUri">The Uri to request.</param>
    /// <param name="configureRequestMessage">A callback that can be used to add additional configuration to the request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    protected async Task<ApiResponse<T>> GetPathAsync<T>(
        Uri requestUri,
        Action<HttpRequestMessage>? configureRequestMessage,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = this.BuildRequest(HttpMethod.Get, requestUri);

        if (configureRequestMessage is not null)
        {
            configureRequestMessage(request);
        }

        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);

        return await this.BuildApiResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds an HTTP request with the supplied data.
    /// </summary>
    /// <typeparam name="T">The object type to send as the request content.</typeparam>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <param name="body">The data to send as the request content.</param>
    /// <returns>The constructed message.</returns>
    protected HttpRequestMessage BuildRequest<T>(HttpMethod method, Uri requestUri, T body)
    {
        ArgumentNullException.ThrowIfNull(body, nameof(body));

        var request = new HttpRequestMessage(method, requestUri);

        string json = JsonSerializer.Serialize(body, body.GetType(), this.SerializerOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return request;
    }

    /// <summary>
    /// Builds an HTTP request with the supplied data.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The URI of the request.</param>
    /// <returns>The constructed message.</returns>
    protected HttpRequestMessage BuildRequest(HttpMethod method, Uri requestUri)
    {
        return new HttpRequestMessage(method, requestUri);
    }

    /// <summary>
    /// Maps a <see cref="HttpResponseMessage"/> to an <see cref="ApiResponse{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type that the response body should be deserialized into.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The mapped <see cref="ApiResponse{T}"/>.</returns>
    /// <exception cref="InvalidOperationException">The response body could not be deserialized to the specified type.</exception>
    protected async Task<ApiResponse<T>> BuildApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        T? result = await JsonSerializer.DeserializeAsync<T>(contentStream, this.SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            throw new InvalidOperationException("Unable to deserialize response body");
        }

        return new ApiResponse<T>(
            response.StatusCode,
            MapHttpResponseHeadersToDictionary(response.Headers),
            result);
    }

    /// <summary>
    /// Builds a URL from the supplied path and query params.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <returns>The Uri.</returns>
    protected Uri ConstructUri(string path, params (string Key, string Value)[] queryParameters)
    {
        string query = string.Join("&", queryParameters.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

        if (!string.IsNullOrEmpty(query))
        {
            path += "?" + query;
        }

        return new Uri(path, UriKind.Relative);
    }

    /// <summary>
    /// Shortcut method for invoking an endpoint that is expected to return a 202 status code and implement the
    /// long running operation pattern.
    /// </summary>
    /// <typeparam name="T">The type of the request body.</typeparam>
    /// <param name="requestUri">The Uri to request.</param>
    /// <param name="method">The method use to request the Uri.</param>
    /// <param name="body">Data to be sent in the request body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An API containing the Location header from the response.</returns>
    protected Task<ApiResponse> CallLongRunningOperationEndpointAsync<T>(
        Uri requestUri,
        HttpMethod method,
        T body,
        CancellationToken cancellationToken = default)
    {
        if (body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        HttpRequestMessage request = this.BuildRequest(method, requestUri, body);
        return this.CallLongRunningOperationEndpointInternalAsync(request, cancellationToken);
    }

    /// <summary>
    /// Shortcut method for invoking an endpoint that is expected to return a 202 status code and implement the
    /// long running operation pattern.
    /// </summary>
    /// <param name="requestUri">The Uri to request.</param>
    /// <param name="method">The method use to request the Uri.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An API containing the Location header from the response.</returns>
    protected Task<ApiResponse> CallLongRunningOperationEndpointAsync(
        Uri requestUri,
        HttpMethod method,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = this.BuildRequest(method, requestUri);
        return this.CallLongRunningOperationEndpointInternalAsync(request, cancellationToken);
    }

    /// <summary>
    /// Sends the supplied request and throws a <see cref="MarainApiException"/> if either the request
    /// fails or the response status code does not indicate success.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    protected async Task<HttpResponseMessage> SendRequestAndThrowOnFailureAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;

        try
        {
            response = await this.Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (HttpRequestException ex)
        {
            string responseContent = (response is null || response.Content is null)
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            throw new MarainApiException("Unexpected error when calling service; see InnerException for details.", ex)
            {
                StatusCode = response?.StatusCode,
                ResponseMessage = responseContent,
            };
        }
    }

    /// <summary>
    /// Gets the response body as a JsonDocument.
    /// </summary>
    /// <param name="responseMessage">The response.</param>
    /// <returns>The resulting JsonDocument.</returns>
    protected async Task<JsonDocument> GetResponseJsonDocumentAsync(HttpResponseMessage responseMessage)
    {
        using Stream content = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return JsonDocument.Parse(content);
    }

    private async Task<ApiResponse> CallLongRunningOperationEndpointInternalAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);

        return new ApiResponse(response.StatusCode, MapHttpResponseHeadersToDictionary(response.Headers));
    }
}