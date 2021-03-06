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
    internal class InsultService : BackgroundService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly DiscordShardedClient _discord;
        private readonly IStringLocalizer<Insults> _localizer;

        private Timer _insultTimer;

        public InsultService(IServiceProvider services)
        {
            _services = services;
            _discord = services.GetRequiredService<DiscordShardedClient>();
            _localizer = services.GetRequiredService<IStringLocalizer<Insults>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _discord.UserJoined += UserJoined;

            Random random = new Random(DateTime.Now.Millisecond);
            _insultTimer = new Timer(InsultAsync, null, 0, Timeout.Infinite); //5-6 hours

            return Task.CompletedTask;
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var randomNum = random.Next(1, 70);
            var insultNum = "Insult" + randomNum;
            var insult = _localizer[insultNum].Value.Replace("{0}", user.Username);
            await user.Guild.SystemChannel.SendMessageAsync(insult);
        }

        private async void InsultAsync(object state)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var randomNum = random.Next(1, 70);
            var insultNum = "Insult" + randomNum;
            
            foreach (var guild in _discord.Guilds)
            {
                var channel = (IMessageChannel)guild.SystemChannel;
                var user = guild.Users.ElementAt(random.Next(guild.Users.Count()));
                var insult = _localizer[insultNum].Value.Replace("{0}", user.Mention);
                await channel.SendMessageAsync(insult);
            }

            var low = new TimeSpan(17, 0, 0); //17 hours
            var high = new TimeSpan(24, 0, 0); //24 hours
            var nextRun = random.Next((int)low.TotalMilliseconds, (int)high.TotalMilliseconds); //17-24 hours
            _insultTimer.Change(nextRun, Timeout.Infinite);
        }

        public void Dispose() 
        {
            base.Dispose();
        }
    }
}
