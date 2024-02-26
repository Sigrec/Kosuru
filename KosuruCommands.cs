using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Kosuru
{
    public class KosuruCommands : BaseCommandModule
    {

        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"HELLO {ctx.User}");
        }

        [Command("kosuru")]
        public async Task ScrapeCommand(CommandContext ctx)
        {
            _ = await new DiscordMessageBuilder()
                    .WithContent($"{ctx.User.Mention} Here are Your Kosuru Results!")
                    .WithAllowedMentions([ new UserMention(ctx.User) ])
                    .SendAsync(ctx.Channel);
        }
    }
}