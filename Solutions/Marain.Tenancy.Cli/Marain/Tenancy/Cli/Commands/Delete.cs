// <copyright file="Delete.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands
{
    using System.Threading.Tasks;
    using Corvus.Tenancy;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    [Command(Name = "delete", Description = "Deletes a tenant.")]
    public class Delete
    {
        private readonly ITenantProvider tenantProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Delete"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider that will be used to delete the tenant.</param>
        public Delete(ITenantProvider tenantProvider)
        {
            this.tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Gets or sets the Id of the tenant to be deleted.
        /// </summary>
        [Option(
            CommandOptionType.SingleValue,
            ShortName = "t",
            LongName = "tenant",
            Description = "The Id of the parent tenant.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="app">The current <c>CommandLineApplication</c>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<int> OnExecute(CommandLineApplication app)
        {
            TenantCollectionResult children = await this.tenantProvider.GetChildrenAsync(this.TenantId, 1);

            if (children.Tenants.Count > 0)
            {
                app.Error.WriteLine(
                    $"Cannot delete tenant with Id {this.TenantId} as it has children. Remove the child tenants first.");
                return -1;
            }

            await this.tenantProvider.DeleteTenantAsync(this.TenantId).ConfigureAwait(false);

            app.Out.Write("Deleted tenant with Id ");
            app.Out.WriteLine(this.TenantId);

            return 0;
        }
    }
}
