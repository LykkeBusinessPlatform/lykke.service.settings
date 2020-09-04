using System;
using System.Globalization;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.SettingsReader.ReloadingManager;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Settings;
using Web.Middleware;
using Web.Models;
using Web.Modules;

namespace web
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; set; }
        public ILog Log { get; private set; }
        public IHealthNotifier HealthNotifier { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                var appSettings = Configuration.Get<AppSettings>();

                services.AddAuthentication(opts =>
                {
                    opts.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opts.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
                {
                    o.LoginPath = new PathString("/Account/SignIn");
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(int.Parse(Configuration["UserLoginTime"]));
                });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
                });

                services.AddMemoryCache();

                services.AddMvc();

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", "SettingsServiceV2 API");
                    options.OperationFilter<ApiKeyHeaderOperationFilter>();
                });

                var builder = new ContainerBuilder();

                var settings = ConstantReloadingManager.From(appSettings);

                builder.RegisterModule(new AppModule(settings));
                builder.RegisterModule(new DbModule(settings));
                //builder.RegisterModule(new RabbitModule(settings, Log, _consoleLogger));
                builder.Populate(services);

                var provider = new AnnotationsBasedMetamodelProvider();
                EntityMetamodel.Configure(provider);

                ApplicationContainer = builder.Build();

                Log = ApplicationContainer.Resolve<ILogFactory>().CreateLog(this);
                HealthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
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

                app.UseAuthentication();
                app.UseStaticFiles();

                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Home}/{action=Index}/{id?}");
                });

                app.UseSwagger();

                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                });

                var cultureInfo = new CultureInfo("en-US");

                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

                appLifetime.ApplicationStarted.Register(StartApplication);
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);
                throw;
            }
        }

        private void StartApplication()
        {
            try
            {
                if (Log != null)
                    HealthNotifier.Notify("Starting");
            }
            catch (Exception ex)
            {
                Log?.Critical(ex);

                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                if (Log != null)
                    HealthNotifier.Notify("Terminating");

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    Log?.Critical(ex);
                    (Log as IDisposable)?.Dispose();
                }

                throw;
            }
        }
    }
}
