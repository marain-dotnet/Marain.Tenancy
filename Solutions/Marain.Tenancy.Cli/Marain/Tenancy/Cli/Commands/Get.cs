// <copyright file="Get.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Extensions.Json;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using McMaster.Extensions.CommandLineUtils;

/// <summary>
/// Retrieves all details for the specified tenant.
/// </summary>
[Command(Name = "get", Description = "Gets tenant details.")]
public class Get
{
    private readonly ITenantProvider tenantProvider;
    private readonly IJsonSerializerOptionsProvider serializationSettingsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Get"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider that will be used to retrieve the information.</param>
    /// <param name="serializerOptionsProvider">The serialization settings provider to use when writing output.</param>
    public Get(ITenantProvider tenantProvider, IJsonSerializerOptionsProvider serializerOptionsProvider)
    {
        this.tenantProvider = tenantProvider;
        this.serializationSettingsProvider = serializerOptionsProvider;
    }

    /// <summary>
    /// Gets or sets the tenant whose details should be retrieved.
    /// </summary>
    [Option(
        CommandOptionType.SingleValue,
        ShortName = "t",
        LongName = "tenant",
        Description = "The Id of the tenant to retrieve details for.")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="app">The current <c>CommandLineApplication</c>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task OnExecute(CommandLineApplication app)
    {
        if (string.IsNullOrEmpty(this.TenantId))
        {
            this.TenantId = this.tenantProvider.Root.Id;
        }

        ITenant tenant = await this.tenantProvider.GetTenantAsync(this.TenantId).ConfigureAwait(false);

        string result = JsonSerializer.Serialize(
            tenant,
            this.serializationSettingsProvider.Instance);

        app.Out.WriteLine(result);
    }
}