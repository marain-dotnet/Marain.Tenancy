using Marain.Tenancy.Client;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(new TenancyClientOptions
{
    TenancyServiceBaseUri = new Uri("http://host.docker.internal:53658/v1.0/invoke/tenancy/method")
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