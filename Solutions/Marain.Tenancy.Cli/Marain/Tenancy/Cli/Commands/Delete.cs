// <copyright file="Delete.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands
{
    using System;
    using System.Threading.Tasks;
    using Corvus.Tenancy;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Deletes a tenant.
    /// </summary>
    [Command(Name = "delete", Description = "Deletes a tenant.")]
    public class Delete
    {
        private readonly ITenantStore tenantStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="Delete"/> class.
        /// </summary>
        /// <param name="tenantStore">The tenant store that will be used to delete the tenant.</param>
        public Delete(ITenantStore tenantStore)
        {
            this.tenantStore = tenantStore;
        }

        /// <summary>
        /// Gets or sets the Id of the tenant to be deleted.
        /// </summary>
        [Option(
            CommandOptionType.SingleValue,
            ShortName = "t",
            LongName = "tenant",
            Description = "The Id of the parent tenant.")]
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
                throw new ArgumentException("Tenant Id must be provided.");
            }

            TenantCollectionResult children = await this.tenantStore.GetChildrenAsync(this.TenantId, 1).ConfigureAwait(false);

            if (children.Tenants.Count > 0)
            {
                app.Error.WriteLine(
                    $"Cannot delete tenant with Id {this.TenantId} as it has children. Remove the child tenants first.");

                return;
            }

            await this.tenantStore.DeleteTenantAsync(this.TenantId).ConfigureAwait(false);

            app.Out.WriteLine("Deleted tenant with Id " + this.TenantId);
        }
    }
}