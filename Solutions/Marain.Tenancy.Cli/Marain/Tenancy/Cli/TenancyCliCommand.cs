// <copyright file="TenancyCliCommand.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Marain.Tenancy.Cli.Commands;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Base class for CLI command implementations.
    /// </summary>
    [Command(Name = "tenancy")]
    [Subcommand(typeof(List))]
    [Subcommand(typeof(Create))]
    [Subcommand(typeof(Delete))]
    [Subcommand(typeof(Get))]
    public class TenancyCliCommand
    {
        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="app">The current <c>CommandLineApplication</c>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The command line framework requires this to be an instance method")]
        protected Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}