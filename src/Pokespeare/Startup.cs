using FunTranslationsApi.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using PokeApi.Client;
using Pokespeare.Common;
using Pokespeare.ConfigModel;
using Pokespeare.Services;
using Refit;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

#pragma warning disable CA1812 // This class is supposed to be never instantiated directly
#pragma warning disable CA1822 // We don't want configuration methods to be static
namespace Pokespeare
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHealthChecks();
            services.AddSwaggerGen(c =>
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                var xmlFile = $"{assemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pokespeare", Version = "v1" });
            });

            services.Configure<PokeApiConfig>(c => Configuration.GetSection("PokeApi").Bind(c));
            services.Configure<FunTranslationsConfig>(c => Configuration.GetSection("FunTranslations").Bind(c));
            services.AddScoped<IPokemonRepository, PokemonRepository>();
            services.AddScoped<IPokemonDescriptionService, PokemonDescriptionService>();
            services.AddScoped<ITranslationService, TranslationService>();

            ConfigureHttpClients(services);

            var redisConnectionString = Configuration.GetConnectionString("Redis__");
            if (redisConnectionString is null)
            {
                services.AddSingleton<IDistributedCache>(new NullDistributedCache());
            }
            else
            {
                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = redisConnectionString;
                });
            }
        }

        private static void ConfigureHttpClients(IServiceCollection services)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
            };

            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonOptions)
            };

            var versionString = typeof(Startup).Assembly.GetName().Version?.ToString(2);
            var userAgent = versionString switch
            {
                null => "Pokespeare",
                _ => $"Pokespeare/{versionString}"
            };

            services.AddRefitClient<IPokeApi>(refitSettings)
                .ConfigureHttpClient((s, c) =>
                {
                    var cfg = s.GetRequiredService<IOptions<PokeApiConfig>>();
                    c.BaseAddress = cfg.Value!.BaseUrl;
                    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                })
                .ConfigureHttpMessageHandlerBuilder(b =>
                {
                    var innerHandler = new SocketsHttpHandler();
                    var policyHandler = ActivatorUtilities.CreateInstance<PolicyHandler>(b.Services, innerHandler);

                    b.PrimaryHandler = policyHandler;
                });

            services.AddRefitClient<IFunTranslationsApi>(refitSettings)
                .ConfigureHttpClient((s, c) =>
                {
                    var cfg = s.GetRequiredService<IOptions<FunTranslationsConfig>>();
                    c.BaseAddress = cfg.Value.BaseUrl;
                    c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                })
                .ConfigureHttpMessageHandlerBuilder(b =>
                {
                    var innerHandler = new SocketsHttpHandler();
                    var policyHandler = ActivatorUtilities.CreateInstance<PolicyHandler>(b.Services, innerHandler);

                    b.PrimaryHandler = policyHandler;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pokespeare v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthy");
                endpoints.MapControllers();
            });
        }
    }
}
