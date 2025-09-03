// <copyright file="Get.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Retrieves all details for the specified tenant.
/// </summary>
public class Get : AsyncCommand<GetSettings>
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
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, GetSettings settings)
    {
        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? this.tenantProvider.Root.Id
            : settings.TenantId;

        ITenant tenant = await this.tenantProvider.GetTenantAsync(tenantId).ConfigureAwait(false);

        string result = JsonSerializer.Serialize(
            tenant,
            this.serializationSettingsProvider.Instance);

        AnsiConsole.WriteLine(result);

        return 0;
    }
}