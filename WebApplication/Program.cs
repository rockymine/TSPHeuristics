using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Modulight.Modules.Hosting;
using StardustDL.RazorComponents.Markdown;

namespace WebApplication {
    public class Program {
        public static async Task Main(string[] args) {
            /* Blazor takes the culture from the browser, and every number on the page goes through
             * ToString(): in a German browser the formulas read "0, 98 · 0, 768" and the inputs
             * expected decimal commas while the graph text uses points. One culture for the whole
             * app keeps numbers, formulas and the graph format consistent. */
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddModules(builder => {
                builder.UseRazorComponentClientModules().AddMarkdownModule();
            });

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsyncWithModules();
        }
    }
}
