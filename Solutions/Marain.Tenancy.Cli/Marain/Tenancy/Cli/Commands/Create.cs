// <copyright file="Create.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands
{
    using System;
    using System.Threading.Tasks;
    using Corvus.Tenancy;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    [Command(Name = "create", Description = "Create a new tenant.")]
    public class Create
    {
        private readonly ITenantStore tenantStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="Create"/> class.
        /// </summary>
        /// <param name="tenantStore">The tenant store that will be used to create the new tenant.</param>
        public Create(ITenantStore tenantStore)
        {
            this.tenantStore = tenantStore;
        }

        /// <summary>
        /// Gets or sets the Id of the tenant that should be the parent of the new tenant.
        /// </summary>
        /// <remarks>
        /// If ommitted, the tenant will be created at the top level, as a child of the root.
        /// </remarks>
        [Option(
            CommandOptionType.SingleOrNoValue,
            ShortName = "t",
            LongName = "tenant",
            Description = "The Id of the parent tenant. Omit if the child should be a parent of the root tenant.")]
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the name of the new tenant.
        /// </summary>
        [Option(
            CommandOptionType.SingleValue,
            ShortName = "n",
            LongName = "name",
            Description = "The name of the new tenant.")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the well-known GUID of the new tenant.
        /// </summary>
        [Option(
            CommandOptionType.SingleValue,
            ShortName = "g",
            LongName = "guid",
            Description = "The well-known GUID of the new tenant.")]
        public string? WellKnownTenantGuid { get; set; }

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

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new InvalidOperationException("Name must be supplied");
            }

            Guid wellKnownGuid = string.IsNullOrEmpty(this.WellKnownTenantGuid)
                ? Guid.NewGuid()
                : Guid.Parse(this.WellKnownTenantGuid);

            ITenant child = await this.tenantStore.CreateWellKnownChildTenantAsync(
                this.TenantId,
                wellKnownGuid,
                this.Name).ConfigureAwait(false);

            app.Out.WriteLine($"Created new child tenant with Id {child.Id} and name {child.Name}");
        }
    }
}
