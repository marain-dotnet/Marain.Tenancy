﻿// <copyright file="List.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ConsoleTables;
    using Corvus.Tenancy;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Lists children of the specified tenant.
    /// </summary>
    [Command(Name = "list", Description = "List tenants.")]
    public class List
    {
        private readonly ITenantStore tenantStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="List"/> class.
        /// </summary>
        /// <param name="tenantStore">The tenant store that will be used to retrieve the information.</param>
        public List(ITenantStore tenantStore)
        {
            this.tenantStore = tenantStore;
        }

        /// <summary>
        /// Gets or sets the tenant whose children should be retrieved.
        /// </summary>
        [Option(
            CommandOptionType.SingleOrNoValue,
            ShortName = "t",
            LongName = "tenant",
            Description = "The Id of the tenant to retrieve children for. Leave blank to retrieve children of the root tenant.")]
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tenant name should be output.
        /// </summary>
        [Option(
            CommandOptionType.NoValue,
            ShortName = "n",
            LongName = "name",
            Description = "Indicates that the tenant name should be displayed")]
        public bool Name { get; set; }

        /// <summary>
        /// Gets or sets a value containing the names of specific properties that should be included in the output.
        /// </summary>
        [Option(
            CommandOptionType.MultipleValue,
            ShortName = "p",
            LongName = "property",
            Description = "The names of tenant properties to include in the output. If omitted, only the tenant Ids will be listed.")]
        public string[]? IncludeProperties { get; set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="app">The current <c>CommandLineApplication</c>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(this.TenantId))
            {
                this.TenantId = this.tenantStore.Root.Id;
            }

            string? continuationToken = null;

            var childTenantIds = new List<string>();

            do
            {
                TenantCollectionResult children = await this.tenantStore.GetChildrenAsync(
                    this.TenantId,
                    20,
                    continuationToken).ConfigureAwait(false);

                childTenantIds.AddRange(children.Tenants);

                continuationToken = children.ContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuationToken));

            if (this.Name || this.IncludeProperties?.Length > 0)
            {
                await this.LoadAndOutputTenantDetailsAsync(childTenantIds, app.Out).ConfigureAwait(false);
            }
            else
            {
                OutputTenantIds(childTenantIds, app.Out);
            }
        }

        private static void OutputTenantIds(List<string> children, TextWriter output)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Child Tenant Ids:");

            foreach (string current in children)
            {
                builder.Append('\t').AppendLine(current);
            }

            output.WriteLine(builder.ToString());
        }

        private async Task LoadAndOutputTenantDetailsAsync(List<string> children, TextWriter output)
        {
            IEnumerable<Task<ITenant>> detailsTasks = children.Select(x => this.tenantStore.GetTenantAsync(x));

            ITenant[] tenants = await Task.WhenAll(detailsTasks).ConfigureAwait(false);

            var headings = new List<string> { "Id" };

            if (this.Name)
            {
                headings.Add("Name");
            }

            if (this.IncludeProperties != null)
            {
                headings.AddRange(this.IncludeProperties);
            }

            var table = new ConsoleTable(headings.ToArray());

            table.Options.OutputTo = output;
            table.Options.EnableCount = false;

            foreach (ITenant tenant in tenants)
            {
                var result = new List<string> { tenant.Id };

                if (this.Name)
                {
                    result.Add(tenant.Name);
                }

                if (this.IncludeProperties != null)
                {
                    foreach (string prop in this.IncludeProperties)
                    {
                        tenant.Properties.TryGet(prop, out string propValue);
                        result.Add(propValue ?? "{not set}");
                    }
                }

                table.AddRow(result.ToArray());
            }

            table.Write(Format.Minimal);
        }
    }
}