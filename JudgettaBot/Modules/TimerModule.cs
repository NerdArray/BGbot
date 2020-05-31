using Discord;
using Discord.Commands;
using JudgettaBot.Resources;
using JudgettaBot.Services;
using Microsoft.Extensions.Localization;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace JudgettaBot.Modules
{
    [Group("timer")]
    public class TimerModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IStringLocalizer<ShieldResources> _localizer;
        private readonly TimerService _timerService;

        public TimerModule(IStringLocalizer<ShieldResources> localizer, TimerService timerService)
        {
            _localizer = localizer;
            _timerService = timerService;
        }

        [Command]
        public async Task StartTimeAsync(string time = null, string name = null)
        {
            if (time == null) // no length of time supplied
            {
                await Context.Channel.SendMessageAsync(_localizer["NoInputMessage"].Value);
            }
            else
            {
                if (time.ToUpper() == "STOP" || time.ToUpper() == "CANCEL")
                {
                    //TODO: Cancel the timer.
                }
                else
                {
                    TimeSpan span;
                    var success = TimeSpan.TryParse(time, new CultureInfo("en-US"), out span);

                    if (success)
                    {
                        await _timerService.StartTimer(Context.User, Context.Channel, span);
                        // Let the user know.
                        await ReplyAsync("I've started a timer for you, " + Context.User.Username + ".  Make sure you have 'Allow direct messages from server members' enabled so that I can send you reminders before it expires.");
                        // DM them too.
                        await Context.User.SendMessageAsync("I've started your timer.  I'll DM you with increasing frequency beginning as it gets closer to expiring.");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(_localizer["BadInputMessage"].Value);
                    }
                }
            }
        }
    }
}
