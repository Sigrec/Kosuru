using System.Diagnostics;
using System.Text.Json;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Kosuru.Config;

namespace Kosuru
{
    internal class Program
    {
        private static DiscordClient KosuruClient { get; set; }
        private static CommandsNextExtension KosuruCommands {  get; set; }
        private const string CONFIG_PATH = @"Config\config.json";
        private const string KOSURU_BOT_JOIN_LINK = "https://discord.com/oauth2/authorize?client_id=1211708758141702224&permissions=274877967360&scope=bot";
        private static KosuruConfig KosuruConfig;

        static async Task Main()
        {
            KosuruConfig = await JsonSerializer.DeserializeAsync<KosuruConfig>(File.OpenRead(CONFIG_PATH));
            CreateClient();

            await KosuruClient.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void CreateClient()
        {
            KosuruClient = new DiscordClient(new DiscordConfiguration()
            {
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.DirectMessages | DiscordIntents.MessageContents,
                Token = KosuruConfig?.DecodeToken(),
                TokenType = TokenType.Bot,
                AutoReconnect = true
            });
            // KosuruClient.Ready += (s, a) => { return Task.CompletedTask; };
            KosuruCommands = KosuruClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = [ KosuruConfig?.Prefix ],
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = false
            });
            KosuruCommands.RegisterCommands<KosuruCommands>();
        }
    }
}
