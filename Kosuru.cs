using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Kosuru.Config;
using MangaAndLightNovelWebScrape;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kosuru
{
    internal class Kosuru
    {
        public static DiscordShardedClient Client { get; set; }
        private const string CONFIG_PATH = @"Config\config.json";
        private const string KOSURU_BOT_JOIN_LINK = "https://discord.com/oauth2/authorize?client_id=1211708758141702224&permissions=277025507328&scope=bot+applications.commands";
        private static KosuruConfig KosuruConfig;
        public const ushort INTERACTION_TIMEOUT = 30;
        public static readonly DiscordColor COLOR = new DiscordColor("#49576F");

        static async Task Main()
        {
            KosuruConfig = await JsonSerializer.DeserializeAsync<KosuruConfig>(File.OpenRead(CONFIG_PATH));
            await CreateClient();

            await Client.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task CreateClient()
        {
            // Setup Kosuru bot
            Client = new DiscordShardedClient(new DiscordConfiguration()
            {
                Intents = DiscordIntents.DirectMessages | DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds,
                Token = KosuruConfig?.DecodeToken(),
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                AutoReconnect = true,
                ShardCount = 1
            });
            await Client.UseSlashCommandsAsync();

            // Set timeout for user input
            await Client.UseInteractivityAsync(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(INTERACTION_TIMEOUT)
            });

            // Setup commands
            IReadOnlyDictionary<int, SlashCommandsExtension> Commands = await Client.UseSlashCommandsAsync();
            Commands.RegisterCommands<KosuruCommands>();

            Client.ComponentInteractionCreated += async (s, e) =>
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                //switch (e.Interaction.Data.CustomId)
                //{
                //    case "stockStatusFilterDropdown":
                //    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                //    break;
                //case "websiteDropdown":
                //    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
                //    break;
                //case "membershipDropdown":
                //    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                //    break;
                //}
            };
        }
    }
}
