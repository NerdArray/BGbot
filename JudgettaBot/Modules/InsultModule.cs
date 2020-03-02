using Discord.Commands;
using JudgettaBot.Resources;
using JudgettaBot.Services;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgettaBot.Modules
{
    [Group("insult")]
    public class InsultModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IStringLocalizer<Insults> _localizer;

        public InsultModule(IStringLocalizer<Insults> localizer)
        {
            _localizer = localizer;
        }

        [Command]
        public async Task InsultAsync(string target = null)
        {
            Random random = new Random(DateTime.Now.Millisecond);
            var randomNum = random.Next(1, 31);
            var insultNum = "Insult" + randomNum;

            if (target == null) // no target for insult.
            {
                var insult = _localizer[insultNum].Value.Replace("{0}", Context.User.Mention);
                await Context.Channel.SendMessageAsync(insult);
            }
            else
            {
                var mentionedUsers = Context.Message.MentionedUsers;
                if (mentionedUsers.Count > 0)
                {
                    var insult = _localizer[insultNum].Value.Replace("{0}", target);
                    await Context.Channel.SendMessageAsync(insult);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("🔪");
                }
            }
        }
    }
}
