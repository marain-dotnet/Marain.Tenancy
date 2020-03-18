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
        /// <param name="tenantProvider">The tenant provider that will be used to retrieve the information.</param>
        public Create(ITenantProvider tenantProvider)
        {
            this.tenantProvider = tenantProvider;
        }

        /// <summary>
        /// Gets or sets the tenant whose children should be retrieved.
        /// </summary>
        [Option(CommandOptionType.SingleOrNoValue, ShortName = "t", LongName = "tenant", Description = "The Id of the parent tenant.")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the tenant whose children should be retrieved.
        /// </summary>
        [Option(CommandOptionType.SingleValue, ShortName = "n", LongName = "name", Description = "The name of the tenant.")]
        public string Name { get; set; }

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

            ITenant child = await this.tenantProvider.CreateChildTenantAsync(this.TenantId).ConfigureAwait(false);
            child.Properties.Set("name", this.Name);
            await this.tenantProvider.UpdateTenantAsync(child);

            app.Out.Write("Created new child tenant with Id ");
            app.Out.Write(child.Id);
            app.Out.Write(" and name ");
            app.Out.WriteLine(this.Name);

            return 0;
        }
    }
}
