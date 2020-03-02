using Discord;
using Discord.WebSocket;
using JudgettaBot.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JudgettaBot.Services
{
    public class GasService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly DiscordShardedClient _discord;
        private readonly IStringLocalizer<Gas> _localizer;

        private Timer _gasTimer;

        public GasService(IServiceProvider services)
        {
            _services = services;
            _discord = services.GetRequiredService<DiscordShardedClient>();
            _localizer = services.GetRequiredService<IStringLocalizer<Gas>>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            _gasTimer = new Timer(ExpelGasAsync, null, 0, random.Next(3600000, 7200000)); //1-2 hours

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _gasTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void ExpelGasAsync(object state)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var randomNum = random.Next(1, 9);
            var gasNum = "Gas" + randomNum;
            var gas = _localizer[gasNum].Value.Replace("{0}", _discord.CurrentUser.Username);

            foreach (var guild in _discord.Guilds)
            {
                var channelCount = guild.TextChannels.Count();
                var channel = (IMessageChannel)guild.TextChannels.ElementAt(random.Next(channelCount));
                await channel.SendMessageAsync("*" + gas + "*");
            }
            var nextRun = random.Next(3600000, 7200000); //1-2 hours;
            _gasTimer.Change(nextRun, nextRun);
        }

        public void Dispose()
        {
            _gasTimer?.Dispose();
        }
    }
}
