using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection serviceCollection = new();

TenancyClientOptions options = new()
{
    TenancyServiceBaseUri = "http://localhost:5000",
};

serviceCollection.AddSingleton(options);
serviceCollection.AddTenancyClient(false);

ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

ITenancyService client = serviceProvider.GetRequiredService<ITenancyService>();

const string RootTenantId = "f26450ab1668784bb327951c8b08f347";

Tenant rootTenant = await client.GetTenantAsync(RootTenantId).ConfigureAwait(false);
Console.WriteLine($"Root tenant: {rootTenant.Name}");

GetChildrenResult childrenResult = await client.GetChildrenAsync(rootTenant.Id).ConfigureAwait(false);
Console.WriteLine($"Children: {childrenResult.Children.Count}");

Console.ReadLine();
