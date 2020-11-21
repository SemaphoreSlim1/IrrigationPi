using IrrigationApi.ApplicationCore;
using IrrigationApi.ApplicationCore.Configuration;
using IrrigationApi.ApplicationCore.Health;
using IrrigationApi.ApplicationCore.Threading;
using IrrigationApi.Backround;
using IrrigationApi.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Device.Gpio;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace IrrigationApi
{
    [ExcludeFromCodeCoverage] //there's not a real easy way to unit test bootstrapping the application or registering the application services
    public class Startup
    {
        private readonly IConfiguration _configRoot;

        public Startup(IConfiguration configRoot)
        {
            _configRoot = configRoot;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<IrrigationConfig>(_configRoot.GetSection("Irrigation"));
            services.Configure<HardwareConfig>(_configRoot.GetSection("HardwareDriver"));

            var channel = Channel.CreateUnbounded<IrrigationJob>(
                new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = false,
                    SingleWriter = false,
                    SingleReader = true
                });

            services.AddSingleton(channel);
            services.AddTransient(sp =>
            {
                var resolvedChannel = sp.GetRequiredService<Channel<IrrigationJob>>();
                return channel.Reader;
            });
            services.AddTransient(sp =>
            {
                var resolvedChannel = sp.GetRequiredService<Channel<IrrigationJob>>();
                return channel.Writer;
            });

            var driverSettings = new HardwareConfig();
            _configRoot.GetSection("HardwareDriver").Bind(driverSettings);

            if (driverSettings.UseMemoryDriver)
            { services.AddSingleton(new GpioController(PinNumberingScheme.Logical, new MemoryGpioDriver(pinCount: 50))); }
            else
            { services.AddSingleton(new GpioController()); }

            services.AddHostedService<PinInitializer>();
            services.AddHostedService<IrrigationProcessor>();

            services.AddSingleton<IIrrigationStopper, IrrigationStopper>();
            services.AddSingleton<IrrigationProcessorStatus>();

            services.AddControllers().AddNewtonsoftJson();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Irrigation API", Version = "v1" });
            });

            services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("Application", tags: new[] { "application" })
            .AddCheck<IrrigationProcessorHealthCheck>("Irrigation", tags: new[] { "irrigation" });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapSwagger();

                endpoints.MapHealthChecks("/Heartbeat", new HealthCheckOptions()
                {
                    Predicate = (check) =>
                    {
                        return check.Name == "Application";
                    },
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status200OK,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                    }
                });

                //This route executes all registered health checks - it MAY reach out to downstream systems
                endpoints.MapHealthChecks("/Health", new HealthCheckOptions()
                {
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status200OK,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                    }
                });
            });

            //redirect the root to swagger
            var rewriteOption = new RewriteOptions();
            rewriteOption.AddRedirect("^$", "swagger");
            app.UseRewriter(rewriteOption);

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Irrigation API");
            });
        }
    }
}
