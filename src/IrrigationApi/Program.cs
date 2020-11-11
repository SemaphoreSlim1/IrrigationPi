using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Diagnostics.CodeAnalysis;

namespace IrrigationApi
{
    [ExcludeFromCodeCoverage] //there's not a real easy way to unit test the application's entry point
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var hostBuilder = Host.CreateDefaultBuilder(args)
                                       .UseSerilog()
                                      .ConfigureWebHostDefaults(webBuilder =>
                                      {
                                          webBuilder.UseStartup<Startup>();
                                      });

                var host = hostBuilder.Build();
                var configRoot = host.Services.GetRequiredService<IConfiguration>();

                Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(configRoot)
                            .CreateLogger();

                Log.Information("Starting web host");
                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }
    }
}
