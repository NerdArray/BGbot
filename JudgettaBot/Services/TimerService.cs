using Discord;
using Discord.WebSocket;
using JudgettaBot.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgettaBot.Services
{
    public class TimerService
    {
        private readonly DiscordShardedClient _client;
        private readonly IServiceProvider _services;

        private List<DiscordTimer> _timers;

        public TimerService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _services = services;

            _timers = new List<DiscordTimer>();
        }

        public async Task StartTimer(SocketUser user, ISocketMessageChannel channel, TimeSpan span)
        {
            await Task.Run(() =>
            {
                var timer = new DiscordTimer(user, channel, span);
                timer.TimerTicked += Timer_TimerTicked;
                timer.Start();
                _timers.Add(timer);
            });
        }

        private async void Timer_TimerTicked(DiscordTimer discordTimer)
        {
            // stop the timer while we're working.
            discordTimer.Stop();

            // calculate how much time is left until the timer expires.
            var timeLeft = discordTimer.EndTime - DateTime.Now;

            // check if the timer has expired.
            if (timeLeft.TotalSeconds <= 0)
            {
                try
                {
                    // DM the user to let them know their timer has expired.
                    await discordTimer.User.SendMessageAsync("Your timer has expired!");
                }
                catch
                {
                    // Something went wrong.  Send the message to the channel the timer request originated from.
                    await discordTimer.Channel.SendMessageAsync(discordTimer.User.Username + "'s timer expired, but something is preventing me from sending them a DM to let them know.");
                }

                // clean up this timer.
                discordTimer.Timer.Stop();
                discordTimer.Timer.Dispose();
                _timers.Remove(discordTimer);
                //await _db.Deletetimer(timerTimer.DbId);
                discordTimer.Dispose();

                return;
            }

            // track the time until the next notification
            TimeSpan timeUntilNotification;

            // more than an hour until the timer expires.
            if (timeLeft.TotalMinutes >= 60)
            {
                // notify again in an hour
                discordTimer.NextNotificationTime = discordTimer.NextNotificationTime.AddHours(1);
                // set the timer interval.
                timeUntilNotification = discordTimer.NextNotificationTime - DateTime.Now;
                discordTimer.Timer.Interval = timeUntilNotification.TotalMilliseconds;
                // start the timer.
                discordTimer.Start();

                //await _db.UpdateShield(shieldTimer.DbId, shieldTimer.NextNotificationTime);

                try
                {
                    // DM the user an update on their timer.
                    await discordTimer.User.SendMessageAsync("Your timer expires in " + timeLeft.Hours + " hours " + timeLeft.Minutes + " minutes.");
                }
                catch
                {
                    // Something went wrong.  Send the message to the channel the timer request originated from.
                    await discordTimer.Channel.SendMessageAsync(discordTimer.User.Username + "'s timer expires in " + timeLeft.Hours + " hours " + timeLeft.Minutes + " minutes, but something is preventing us from sending them a DM to let them know.");
                }

                return;
            }

            // More than 30 minutes until the timer expires.
            if (timeLeft.TotalMinutes >= 30)
            {
                discordTimer.NextNotificationTime = discordTimer.NextNotificationTime.AddMinutes(30);
            }

            // More than 15 minutes until the timer expires.
            if (timeLeft.TotalMinutes >= 15 && timeLeft.TotalMinutes < 30)
            {
                discordTimer.NextNotificationTime = discordTimer.NextNotificationTime.AddMinutes(15);
            }

            // More than 5 minutes until the timer expires.
            if (timeLeft.TotalMinutes >= 5 && timeLeft.TotalMinutes < 15)
            {
                discordTimer.NextNotificationTime = discordTimer.NextNotificationTime.AddMinutes(5);
            }
            else // Less than 5 minutes until the timer expires.
            {
                // Check if there's still time before the timer expires.
                if (discordTimer.EndTime > DateTime.Now)
                {
                    discordTimer.NextNotificationTime = discordTimer.EndTime;
                }
                else // timer has expired.
                {
                    try
                    {
                        // DM the user an update on their timer.
                        await discordTimer.User.SendMessageAsync("Your timer has expired!");
                    }
                    catch
                    {
                        // Something went wrong.  Send the message to the channel the timer request originated from.
                        await discordTimer.Channel.SendMessageAsync(discordTimer.User.Username + "'s timer expired, but something is preventing us from sending them a DM to let them know.");
                    }

                    // clean up this timer.
                    discordTimer.Stop();
                    discordTimer.Timer.Dispose();
                    _timers.Remove(discordTimer);
                    //await _db.DeleteShield(shieldTimer.DbId);
                    discordTimer.Dispose();

                    return;
                }
            }

            // set the timer interval.
            timeUntilNotification = discordTimer.NextNotificationTime - DateTime.Now;
            discordTimer.Timer.Interval = timeUntilNotification.TotalMilliseconds;
            // start the timer.
            discordTimer.Start();

            //await _db.UpdateShield(shieldTimer.DbId, shieldTimer.NextNotificationTime);

            try
            {
                // DM the user an update on their timer.
                await discordTimer.User.SendMessageAsync("Your timer expires in " + timeLeft.Minutes + " minutes.");
            }
            catch
            {
                // Something went wrong.  Send the message to the channel the timer request originated from.
                await discordTimer.Channel.SendMessageAsync(discordTimer.User.Username + "'s timer expires in " + timeLeft.Minutes + " minutes, but something is preventing us from sending them a DM to let them know.");
            }
        }
    }
}
