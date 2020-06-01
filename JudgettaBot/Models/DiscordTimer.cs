using Discord.WebSocket;
using System;
using System.Timers;

namespace JudgettaBot.Models
{
    public class DiscordTimer : IDisposable
    {
        public delegate void TimerTickedEventHandler(DiscordTimer discordTimer);
        public event TimerTickedEventHandler TimerTicked;

        /// <summary>
        /// Creates a new timer
        /// </summary>
        /// <param name="user">The user who initiated the timer.</param>
        /// <param name="channel">The channel that the request originated from.</param>
        /// <param name="hours">The number of hours until the timer ends.</param>
        /// <param name="minutes">The number of minutes until the timer ends.</param>
        /// <param name="seconds">The number of seconds until the timer ends.</param>
        public DiscordTimer(SocketUser user, ISocketMessageChannel channel, TimeSpan time)
        {
            User = user;
            Channel = channel;

            EndTime = DateTime.Now.AddDays(time.Days).AddHours(time.Hours).AddMinutes(time.Minutes).AddSeconds(time.Seconds);

            SetNextNotificationTime(time);

            Timer = new Timer((NextNotificationTime - DateTime.Now).TotalMilliseconds);
            Timer.Elapsed += Timer_Elapsed;
            Timer.AutoReset = false;
        }

        /// <summary>
        /// Creates a new timer and matches it to an existing timer in the database.
        /// </summary>
        /// <param name="user">The user who initiated the timer.</param>
        /// <param name="channel">The channel that the request originated from.</param>
        /// <param name="endTime">The predefined end time of the timer</param>
        /// <param name="nextNotificationTime">The time that the next timer notification is due.</param>
        /// <param name="dbId">The ID of the timer in the database.</param>
        public DiscordTimer(SocketUser user, ISocketMessageChannel channel, DateTime endTime, DateTime nextNotificationTime, int dbId)
        {
            User = user;
            Channel = channel;
            EndTime = endTime;
            NextNotificationTime = nextNotificationTime;
            DbId = dbId;

            Timer = new Timer((NextNotificationTime - DateTime.Now).TotalMilliseconds);
            Timer.Elapsed += Timer_Elapsed;
            Timer.AutoReset = true;
        }

        private void SetNextNotificationTime(TimeSpan time)
        {
            if (time.TotalHours > 3)
            {
                // the first notification is 3 hours before the timer expires.
                NextNotificationTime = EndTime.AddHours(-3);
            }
            else
            {
                if (time.TotalMinutes > 60)
                {
                    NextNotificationTime = EndTime.AddHours(-1);
                }
                else
                {
                    if (time.TotalMinutes > 30)
                    {
                        NextNotificationTime = EndTime.AddMinutes(-30);
                    }
                    else
                    {
                        if (time.TotalMinutes > 15)
                        {
                            NextNotificationTime = EndTime.AddMinutes(-15);
                        }
                        else
                        {
                            if (time.TotalMinutes > 5)
                            {
                                NextNotificationTime = EndTime.AddMinutes(-5);
                            }
                            else
                            {
                                if (time.TotalMinutes > 1)
                                {
                                    NextNotificationTime = EndTime.AddMinutes(-1);
                                }
                                else
                                {
                                    NextNotificationTime = EndTime;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Start()
        {
            Timer.Start();
        }

        public void Stop()
        {
            Timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerTicked?.Invoke(this);
        }

        // Track the ID of this record in the database.
        public int DbId { get; set; }

        public SocketUser User { get; private set; }

        public ISocketMessageChannel Channel { get; private set; }

        public DateTime EndTime { get; private set; }

        public DateTime NextNotificationTime { get; set; }

        public Timer Timer { get; set; }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}
