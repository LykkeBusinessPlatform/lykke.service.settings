using System;
using System.Globalization;
using System.Threading.Tasks;
using Autofac;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.Log;
using Lykke.Logs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Web.Middleware;
using Web.Models;
using Web.Modules;
using Web.Code;
using Web.Settings;
using Microsoft.OpenApi.Models;

namespace web
{
    public class Startup
    {
        private const string ApiName = "SettingsServiceV2 API";
        private AppSettings _appSettings;

        public IConfigurationRoot Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            _appSettings = Configuration.Get<AppSettings>();

            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                opts.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
            {
                o.LoginPath = new PathString("/Account/SignIn");
                o.ExpireTimeSpan = TimeSpan.FromMinutes(_appSettings.UserLoginTime);
            });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddMemoryCache();

            services.AddControllersWithViews(); //services.AddRazorPages();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = ApiName, Version = "v1" });
                c.CustomSchemaIds(type => type.ToString());
                c.OperationFilter<ApiKeyHeaderOperationFilter>();
            });

            ConfigureLogging(services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AppModule(_appSettings));
            builder.RegisterModule(new DbModule(_appSettings));
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/home/error/{0}");
            }

            app.UseLykkeMiddleware(ex => new ErrorResponse { ErrorMessage = "Technical problem" });

            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();

            app.UseSwaggerUI(x =>
            {
                x.RoutePrefix = "swagger/ui";
                x.SwaggerEndpoint("/swagger/v1/swagger.json", ApiName);
                x.EnableDeepLinking();
            });

            var cultureInfo = new CultureInfo("en-US");

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            appLifetime.ApplicationStarted.Register(() => StartApplicationAsync(app.ApplicationServices).GetAwaiter().GetResult());
            appLifetime.ApplicationStopped.Register(() => CleanUp(app.ApplicationServices));
        }

        private async Task StartApplicationAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var selfTestService = serviceProvider.GetRequiredService<SelfTestService>();
                await selfTestService.SelfTestAsync();

                var healthNotifier = serviceProvider.GetService<IHealthNotifier>();
                if (healthNotifier != null)
                    healthNotifier.Notify("Starting");
            }
            catch (Exception ex)
            {
                var log = serviceProvider.GetService<ILogFactory>().CreateLog(this);
                log.Critical(ex);

                throw;
            }
        }

        private void CleanUp(IServiceProvider serviceProvider)
        {
            try
            {
                var healthNotifier = serviceProvider.GetService<IHealthNotifier>();
                if (healthNotifier != null)
                    healthNotifier.Notify("Terminating");
            }
            catch (Exception ex)
            {
                var log = serviceProvider.GetService<ILogFactory>().CreateLog(this);
                if (log != null)
                {
                    log.Critical(ex);
                    (log as IDisposable)?.Dispose();
                }

                throw;
            }
        }

        private void ConfigureLogging(IServiceCollection services)
        {
            services.AddLykkeLogging();

            var serilogConfigurator = new SerilogConfigurator();
#if DEBUG
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("serilogsettings.json", optional: false);
            serilogConfigurator.AddFromConfiguration(configBuilder.Build());
#else
            if (!string.IsNullOrWhiteSpace(_appSettings.Db.ConnectionString))
                serilogConfigurator.AddAzureTable(
                    _appSettings.Db.ConnectionString,
                    "SettingsServiceLog");

            if (!string.IsNullOrWhiteSpace(_appSettings.SlackNotificationsConnString)
                && !string.IsNullOrWhiteSpace(_appSettings.SlackNotificationsQueueName))
                serilogConfigurator.AddAzureQueue(
                    _appSettings.SlackNotificationsConnString,
                    _appSettings.SlackNotificationsQueueName);
#endif
            serilogConfigurator.Configure();
        }
    }
}
