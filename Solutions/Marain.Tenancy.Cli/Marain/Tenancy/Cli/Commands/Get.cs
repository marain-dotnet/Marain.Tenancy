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
public class Get(ITenantProvider tenantProvider, IJsonSerializerOptionsProvider serializationSettingsProvider) : AsyncCommand<GetSettings>
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, GetSettings settings)
    {
        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? tenantProvider.Root.Id
            : settings.TenantId;

        ITenant tenant = await tenantProvider.GetTenantAsync(tenantId).ConfigureAwait(false);

        string result = JsonSerializer.Serialize(
            tenant,
            serializationSettingsProvider.Instance);

        AnsiConsole.WriteLine(result);

        return 0;
    }
}