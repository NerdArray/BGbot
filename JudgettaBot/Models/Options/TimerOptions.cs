using System;
using System.Collections.Generic;

namespace JudgettaBot.Models.Options
{
    public class TimerOptions
    {
        public TimerDetail[]? Timers { get; set; }
    }

    public class TimerDetail
    {
        public string Name { get; set; }

        public ulong UserId { get; set; }

        public ulong ChannelId { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime NextNotificationTime { get; set; }
    }
}

