// <copyright file="List.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        private readonly ITenantProvider tenantProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="List"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider that will be used to retrieve the information.</param>
        public List(ITenantProvider tenantProvider)
        {
            this.tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Gets or sets the tenant whose children should be retrieved.
        /// </summary>
        [Option(CommandOptionType.SingleOrNoValue, ShortName = "t", LongName = "tenant", Description = "The Id of the tenant to retrieve children for. Leave blank to retrieve children of the root tenant.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not details should be loaded for the tenants.
        /// </summary>
        [Option(CommandOptionType.MultipleValue, ShortName = "p", LongName = "property", Description = "The names of properties to include.")]
        public string[] IncludeProperties { get; set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="app">The current <c>CommandLineApplication</c>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<int> OnExecute(CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(this.TenantId))
            {
                this.TenantId = this.tenantProvider.Root.Id;
            }

            string continuationToken = null;

            var childTenantIds = new List<string>();

            do
            {
                TenantCollectionResult children = await this.tenantProvider.GetChildrenAsync(this.TenantId, 2, continuationToken).ConfigureAwait(false);

                childTenantIds.AddRange(children.Tenants);

                continuationToken = children.ContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuationToken));

            if (this.IncludeProperties?.Length > 0)
            {
                await this.LoadAndOutputTenantDetailsAsync(childTenantIds, app.Out).ConfigureAwait(false);
            }
            else
            {
                this.OutputTenantIds(childTenantIds, app.Out);
            }

            return 0;
        }

        private async Task LoadAndOutputTenantDetailsAsync(List<string> children, TextWriter output)
        {
            IEnumerable<Task<ITenant>> detailsTasks = children.Select(x => this.tenantProvider.GetTenantAsync(x));

            await Task.WhenAll(detailsTasks).ConfigureAwait(false);

            var headings = new List<string> { "Id" };
            headings.AddRange(this.IncludeProperties);

            var table = new ConsoleTable(headings.ToArray());

            table.Options.OutputTo = output;
            table.Options.EnableCount = false;

            detailsTasks.ForEach(task =>
            {
                ITenant tenant = task.Result;

                var result = new List<string> { tenant.Id };

                foreach (string prop in this.IncludeProperties)
                {
                    tenant.Properties.TryGet<string>(prop, out string propValue);
                    result.Add(propValue ?? "{not set}");
                }

                table.AddRow(result.ToArray());
            });

            table.Write(Format.Minimal);
        }

        private void OutputTenantIds(List<string> children, TextWriter output)
        {
            output.WriteLine("Child Tenant Ids:");

            foreach (string current in children)
            {
                output.WriteLine($"\t{current}");
            }
        }
    }
}
