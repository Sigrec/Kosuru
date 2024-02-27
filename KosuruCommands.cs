using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using static MangaAndLightNovelWebScrape.Models.Constants;

namespace Kosuru
{
    public class KosuruCommands : BaseCommandModule
    {
        private static readonly List<DiscordSelectComponentOption> RegionDropdown = new List<DiscordSelectComponentOption>()
        {
            new DiscordSelectComponentOption(
                Region.America.ToString(),
                Region.America.ToString(),
                isDefault: true,
                emoji: new DiscordComponentEmoji("🇺🇸")),
            new DiscordSelectComponentOption(
                Region.Australia.ToString(),
                Region.Australia.ToString(),
                isDefault: false,
                emoji: new DiscordComponentEmoji("🇦🇺")),
            new DiscordSelectComponentOption(
                Region.Britain.ToString(),
                Region.Britain.ToString(),
                isDefault: false,
                emoji: new DiscordComponentEmoji("🇬🇧")),
            new DiscordSelectComponentOption(
                Region.Canada.ToString(),
                Region.Canada.ToString(),
                isDefault: false,
                emoji: new DiscordComponentEmoji("🇨🇦")),
            new DiscordSelectComponentOption(
                Region.Europe.ToString(),
                Region.Europe.ToString(),
                isDefault: false,
                emoji: new DiscordComponentEmoji("🇪🇺")),
            new DiscordSelectComponentOption(
                Region.Japan.ToString(),
                Region.Japan.ToString(),
                isDefault: false,
                emoji: new DiscordComponentEmoji("🇯🇵")) 
        };

        private static readonly DiscordSelectComponent RegionDropdownComponent = new DiscordSelectComponent("regionDropdown", null, RegionDropdown, false, 1, 1);

        [Command("kosuru")]
        public async Task ScrapeCommand(CommandContext ctx)
        {
            // await ctx.RespondAsync($"Hi **{ctx.User.GlobalName}**, Here are your Results!");
            // var myButton = new DiscordButtonComponent(ButtonStyle.Primary, "my_custom_id", "This is a button!");

            _ = await new DiscordMessageBuilder()
                    .WithContent($"**Select your Region**")
                    .AddComponents(RegionDropdownComponent)
                    .WithReply(ctx.Message.Id, true)
                    .SendAsync(ctx.Channel);
        }

        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            // await ctx.RespondAsync($"Hi **{ctx.User.GlobalName}**, Here are your Results!");
            // var myButton = new DiscordButtonComponent(ButtonStyle.Primary, "my_custom_id", "This is a button!");

            _ = await new DiscordMessageBuilder()
                    .WithContent($"Hi **{ctx.User.GlobalName}**\n **Select your Region**")
                    .AddComponents(RegionDropdownComponent)
                    .WithReply(ctx.Message.Id, true)
                    .SendAsync(ctx.Channel);
        }
    }
}