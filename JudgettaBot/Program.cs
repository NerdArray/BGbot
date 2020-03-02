using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JudgettaBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JudgettaBot
{
    public class Program
    {
        private const int TOTAL_SHARDS = 1;

        private static string _rootPath;

        static async Task Main(string[] args)
        {
            // Explicitly set working directory.
            _rootPath = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(_rootPath);

            var config = new DiscordSocketConfig
            {
                TotalShards = TOTAL_SHARDS
            };

            using (var services = ConfigureServices(config))
            {
                var client = services.GetRequiredService<DiscordShardedClient>();
                
                client.ShardReady += ReadyAsync;
                client.Log += LogAsync;

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BGBOT_TOKEN"));
                await client.StartAsync();

                await Task.Delay(TimeSpan.FromSeconds(30));
                await services.GetRequiredService<GasService>().StartAsync(new CancellationToken());

                await services.GetRequiredService<InsultService>().StartAsync(new CancellationToken());

                await Task.Delay(-1);
            }
        }

        private static ServiceProvider ConfigureServices(DiscordSocketConfig config)
        {
            return new ServiceCollection()
                .AddLocalization()
                .Configure<RequestLocalizationOptions>(options =>
                {
                    var supportedCultures = new[]
                    {
                        new CultureInfo("en-US")
                    };

                    options.DefaultRequestCulture = new RequestCulture("en-US", "en-US");
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;
                })
                .AddSingleton(new DiscordShardedClient(config))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddHttpClient()
                .AddSingleton<JokeService>()
                .AddSingleton<GasService>()
                .AddSingleton<InsultService>()
                .BuildServiceProvider();
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
