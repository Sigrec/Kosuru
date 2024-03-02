﻿using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Kosuru.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kosuru
{
    internal class Kosuru
    {
        internal static readonly DiscordSelectComponent StockStatusFilterDropdownComponent = new DiscordSelectComponent("stockStatusFilterDropdown", "Select Stock Status Filter(s)",
        [
            new DiscordSelectComponentOption(
                "None",
                "NONE"),
            new DiscordSelectComponentOption(
                "Out of Stock",
                "OOS"),
            new DiscordSelectComponentOption(
                "Pre-Order",
                "PO"),
            new DiscordSelectComponentOption(
                "Backorder",
                "BO")
        ],
        false, 1, 3);

        // Static embed messages
        internal static DiscordEmbedBuilder CrashEmbed;
        internal static DiscordEmbedBuilder CooldownEmbed;
        internal static DiscordEmbedBuilder HelpEmbed;
        internal static DiscordEmbedBuilder NoResponseEmbed;

        internal const string NAME = "Kosuru";
        internal const string KOSURU_BOT_JOIN_LINK = "https://discord.com/oauth2/authorize?client_id=1211708758141702224&permissions=277025449984&scope=bot+applications.commands";
        internal const ushort INTERACTION_TIMEOUT = 45;
        internal static readonly DiscordColor COLOR = new DiscordColor("#49576F");

        public static DiscordShardedClient Client { get; set; }
        private static KosuruConfig KosuruConfig;

        static async Task Main()
        {
            // Configure and start the bot
            KosuruConfig = await JsonSerializer.DeserializeAsync<KosuruConfig>(File.OpenRead(@"Config\config.json"));
            await CreateClient();
            await Client.StartAsync();
            CreateEmbeds();
            await Task.Delay(-1);
        }

        private static async Task CreateClient()
        {
            // Setup Kosuru bot
            Client = new DiscordShardedClient(new DiscordConfiguration()
            {
                Intents = DiscordIntents.DirectMessages | DiscordIntents.MessageContents | DiscordIntents.Guilds,
                Token = KosuruConfig?.DecodeToken(),
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                AutoReconnect = true,
                ShardCount = 1
            });
            await Client.UseSlashCommandsAsync();

            Client.ComponentInteractionCreated += async (s, e) =>
            {
                switch (e.Interaction.Data.CustomId)
                {
                    case "websiteDropdown":
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        break;
                    case "membershipDropdown":
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        break;
                    case "stockStatusFilterDropdown":
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        break;
                }
            };

            // Set timeout for user input
            await Client.UseInteractivityAsync(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(INTERACTION_TIMEOUT)
            });

            // Setup commands
            var Commands = await Client.UseSlashCommandsAsync(new SlashCommandsConfiguration
            {
                Services = new ServiceCollection().AddSingleton<MangaAndLightNovelWebScrape.MasterScrape>().BuildServiceProvider()
            });
            Commands.RegisterCommands<KosuruCommands>();
        }

        private static void CreateEmbeds()
        {
            CrashEmbed = new DiscordEmbedBuilder
            {
                Description = $"### :bangbang: Kosuru Crashed! Try Again",
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow
            }.WithFooter(Kosuru.NAME, Kosuru.Client.CurrentUser.AvatarUrl);

            CooldownEmbed = new DiscordEmbedBuilder
            {
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow
            }.WithFooter(Kosuru.NAME, Kosuru.Client.CurrentUser.AvatarUrl);

            HelpEmbed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = Kosuru.NAME,
                    Url = @"https://github.com/Sigrec/Kosuru",
                    IconUrl = Kosuru.Client.CurrentUser.AvatarUrl
                },
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow,
                Description = "Scrapes websites from a given region for a manga or light novel series, and returns a list of compared prices for an entry in that series and outputs a list of the volumes with the lowest price. Users can additionally filter out entries based on stock status."
            }.WithFooter(Kosuru.NAME, Kosuru.Client.CurrentUser.AvatarUrl)
             .WithThumbnail(Kosuru.Client.CurrentUser.AvatarUrl)
             .AddField("Commands", "`/kosuru start [title] [region] [format] [dm]` - Starts Kosuru\n`/kosuru list [region]` - Lists the available websites for a region\n`/kosuru help` - Gives information about the Kosuru bot")
             .AddField("Stock Status Filters", "**OOS** (Out of Stock) | **PO** (Pre-Order) | **BO** (Backorder)")
             .AddField("Website & Region Requests", "If you want a new website or region to be added submit a [request](<https://github.com/Sigrec/MangaAndLightNovelWebScrape/issues/new/choose>), I will respond at a later time whether this request is doable and if so, it will be added to the queue");

            NoResponseEmbed = new DiscordEmbedBuilder
            {
                Description = "### :warning: Took to long to Respond! Try Again",
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow
            }.WithFooter(Kosuru.NAME, Kosuru.Client.CurrentUser.AvatarUrl);
        }
    }
}
