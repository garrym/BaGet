using System;
using System.Net;
using System.Threading;
using BaGet.Core;
using BaGet.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BaGet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "baget",
                Description = "A light-weight NuGet service",
            };

            app.HelpOption(inherited: true);

            app.Command("import", import =>
            {
                import.Command("downloads", downloads =>
                {
                    downloads.OnExecute(async () =>
                    {
                        var provider = CreateHostBuilder(args).Build().Services;

                        await provider
                            .GetRequiredService<DownloadsImporter>()
                            .ImportAsync(CancellationToken.None);
                    });
                });
            });

            app.OnExecute(() =>
            {
                CreateWebHostBuilder(args).Build().Run();
            });

            app.Execute(args);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    // Remove the upload limit from Kestrel. If needed, an upload limit can
                    // be enforced by a reverse proxy server, like IIS.
                    options.Limits.MaxRequestBodySize = null;
                    options.Listen(IPAddress.Loopback, 443, listenOptions =>
                    {
                        listenOptions.UseHttps(Environment.GetEnvironmentVariable("TLS_CERTIFICATE_PATH"), Environment.GetEnvironmentVariable("TLS_CERTIFICATE_PASSWORD"));
                    });
                })
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var root = Environment.GetEnvironmentVariable("BAGET_CONFIG_ROOT");
                    if (!string.IsNullOrEmpty(root))
                        config.SetBasePath(root);
                });

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return new HostBuilder()
                .ConfigureBaGetConfiguration(args)
                .ConfigureBaGetServices()
                .ConfigureBaGetLogging();
        }
    }
}
