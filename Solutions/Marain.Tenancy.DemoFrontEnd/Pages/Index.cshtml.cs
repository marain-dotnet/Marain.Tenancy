﻿using Dapr.Client;
using Marain.Tenancy.Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marain.Tenancy.DemoFrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly DaprClient daprClient;

        public IndexModel(ILogger<IndexModel> logger, DaprClient daprClient)
        {
            _logger = logger;
            this.daprClient = daprClient;
        }

        public async Task OnGet()
        {
            try
            {
                var tenants = await daprClient.InvokeMethodAsync<Tenant>(
                                    HttpMethod.Get,
                                    "tenancy",
                                    "f26450ab1668784bb327951c8b08f347/marain/tenant");


                ViewData["tenants"] = tenants;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}