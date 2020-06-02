using Discord;
using Discord.WebSocket;
using JudgettaBot.Models;
using JudgettaBot.Models.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JudgettaBot.Services
{
    public class TimerService
    {
        private readonly ILogger _logger;
        private readonly DiscordShardedClient _client;
        private readonly IHostEnvironment _host;
        private readonly IServiceProvider _services;
        private TimerOptions _timerDb;

        private List<DiscordTimer> _timers;
        
        public TimerService(ILogger<TimerService> logger, IServiceProvider services, IHostEnvironment host, TimerOptions timerOptions)
        {
            _logger = logger;
            _client = services.GetRequiredService<DiscordShardedClient>();
            _services = services;
            _host = host;
            _timerDb = timerOptions;

            _timers = new List<DiscordTimer>();
        }

        public async Task<int> CreateTimer(string name, SocketUser user, ISocketMessageChannel channel, TimeSpan span)
        {
            await RefreshData();

            // check if a timer exists without a name already
            if (name == null)
            {
                try
                {
                    var t = _timers.Where(x => x.User.Id == user.Id && x.Name == "Default").FirstOrDefault();
                    if (t != null)
                    {
                        // Notify the user to use a name.
                        await channel.SendMessageAsync("You already have a timer running that is named Default.  Try naming your timer like, @bot timer 00:30:00 shield");
                        return -1;
                    }
                    else
                    {
                        name = "Default";
                    }
                }
                catch (ArgumentNullException)
                {
                    name = "Default";
                }
            }

            // check if a timer exists with the same name already.
            try
            {
                var exists = _timers.Where(x => x.User.Id == user.Id && x.Name == name).FirstOrDefault();
                if (exists != null)
                {
                    // Notify the user to use a name.
                    await channel.SendMessageAsync("You already have a timer running with that name.  Try a different name.");
                    return -1;
                }
            }
            catch (ArgumentNullException)
            {
                //Do nothing.  This is what we want.
            }

            var timer = new DiscordTimer(name, user, channel, span);
            timer.TimerTicked += Timer_TimerTicked;
            timer.Start();
            _timers.Add(timer);
            await SaveTimer(timer);
            return 1;
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
                discordTimer.Stop();
                _timers.Remove(discordTimer);
                var success = await DeleteTimer(discordTimer);

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
                    _timers.Remove(discordTimer);
                    var success = await DeleteTimer(discordTimer);

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

        private async Task RefreshData()
        {
            List<TimerDetail> timerDb = new List<TimerDetail>();

            await Task.Run(() =>
            {
                using (var scope = _services.CreateScope())
                {
                    try
                    {
                        _timerDb = scope.ServiceProvider.GetRequiredService<TimerOptions>();
                    }
                    catch (InvalidOperationException)
                    {
                        // Do nothing because it's probably just a null value.
                    }
                }
            });
        }

        private async Task SaveTimer(DiscordTimer timer)
        {
            TimerDetail detail = new TimerDetail()
            {
                Name = timer.Name,
                UserId = timer.User.Id,
                ChannelId = timer.Channel.Id,
                NextNotificationTime = timer.NextNotificationTime,
                EndTime = timer.EndTime
            };

            List<TimerDetail> results = new List<TimerDetail>();
            try
            {
                results = _timerDb.Timers.Where(x => x.UserId == timer.User.Id).ToList();
            }
            catch (ArgumentNullException)
            {
                // Do nothing.
            }

            if (results.Count > 0) //timer exists
            {
                if (timer.Name != null)
                {
                    var result = results.FirstOrDefault(x => x.Name == timer.Name);
                    if (result != null)
                    {
                        _timerDb.Timers.Where(x => x.UserId == timer.User.Id && x.Name == timer.Name).Select(t => t = detail);
                    }
                    else
                    {
                        if (_timerDb.Timers == null)
                        {
                            _timerDb.Timers = new TimerDetail[] { detail };
                        }
                        else
                        {
                            TimerDetail[] t = new TimerDetail[_timerDb.Timers.Length + 1];
                            for (int i = 0; i < _timerDb.Timers.Length; i++)
                            {
                                t[i] = _timerDb.Timers[i];
                            }
                            t[t.Length - 1] = detail;

                            _timerDb.Timers = t;
                        }
                    }
                }
                else
                {
                    _timerDb.Timers.Where(x => x.UserId == timer.User.Id).Select(t => t = detail);
                }
            }
            else  // timer not found.
            {
                if (_timerDb.Timers == null)
                {
                    _timerDb.Timers = new TimerDetail[] { detail };
                }
                else
                {
                    TimerDetail[] t = new TimerDetail[_timerDb.Timers.Length + 1];
                    for (int i = 0; i < _timerDb.Timers.Length; i++)
                    {
                        t[i] = _timerDb.Timers[i];
                    }
                    t[t.Length - 1] = detail;

                    _timerDb.Timers = t;
                }
            }

            await WriteToDisk();
        }

        private async Task<int> DeleteTimer(DiscordTimer timer)
        {
            TimerDetail detail = new TimerDetail()
            {
                Name = timer.Name,
                UserId = timer.User.Id,
                ChannelId = timer.Channel.Id,
                NextNotificationTime = timer.NextNotificationTime,
                EndTime = timer.EndTime
            };

            List<TimerDetail> results = new List<TimerDetail>();
            try
            {
                results = _timerDb.Timers.Where(x => x.UserId == timer.User.Id && x.Name == timer.Name).ToList();
            }
            catch (ArgumentNullException)
            {
                // Do nothing.
            }

            if (results.Count > 0) //timer exists
            {
                var timers = new List<TimerDetail>(_timerDb.Timers);
                foreach (var t in results)
                {
                    timers.Remove(t);
                }
                _timerDb.Timers = timers.ToArray();
                await WriteToDisk();
            }
            else // couldn't find timer.
            {
                return -1;
            }
            timer.Stop();
            timer.Dispose();

            return 1;
        }

        public async Task<int> DeleteTimerByName(ulong user, string name)
        {
            try
            {
                var results = _timers.Where(x => x.User.Id == user && x.Name == name).ToList();

                var timers = new List<TimerDetail>(_timerDb.Timers);
                foreach (var t in results)
                {
                    _timers.Remove(t);
                    t.Stop();
                    var detail = timers.Where(x => x.UserId == t.User.Id && x.Name == t.Name).FirstOrDefault();
                    timers.Remove(detail);
                    t.Dispose();
                }
                _timerDb.Timers = timers.ToArray();
                await WriteToDisk();
            }
            catch (ArgumentNullException)
            {
                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
            return 1;
        }

        private async Task WriteToDisk()
        {
            try
            {
                using (FileStream fs = File.Create(_host.ContentRootPath + "\\timestore.json"))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };

                    await JsonSerializer.SerializeAsync(fs, _timerDb, options).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var err = "Exception: " + ex.Message;
                if (ex.InnerException != null)
                {
                    err += "\r\nInner Exception: " + ex.InnerException.Message;
                }
                err += "\r\nStack Trace: " + ex.StackTrace;
                _logger.LogError(err); // log error.
                //TODO: Notify the user something went wrong.
            }
        }

        public async Task LoadSavedTimers()
        {
            var timers = new List<TimerDetail>(_timerDb.Timers);

            foreach (var timer in _timerDb.Timers)
            {
                var user = _client.GetUser(timer.UserId);
                var channel = _client.GetChannel(timer.ChannelId);
                var span = timer.EndTime - DateTime.Now;
                if (span.TotalSeconds <= 0)
                {
                    timers.Remove(timer);
                }
                else
                {
                    await CreateTimer(timer.Name, user, (ISocketMessageChannel)channel, span);
                }
            }
        }
    }
}
