using System;
using GreenPipes;
using MassTransit;
using Messaging.InterfacesConstants.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OrdersApi.Messages.Consumers;
using OrdersApi.Persistence;
using OrdersApi.Services;

namespace OrdersApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IOrderRepository, OrderRepository>();

            services.AddDbContext<OrdersContext>(options => options.UseSqlServer
            (
                Configuration.GetConnectionString("OrdersContextConnection")
            ));

            AddMassTransit(services);

            services.AddHttpClient();
            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed((host) => true)
                    );
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrdersApi", Version = "v1" });
            });
        }

        private static void AddMassTransit(IServiceCollection services)
        {
            services.AddMassTransit(
                c =>
                {
                    c.AddConsumer<RegisterOrderCommandConsumer>();
                    c.AddConsumer<OrderDispatchedEventConsumer>();
                }
            );

            services.AddMassTransit(x =>
            {
                x.AddBus(busFactory =>
                {
                    var bus = Bus.Factory.CreateUsingRabbitMq(config =>
                    {
                        config.Host("localhost", "/", configurator =>
                        {
                            configurator.Username(RabbitMqMassTransitConstants.UserName);
                            configurator.Password(RabbitMqMassTransitConstants.Password);
                        });
                        config.ReceiveEndpoint(RabbitMqMassTransitConstants.RegisterOrderCommandQueue,
                           e =>
                           {
                               e.PrefetchCount = 16;
                               e.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(10)));
                               e.Consumer<RegisterOrderCommandConsumer>(busFactory);
                           });
                        config.ReceiveEndpoint(RabbitMqMassTransitConstants.OrderDispatchedServiceQueue,
                           e =>
                           {
                               e.PrefetchCount = 16;
                               e.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(10)));
                               e.Consumer<OrderDispatchedEventConsumer>(busFactory);
                           });

                        config.ConfigureEndpoints(busFactory);
                    });

                    return bus;
                });

            });

            services.AddSingleton<IHostedService, BusService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrdersApi v1"));
            }

            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
