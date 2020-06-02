using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JudgettaBot.Models.Options;
using JudgettaBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace JudgettaBot
{
    public class Program
    {
        private const int TOTAL_SHARDS = 1;

        private static IHost _host;
        private static string _rootPath;

        public static async Task Main(string[] args)
        {
            // Explicitly set working directory.
            _rootPath = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(_rootPath);

            var config = new DiscordSocketConfig
            {
                TotalShards = TOTAL_SHARDS
            };

            _host = new HostBuilder()
                .UseContentRoot(_rootPath)
                .ConfigureAppConfiguration(hostConfig =>
                {
                    hostConfig.SetBasePath(_rootPath);
                    hostConfig.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    hostConfig.AddJsonFile("timestore.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((hostContext, logger) =>
                {
                    // Clear default log providers.
                    logger.ClearProviders();

                    // Set log providers.
                    logger.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    logger.AddConsole();
                    logger.AddDebug();
                    logger.AddEventLog();
                    logger.AddEventSourceLogger();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLocalization().Configure<RequestLocalizationOptions>(options =>
                    {
                        var supportedCultures = new[]
                        {
                            new CultureInfo("en-US")
                        };

                        options.DefaultRequestCulture = new RequestCulture("en-US", "en-US");
                        options.SupportedCultures = supportedCultures;
                        options.SupportedUICultures = supportedCultures;
                    });

                    services.Configure<TimerOptions>(hostContext.Configuration);
                    services.AddScoped(cfg => cfg.GetService<IOptionsSnapshot<TimerOptions>>().Value);

                    services.AddSingleton(new DiscordShardedClient(config));
                    services.AddSingleton<CommandService>();
                    services.AddSingleton<CommandHandlingService>();
                    services.AddHttpClient();
                    services.AddSingleton<TimerService>();
                    services.AddSingleton<JokeService>();
                    services.AddHostedService<GasService>();
                    services.AddHostedService<InsultService>();
                })
                .UseConsoleLifetime()
                .Build();

            var client = _host.Services.GetRequiredService<DiscordShardedClient>();
            client.ShardReady += ReadyAsync;
            client.Log += LogAsync;

            await _host.Services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BGBOT_TOKEN"));
            await client.StartAsync();

            await Task.Delay(30000);

            await _host.Services.GetRequiredService<TimerService>().LoadSavedTimers();

            await _host.RunAsync(new System.Threading.CancellationToken()).ConfigureAwait(false);
        }

        private static Task ReadyAsync(DiscordSocketClient shard)
        {
            Console.WriteLine($"Shard Number {shard.ShardId} is connected and ready!");
            return Task.CompletedTask;
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
