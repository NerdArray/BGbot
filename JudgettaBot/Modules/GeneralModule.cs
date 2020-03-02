using Discord.Commands;
using JudgettaBot.Resources;
using Microsoft.Extensions.Localization;
using System.Threading.Tasks;

namespace JudgettaBot.Modules
{
    public class GeneralModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IStringLocalizer<General> _localizer;

        public GeneralModule(IStringLocalizer<General> localizer)
        {
            _localizer = localizer;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            var message = _localizer["HelpMessage"].Value.Replace("{0}", Context.User.Mention);
            await Context.Channel.SendMessageAsync(message);
        }
    }
}
