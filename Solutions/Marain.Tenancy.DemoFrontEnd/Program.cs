using Marain.Tenancy.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
TenancyClientOptions options = new TenancyClientOptions();
var daprHost = Environment.GetEnvironmentVariable("HOSTNAME");
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
options.TenancyServiceBaseUri = new Uri($"http://{daprHost}:{daprHttpPort}/v1.0/invoke/tenancy/method");

builder.Services.AddSingleton(options);
builder.Services.AddTenancyClient(enableResponseCaching:false);

builder.Services.AddDaprClient();

var app = builder.Build();

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
