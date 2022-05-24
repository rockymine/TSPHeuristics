using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Modulight.Modules.Hosting;
using StardustDL.RazorComponents.Markdown;
using Photino.Blazor;

namespace WebApplication {
    public class Program {
        public static void Main(string[] args)
        {
            try {
                Run(args);
            } catch (Exception ex) {

                throw;
            }
            
        }

        private static void Run(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args)
              => Console.WriteLine(args.ExceptionObject.ToString());

            var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

            builder.Services.AddLogging();
            builder.RootComponents.Add<App>("app");

            var app = builder.Build();

            app.MainWindow.SetTitle("Test");
            app.MainWindow.ContextMenuEnabled = true;
            app.MainWindow.DevToolsEnabled = true;
            app.MainWindow.GrantBrowserPermissions = false;

            app.MainWindow.Width = 800;
            app.MainWindow.Height = 600;

            app.MainWindow.Load("wwwroot/index.html");
            app.Run();
        }
    }
}
