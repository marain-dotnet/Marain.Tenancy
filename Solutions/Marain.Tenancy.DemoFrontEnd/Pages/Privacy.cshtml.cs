namespace Marain.Tenancy.DemoFrontEnd.Pages
{
    using Marain.Tenancy.Client;
    using Marain.Tenancy.Client.Models;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Rest;

    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;
        private readonly ITenancyService _client;

        public PrivacyModel(ILogger<PrivacyModel> logger, ITenancyService client)
        {
            _logger = logger;
            this._client = client;
        }

        public async Task OnGet()
        {
            var tenant = await this._client.GetTenantAsync("f26450ab1668784bb327951c8b08f347");
            
            ViewData["tenant"] = tenant;
        }
    }
}