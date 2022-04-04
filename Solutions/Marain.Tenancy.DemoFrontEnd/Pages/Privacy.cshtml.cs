namespace Marain.Tenancy.DemoFrontEnd.Pages
{
    using Marain.Tenancy.Client;

    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> logger;
        private readonly ITenancyService client;

        public PrivacyModel(ILogger<PrivacyModel> logger, ITenancyService client)
        {
            this.logger = logger;
            this.client = client;
        }

        public async Task OnGet()
        {
            var tenant = await this.client.GetTenantAsync("f26450ab1668784bb327951c8b08f347");

            this.ViewData["tenant"] = tenant;
        }
    }
}