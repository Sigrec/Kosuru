using System.Diagnostics;
using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Kosuru.Config;
using Src.Models;
using static MangaAndLightNovelWebScrape.Models.Constants;

namespace Kosuru
{
    internal class Program
    {
        public static DiscordClient KosuruClient { get; set; }
        private static CommandsNextExtension KosuruCommands {  get; set; }
        private const string CONFIG_PATH = @"Config\config.json";
        private const string KOSURU_BOT_JOIN_LINK = "https://discord.com/oauth2/authorize?client_id=1211708758141702224&permissions=274877967360&scope=bot";
        private static KosuruConfig KosuruConfig;
        public static HashSet<string> SelectedWebsites;
        public static StockStatus[] SelectedStockStatusFilter;

        static async Task Main()
        {
            KosuruConfig = await JsonSerializer.DeserializeAsync<KosuruConfig>(File.OpenRead(CONFIG_PATH));
            CreateClient();

            await KosuruClient.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void CreateClient()
        {
            // Setup Kosuru bot
            KosuruClient = new DiscordClient(new DiscordConfiguration()
            {
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.DirectMessages | DiscordIntents.MessageContents,
                Token = KosuruConfig?.DecodeToken(),
                TokenType = TokenType.Bot,
                AutoReconnect = true
            });

            // Set timeout for user input
            KosuruClient.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            // Setup commands
            KosuruCommands = KosuruClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = [ KosuruConfig?.Prefix ],
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = false
            });
            KosuruCommands.RegisterCommands<KosuruCommands>();

            KosuruClient.ComponentInteractionCreated += async (s, e) =>
            {
                switch (e.Interaction.Data.CustomId)
                {
                    case "bookTypeDropdown":
                        Console.WriteLine($"Selected Websites {e.Interaction.Data.Values[0]}");
                        break;
                    case "websiteDropdown":
                        Console.WriteLine($"Selected Websites [{string.Join(", ", e.Interaction.Data.Values)}]");
                        break;
                    case "stockStatusFilterDropdown":
                        Console.WriteLine($"Selected Stock Filter(s) [{string.Join(", ", e.Interaction.Data.Values)}]");
                        break;
                    case "membershipDropdown":
                        Console.WriteLine($"Memberships [{string.Join(", ", e.Interaction.Data.Values)}]");
                        break;
                };
                await e.Interaction.DeferAsync();
            };
        }
    }
}
