// <copyright file="Create.cs" company="Endjin Limited">
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
    [Command(Name = "create", Description = "Create a new tenant.")]
    public class Create
    {
        private readonly ITenantProvider tenantProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Create"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider that will be used to create the new tenant.</param>
        public Create(ITenantProvider tenantProvider)
        {
            this.tenantProvider = tenantProvider;
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

            ITenant child = await this.tenantProvider.CreateChildTenantAsync(this.TenantId).ConfigureAwait(false);
            child.Properties.Set("name", this.Name);
            await this.tenantProvider.UpdateTenantAsync(child).ConfigureAwait(false);

            app.Out.WriteLine($"Created new child tenant with Id {child.Id} and name {this.Name}");
        }
    }
}
