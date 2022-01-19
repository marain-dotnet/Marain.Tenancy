using Marain.Tenancy.Client;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Lookup dapr connection details
string daprHost = "localhost"; //Environment.GetEnvironmentVariable("HOSTNAME") ?? "localhost"; //"host.docker.internal";
string daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
string daprTenancyBaseUri = $"http://{daprHost}:{daprHttpPort}/v1.0/invoke/tenancy/method";
Console.WriteLine($"daprTenancyBaseUri: {daprTenancyBaseUri}");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(new TenancyClientOptions
{
    TenancyServiceBaseUri = new Uri(daprTenancyBaseUri)
});
builder.Services.AddTenancyClient(enableResponseCaching:false);
builder.Services.AddDaprClient();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
