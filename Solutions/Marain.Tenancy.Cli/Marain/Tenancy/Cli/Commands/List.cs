// <copyright file="List.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Lists children of the specified tenant.
/// </summary>
public class List : AsyncCommand<ListSettings>
{
    private readonly ITenantStore tenantStore;
    private readonly IJsonSerializerOptionsProvider serializationSettingsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="List"/> class.
    /// </summary>
    /// <param name="tenantStore">The tenant store that will be used to retrieve the information.</param>
    /// <param name="serializerOptionsProvider">The serialization settings provider to use when writing output.</param>
    public List(ITenantStore tenantStore, IJsonSerializerOptionsProvider serializerOptionsProvider)
    {
        this.tenantStore = tenantStore;
        this.serializationSettingsProvider = serializerOptionsProvider;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, ListSettings settings)
    {
        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? this.tenantStore.Root.Id
            : settings.TenantId;

        string? continuationToken = null;

        var childTenantIds = new List<string>();

        do
        {
            TenantCollectionResult children = await this.tenantStore.GetChildrenAsync(
                tenantId,
                20,
                continuationToken).ConfigureAwait(false);

            childTenantIds.AddRange(children.Tenants);

            continuationToken = children.ContinuationToken;
        }
        while (!string.IsNullOrEmpty(continuationToken));

        if (settings.Name || settings.IncludeProperties?.Length > 0)
        {
            await this.LoadAndOutputTenantDetailsAsync(childTenantIds, settings).ConfigureAwait(false);
        }
        else
        {
            OutputTenantIds(childTenantIds);
        }

        return 0;
    }

    private static void OutputTenantIds(List<string> children)
    {
        AnsiConsole.MarkupLine("[bold]Child Tenant Ids:[/]");

        foreach (string current in children)
        {
            AnsiConsole.WriteLine($"  {current}");
        }
    }

    private async Task LoadAndOutputTenantDetailsAsync(List<string> children, ListSettings settings)
    {
        IEnumerable<Task<ITenant>> detailsTasks = children.Select(x => this.tenantStore.GetTenantAsync(x));

        ITenant[] tenants = await Task.WhenAll(detailsTasks).ConfigureAwait(false);

        var headings = new List<string> { "Id" };

        if (settings.Name)
        {
            headings.Add("Name");
        }

        if (settings.IncludeProperties != null)
        {
            headings.AddRange(settings.IncludeProperties);
        }

        var table = new Table();

        foreach (string heading in headings)
        {
            table.AddColumn(heading);
        }

        foreach (ITenant tenant in tenants)
        {
            var result = new List<string> { tenant.Id };

            if (settings.Name)
            {
                result.Add(tenant.Name);
            }

            if (settings.IncludeProperties != null)
            {
                foreach (string prop in settings.IncludeProperties)
                {
                    tenant.Properties.TryGet(prop, out JsonNode propValue);
                    result.Add(propValue.ToJsonString(this.serializationSettingsProvider.Instance) ?? "{not set}");
                }
            }

            table.AddRow(result.ToArray());
        }

        AnsiConsole.Write(table);
    }
}