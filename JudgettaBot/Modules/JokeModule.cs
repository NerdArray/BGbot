using Discord.Commands;
using JudgettaBot.Models;
using JudgettaBot.Resources;
using JudgettaBot.Services;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using System.Threading.Tasks;

namespace JudgettaBot.Modules
{
    public class JokeModule : ModuleBase<ShardedCommandContext>
    {
        private readonly JokeService _jokeService;
        private readonly IStringLocalizer<General> _localizer;

        public JokeModule(JokeService jokeService, IStringLocalizer<General> localizer)
        {
            _jokeService = jokeService;
            _localizer = localizer;
        }

        [Command("joke")]
        public async Task JokeAsync()
        {
            var response = await _jokeService.GetJokeAsync();
            var joke = JsonSerializer.Deserialize(response, typeof(DadJoke)) as DadJoke;
            await Context.Channel.SendMessageAsync(_localizer["JokeMessage"]);
            await Context.Channel.SendMessageAsync(joke.joke);
        }
    }
}
