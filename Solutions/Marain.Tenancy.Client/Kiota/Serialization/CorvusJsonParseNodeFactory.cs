// <copyright file="CorvusJsonParseNodeFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Kiota.Serialization;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Microsoft.Kiota.Abstractions.Serialization;

/// <summary>
/// A custom parse node factory that uses Corvus JSON serialization options.
/// </summary>
public class CorvusJsonParseNodeFactory : IParseNodeFactory, IAsyncParseNodeFactory
{
    private readonly IJsonSerializerOptionsProvider _optionsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorvusJsonParseNodeFactory"/> class.
    /// </summary>
    /// <param name="optionsProvider">The JSON serializer options provider.</param>
    public CorvusJsonParseNodeFactory(IJsonSerializerOptionsProvider optionsProvider)
    {
        this._optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }

    /// <summary>
    /// Gets the valid content type for this factory.
    /// </summary>
    public string ValidContentType => "application/json";

    /// <summary>
    /// Creates a parse node for the specified content type and stream.
    /// </summary>
    /// <param name="contentType">The content type to parse.</param>
    /// <param name="content">The content stream to parse.</param>
    /// <returns>A parse node instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when contentType or content is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when contentType is not supported.</exception>
    public IParseNode GetRootParseNode(string contentType, Stream content)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentType);
        ArgumentNullException.ThrowIfNull(content);

        if (!this.ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(contentType), $"Expected {this.ValidContentType}");
        }

        JsonSerializerOptions jsonOptions = this._optionsProvider.Instance;
        var document = JsonDocument.Parse(content, new JsonDocumentOptions
        {
            AllowTrailingCommas = jsonOptions.AllowTrailingCommas,
            CommentHandling = jsonOptions.ReadCommentHandling == JsonCommentHandling.Skip
                ? JsonCommentHandling.Skip
                : JsonCommentHandling.Disallow,
        });

        return new CorvusJsonParseNode(document.RootElement.Clone(), jsonOptions);
    }

    /// <summary>
    /// Asynchronously creates a parse node for the specified content type and stream.
    /// </summary>
    /// <param name="contentType">The content type to parse.</param>
    /// <param name="content">The content stream to parse.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation and contains the parse node instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when contentType or content is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when contentType is not supported.</exception>
    public async Task<IParseNode> GetRootParseNodeAsync(string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentType);
        ArgumentNullException.ThrowIfNull(content);

        if (!this.ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(contentType), $"Expected {this.ValidContentType}");
        }

        JsonSerializerOptions jsonOptions = this._optionsProvider.Instance;
        JsonDocument document = await JsonDocument.ParseAsync(content, new JsonDocumentOptions
        {
            AllowTrailingCommas = jsonOptions.AllowTrailingCommas,
            CommentHandling = jsonOptions.ReadCommentHandling == JsonCommentHandling.Skip
                ? JsonCommentHandling.Skip
                : JsonCommentHandling.Disallow,
        }, cancellationToken).ConfigureAwait(false);

        return new CorvusJsonParseNode(document.RootElement.Clone(), jsonOptions);
    }
}