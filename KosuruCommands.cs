using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Websites;
using Src.Models;
using System.Diagnostics;
using System.Text;
using static MangaAndLightNovelWebScrape.Models.Constants;

namespace Kosuru
{
    public class KosuruCommands : BaseCommandModule
    {
        /*private static readonly DiscordSelectComponent RegionDropdownComponent = new DiscordSelectComponent(
            "regionDropdown", 
            "Select Region",
            [
                new DiscordSelectComponentOption(
                    Region.America.ToString(),
                    Region.America.ToString(),
                    isDefault: false,
                    emoji: new DiscordComponentEmoji("🇺🇸")),
                new DiscordSelectComponentOption(
                    Region.Australia.ToString(),
                    Region.Australia.ToString(),
                    emoji: new DiscordComponentEmoji("🇦🇺")),
                new DiscordSelectComponentOption(
                    Region.Britain.ToString(),
                    Region.Britain.ToString(),
                    emoji: new DiscordComponentEmoji("🇬🇧")),
                new DiscordSelectComponentOption(
                    Region.Canada.ToString(),
                    Region.Canada.ToString(),
                    emoji: new DiscordComponentEmoji("🇨🇦")),
                new DiscordSelectComponentOption(
                    Region.Europe.ToString(),
                    Region.Europe.ToString(),
                    emoji: new DiscordComponentEmoji("🇪🇺")),
                new DiscordSelectComponentOption(
                    Region.Japan.ToString(),
                    Region.Japan.ToString(),
                    isDefault: false,
                    emoji: new DiscordComponentEmoji("🇯🇵"))
            ],
            false, 1, 1);*/

        private static readonly DiscordSelectComponent StockStatusFilterDropdownComponent = new DiscordSelectComponent("stockStatusFilterDropdown", "Select Stock Status Filter(s)", 
            [
                new DiscordSelectComponentOption(
                    "None",
                    "None"),
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
            false, 0, 3);

        private static readonly DiscordSelectComponent BookTypeDropdownComponent = new DiscordSelectComponent("bookTypeDropdown", "Select Book Type",
            [
                new DiscordSelectComponentOption(
                    "Manga",
                    "MANGA"),
                new DiscordSelectComponentOption(
                    "Light Novel",
                    "NOVEL")
            ],
            false, 1, 1);

        private static readonly DiscordSelectComponent MembershipDropdownComponent = new DiscordSelectComponent("membershipDropdown", "Select Membership(s)",
            [
                new DiscordSelectComponentOption(
                    "None",
                    "None"),
                new DiscordSelectComponentOption(
                    BarnesAndNoble.WEBSITE_TITLE,
                    BarnesAndNoble.WEBSITE_TITLE),
                new DiscordSelectComponentOption(
                    BooksAMillion.WEBSITE_TITLE,
                    BooksAMillion.WEBSITE_TITLE),
                new DiscordSelectComponentOption(
                    KinokuniyaUSA.WEBSITE_TITLE,
                    KinokuniyaUSA.WEBSITE_TITLE),
                new DiscordSelectComponentOption(
                    Indigo.WEBSITE_TITLE,
                    Indigo.WEBSITE_TITLE)
            ],
            false, 0, 4);

        private static MasterScrape scrape;

        [Command("kosuru")]
        public async Task ScrapeCommand(CommandContext ctx, string region)
        {
            // Get input for the scrape from user
            try
            {
                Region curRegion = Helpers.GetRegionFromString(region);
                var selectScrapeOptionsMessage = await new DiscordMessageBuilder()
                    .WithContent("### Select Params")
                    .AddComponents(new List<DiscordActionRowComponent>
                    {
                    new([BookTypeDropdownComponent]),
                    new([GenerateWebsiteDropdownComponent(curRegion)]),
                    new([StockStatusFilterDropdownComponent]),
                    new([MembershipDropdownComponent])
                    })
                    .WithReply(ctx.Message.Id, true)
                    .SendAsync(ctx.Channel);

                var interactivityTimeout = Program.KosuruClient.GetInteractivity();
                var selectionArray = Task.WhenAll(interactivityTimeout.WaitForSelectAsync(selectScrapeOptionsMessage, "websiteDropdown"), interactivityTimeout.WaitForSelectAsync(selectScrapeOptionsMessage, "bookTypeDropdown"), interactivityTimeout.WaitForSelectAsync(selectScrapeOptionsMessage, "stockStatusFilterDropdown"), interactivityTimeout.WaitForSelectAsync(selectScrapeOptionsMessage, "membershipDropdown"));

                string[] websiteSelection = selectionArray.Result[0].Result.Values;
                BookType bookTypeSelection = selectionArray.Result[1].Result.Values[0].Equals("MANGA") ? BookType.Manga : BookType.LightNovel;
                string[] stockStatusFilterSelection = selectionArray.Result[2].Result.Values;
                string[] membershipSelection = selectionArray.Result[3].Result.Values;


                if (websiteSelection.Length != 0)
                {
                    // Start scrape
                    scrape = new MasterScrape(GetStockStatus(stockStatusFilterSelection), curRegion);
                    await scrape.InitializeScrapeAsync(
                        "jujutsu kaisen",
                        bookTypeSelection,
                        scrape.GenerateWebsiteList(websiteSelection),
                        IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection),
                        IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection),
                        IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection),
                        IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection)
                    );

                    // Delete initial msg and print results
                    // Format the result output
                    int longestTitle = "Title".Length;
                    int longestPrice = "Price".Length;
                    int longestStockStatus = "Status".Length;
                    int longestWebsite = "Website".Length;
                    foreach (EntryModel entry in scrape.GetResults())
                    {
                        longestTitle = Math.Max(longestTitle, entry.Entry.Length);
                        longestPrice = Math.Max(longestPrice, entry.Price.Length);
                        longestWebsite = Math.Max(longestWebsite, entry.Website.Length);
                    }

                    StringBuilder resultField = new StringBuilder();
                    resultField.AppendLine($"┏{"━".PadRight(longestTitle + 2, '━')}┳{"━".PadRight(longestPrice + 2, '━')}┳{"━".PadRight(longestStockStatus + 2, '━')}┳{"━".PadRight(longestWebsite + 2, '━')}┓");
                    resultField.AppendLine($"┃ {"Title".PadRight(longestTitle)} ┃ {"Price".PadRight(longestPrice)} ┃ {"Status".PadRight(longestStockStatus)} ┃ {"Website".PadRight(longestWebsite)} ┃");
                    resultField.AppendLine($"┣{"━".PadRight(longestTitle + 2, '━')}╋{"━".PadRight(longestPrice + 2, '━')}╋{"━".PadRight(longestStockStatus + 2, '━')}╋{"━".PadRight(longestWebsite + 2, '━')}┫");
                    foreach (EntryModel entry in scrape.GetResults()) { resultField.AppendLine($"┃ {entry.Entry.PadRight(longestTitle)} ┃ {entry.Price.PadRight(longestPrice)} ┃ {entry.StockStatus.ToString().PadRight(longestStockStatus)} ┃ {entry.Website.PadRight(longestWebsite)} ┃"); }
                    resultField.AppendLine($"┗{"━".PadRight(longestTitle + 2, '━')}┻{"━".PadRight(longestPrice + 2, '━')}┻{"━".PadRight(longestStockStatus + 2, '━')}┻{"━".PadRight(longestWebsite + 2, '━')}┛").Append("");

                    await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + @"\KosuruOutput.txt", resultField.ToString());
                    // Console.WriteLine(Directory.GetCurrentDirectory() + "\\KosuruOutput.txt");

                    DiscordEmbedBuilder results = new DiscordEmbedBuilder
                    {
                        Color = ctx.User.BannerColor,
                        Timestamp = DateTime.Now,
                    };
                    results.AddField("Title", "Jujutsu Kaisen", true);
                    results.AddField("Region", curRegion.ToString(), true);
                    string websites = string.Empty;
                    foreach (var resultUrl in scrape.GetResultUrls())
                    {
                        websites += $"[{resultUrl.Key}]({resultUrl.Value})\n";
                    }
                    results.AddField("Book Type", bookTypeSelection.ToString(), true);
                    results.AddField("Memberships", "Barnes & Noble", true);
                    results.AddField("Websites", websites, true);
                    results.WithAuthor("Kosuru", @"https://github.com/Sigrec/Kosuru", ctx.Member.AvatarUrl);
                    results.WithFooter("Kosuru", ctx.Member.AvatarUrl);
                
                    await new DiscordMessageBuilder()
                            .WithContent($"{ctx.User.Mention} Here are your Results!")
                            .AddEmbed(results)
                            .AddFile("KosuruOutput.txt", new FileStream("KosuruOutput.txt", FileMode.Open, FileAccess.Read))
                            .WithAllowedMentions([new UserMention(ctx.User)])
                            .SendAsync(ctx.Channel);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            // Get input for the scrape from user
            try
            {
                scrape = new MasterScrape(StockStatusFilter.EXCLUDE_NONE_FILTER, Region.America);
                await scrape.InitializeScrapeAsync(
                    "world trigger",
                    BookType.Manga,
                    new HashSet<Website>([ Website.Crunchyroll, Website.RobertsAnimeCornerStore, Website.InStockTrades ])
                );

                // Delete initial msg and print results
                Console.WriteLine(ctx.User.BannerColor);

                // Format the result output
                int longestTitle = "Title".Length;
                int longestPrice = "Price".Length;
                int longestStockStatus = "Status".Length;
                int longestWebsite = "Website".Length;
                foreach (EntryModel entry in scrape.GetResults())
                {
                    longestTitle = Math.Max(longestTitle, entry.Entry.Length);
                    longestPrice = Math.Max(longestPrice, entry.Price.Length);
                    longestWebsite = Math.Max(longestWebsite, entry.Website.Length);
                }

                StringBuilder resultField = new StringBuilder();
                resultField.AppendLine($"┏{"━".PadRight(longestTitle + 2, '━')}┳{"━".PadRight(longestPrice + 2, '━')}┳{"━".PadRight(longestStockStatus + 2, '━')}┳{"━".PadRight(longestWebsite + 2, '━')}┓");
                resultField.AppendLine($"┃ {"Title".PadRight(longestTitle)} ┃ {"Price".PadRight(longestPrice)} ┃ {"Status".PadRight(longestStockStatus)} ┃ {"Website".PadRight(longestWebsite)} ┃");
                resultField.AppendLine($"┣{"━".PadRight(longestTitle + 2, '━')}╋{"━".PadRight(longestPrice + 2, '━')}╋{"━".PadRight(longestStockStatus + 2, '━')}╋{"━".PadRight(longestWebsite + 2, '━')}┫");
                foreach (EntryModel entry in scrape.GetResults()) { resultField.AppendLine($"┃ {entry.Entry.PadRight(longestTitle)} ┃ {entry.Price.PadRight(longestPrice)} ┃ {entry.StockStatus.ToString().PadRight(longestStockStatus)} ┃ {entry.Website.PadRight(longestWebsite)} ┃"); }
                resultField.AppendLine($"┗{"━".PadRight(longestTitle + 2, '━')}┻{"━".PadRight(longestPrice + 2, '━')}┻{"━".PadRight(longestStockStatus + 2, '━')}┻{"━".PadRight(longestWebsite + 2, '━')}┛").Append("");

                await File.WriteAllTextAsync(Directory.GetCurrentDirectory() + @"\KosuruOutput.txt", resultField.ToString());
                // Console.WriteLine(Directory.GetCurrentDirectory() + "\\KosuruOutput.txt");

                DiscordEmbedBuilder results = new DiscordEmbedBuilder
                {
                    Color = ctx.User.BannerColor,
                    Timestamp = DateTime.Now,
                };
                results.AddField("Title", "World Trigger", true);
                results.AddField("Region", Region.America.ToString(), true);
                string websites = string.Empty;
                foreach (var resultUrl in scrape.GetResultUrls())
                {
                    websites += $"[{resultUrl.Key}]({resultUrl.Value})\n";
                }
                results.AddField("Websites", websites, true);
                results.WithAuthor("Kosuru", @"https://github.com/Sigrec/Kosuru", ctx.Member.AvatarUrl);
                results.WithFooter("Kosuru", ctx.Member.AvatarUrl);

                await new DiscordMessageBuilder()
                    .WithContent($"{ctx.User.Mention} Here are your Results!")
                    .AddEmbed(results)
                    .AddFile("KosuruOutput.txt", new FileStream("KosuruOutput.txt", FileMode.Open, FileAccess.Read))
                    .WithAllowedMentions([new UserMention(ctx.User)])
                    .SendAsync(ctx.Channel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private static StockStatus[] GetStockStatus(string[] selectedFilters)
        {
            if (!selectedFilters.Contains("NONE"))
            {
                switch (selectedFilters.Length)
                {
                    case 1:
                        return Helpers.GetStockStatusFilterFromString(selectedFilters[0]);
                    case 2:
                        if (selectedFilters.Contains("OOS") && selectedFilters.Contains("BO")) { return StockStatusFilter.EXCLUDE_OOS_AND_BO_FILTER; }
                        else if (selectedFilters.Contains("OOS") && selectedFilters.Contains("PO")) { return StockStatusFilter.EXCLUDE_OOS_AND_PO_FILTER; }
                        else { return StockStatusFilter.EXCLUDE_PO_AND_BO_FILTER; }
                    case 3:
                        return StockStatusFilter.EXCLUDE_ALL_FILTER;
                    case 0:
                    default:
                        return StockStatusFilter.EXCLUDE_NONE_FILTER;
                }
            }
            return StockStatusFilter.EXCLUDE_NONE_FILTER;
        }

        private static bool IsMember(string website, string[] memberships)
        {
            return !memberships.Contains("NONE") && memberships.Contains(website);
        }

        private static DiscordSelectComponent GenerateWebsiteDropdownComponent(Region region)
        {
            string[] input = Helpers.GetRegionWebsiteListAsString(region);
            List<DiscordSelectComponentOption> optionsList = new List<DiscordSelectComponentOption>();
            foreach (string website in input)
            {
                optionsList.Add(new DiscordSelectComponentOption(website, website));
            }
            return new DiscordSelectComponent("websiteDropdown", "Select Website(s)", optionsList, false, 1, input.Length);
        }
    }
}