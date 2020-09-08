using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using Web.Code;

namespace web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"SettingsService version {PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
                Console.WriteLine("Is DEBUG");
#else
                Console.WriteLine("Is RELEASE");
#endif
                var host = Host.CreateDefaultBuilder(args)
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .UseSerilog()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureWebHostDefaults(hostBuilder =>
                    {
                        var sertConnString = Environment.GetEnvironmentVariable("CertConnectionString");
                        if (string.IsNullOrWhiteSpace(sertConnString) || sertConnString.Length < 10)
                        {
                            hostBuilder
                                .UseKestrel()
                                .UseUrls("http://*:5000/");
                        }
                        else
                        {
                            var xcert = CertificateLoader.Load(sertConnString);
                            hostBuilder
                                .UseKestrel(x =>
                                {
                                    x.Listen(IPAddress.Any, 443, listenOptions => listenOptions.UseHttps(xcert));
                                    x.AddServerHeader = false;
                                })
                                .UseUrls("https://*:443/");
                        }
                        hostBuilder.UseStartup<Startup>();
                    })
                    .Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");

                Task.WhenAny(
                        Task.Delay(delay),
                        Task.Run(() =>
                        {
                            Console.ReadKey(true);
                        }))
                    .Wait();
            }

            Console.WriteLine("Terminated");
        }
    }
}
