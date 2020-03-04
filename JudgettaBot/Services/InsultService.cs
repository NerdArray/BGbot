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
    public class InsultService : IHostedService, IDisposable
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

        private async Task UserJoined(SocketGuildUser user)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var randomNum = random.Next(1, 70);
            var insultNum = "Insult" + randomNum;
            var insult = _localizer[insultNum].Value.Replace("{0}", user.Username);
            await user.Guild.SystemChannel.SendMessageAsync(insult);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.UserJoined += UserJoined;

            Random random = new Random(DateTime.Now.Millisecond);
            _insultTimer = new Timer(InsultAsync, null, 0, random.Next(18000000, 21600000)); //5-6 hours
            return Task.CompletedTask;
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

            var nextRun = random.Next(18000000, 21600000); //5-6 hours
            _insultTimer.Change(nextRun, nextRun);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
