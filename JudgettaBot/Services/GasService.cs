﻿using Discord;
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
    internal class GasService : BackgroundService, IDisposable
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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            _gasTimer = new Timer(ExpelGasAsync, null, 0, Timeout.Infinite); 

            return Task.CompletedTask;
        }

        private async void ExpelGasAsync(object state)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var randomNum = random.Next(1, 11);
            var gasNum = "Gas" + randomNum;
            var gas = _localizer[gasNum].Value.Replace("{0}", _discord.CurrentUser.Username);

            foreach (var guild in _discord.Guilds)
            {
                var channelCount = guild.TextChannels.Count();
                var channel = (IMessageChannel)guild.TextChannels.ElementAt(random.Next(channelCount));
                await channel.SendMessageAsync("*" + gas + "*");
            }
            var low = new TimeSpan(6, 0, 0); //6 hours
            var high = new TimeSpan(24, 0, 0); //24 hours
            var nextRun = random.Next((int)low.TotalMilliseconds, (int)high.TotalMilliseconds); //6-24 hours
            _gasTimer.Change(nextRun, Timeout.Infinite);
        }

        public void Dispose()
        {
            _gasTimer?.Change(Timeout.Infinite, 0);
            _gasTimer?.Dispose();
            base.Dispose();
        }
    }
}
