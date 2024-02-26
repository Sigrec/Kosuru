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
        private const string KOSURU_BOT_JOIN_LINK = "https://discord.com/oauth2/authorize?client_id=1211708758141702224&permissions=377957173248&scope=bot+applications.commands";
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
            DiscordConfiguration kosuruCondiguration = new DiscordConfiguration()
            {
                Intents = DiscordIntents.MessageContents | DiscordIntents.DirectMessages | DiscordIntents.Guilds,
                Token = KosuruConfig?.DecodeToken(),
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            KosuruClient = new DiscordClient(kosuruCondiguration);
            KosuruClient.Ready += (s, a) => { return Task.CompletedTask; };
            Debug.WriteLine(KosuruConfig?.Prefix);

            CommandsNextConfiguration commandsConfiguration = new CommandsNextConfiguration()
            {
                StringPrefixes = [ KosuruConfig?.Prefix ],
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = false
            };
            KosuruCommands = KosuruClient.UseCommandsNext(commandsConfiguration);
            KosuruCommands.RegisterCommands<KosuruCommands>();
        }
    }
}
