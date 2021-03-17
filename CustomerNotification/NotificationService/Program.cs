using System;
using System.IO;
using System.Threading.Tasks;
using EmailService;
using GreenPipes;
using MassTransit;
using Messaging.InterfacesConstants.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Consumers;

namespace NotificationService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile($"appsettings.json", optional: false);
                    configHost.AddEnvironmentVariables();
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: false);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    AddEmailConfig(hostContext, services);
                    AddMassTransit(services);
                }
            );

            return hostBuilder;
        }

        private static void AddEmailConfig(HostBuilderContext hostContext, IServiceCollection services)
        {
            //Get and parse email config from appsettings
            var emailConfig = hostContext.Configuration
                                .GetSection("EmailConfiguration")
                                .Get<EmailConfig>();
            services.AddSingleton(emailConfig);
            services.AddScoped<IEmailSender, EmailSender>();
        }

        private static void AddMassTransit(IServiceCollection services)
        {
            services.AddMassTransit(c =>
            {
                c.AddConsumer<OrderProcessedEventConsumer>();
            });
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
                        config.ReceiveEndpoint(RabbitMqMassTransitConstants.NotificationServiceQueue,
                           e =>
                           {
                               e.PrefetchCount = 16;
                               e.UseMessageRetry(gpc => gpc.Interval(2, TimeSpan.FromSeconds(10)));
                               e.Consumer<OrderProcessedEventConsumer>(busFactory);
                           }
                       );

                        config.ConfigureEndpoints(busFactory);
                    });

                    return bus;
                });

            });

            services.AddSingleton<IHostedService, BusService>();
        }
    }
}
