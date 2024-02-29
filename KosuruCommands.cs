using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Extensions.Logging;
using Src.Models;
using System.Reflection;
using System.Text;
using static MangaAndLightNovelWebScrape.Models.Constants;
using static System.Net.Mime.MediaTypeNames;

namespace Kosuru
{
    public class KosuruCommands : ApplicationCommandModule
    {
        private static readonly DiscordSelectComponent StockStatusFilterDropdownComponent = new DiscordSelectComponent("stockStatusFilterDropdown", "Select Stock Status Filter(s)", 
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
                    "NONE"),
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

        private MasterScrape scrape;

        // TODO - Reply w/ Waiting Message After Selection
        // TODO - Issue when scrape takes longer than INTERACTION_TIMEOUT where it stops
        [SlashCommand("kosuru", "Kosuru")]
        // [SlashCooldown(1, Kosuru.INTERACTION_TIMEOUT, SlashCooldownBucketType.User)]
        public async Task KosuruCommand(InteractionContext ctx,
            [Option("Title", "Enter Title")] string title,

            [Choice("America", "America")]
            [Choice("Australia", "Australia")]
            [Choice("Britain", "Britain")]
            [Choice("Canada", "Canada")]
            [Choice("Europe", "Europe")]
            [Option("Region", "Select Region")]string region,

            [Choice("Manga", "MANGA")]
            [Choice("Light Novel", "NOVEL")]
            [Option("Format", "Manga or Light Novel")]string format)
        {
            // Get input for the scrape from user
            try
            {
                Region curRegion = Helpers.GetRegionFromString(region);
                BookType bookType = format == "MANGA" ? BookType.Manga : BookType.LightNovel;
                
                await ctx.DeferAsync(true);
                var selectScrapeOptionsResponse = await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordInteractionResponseBuilder()
                            .AddComponents(new List<DiscordActionRowComponent>
                            {
                                new([GenerateWebsiteDropdownComponent(curRegion)]),
                                new([StockStatusFilterDropdownComponent]),
                                new([MembershipDropdownComponent])
                            })
                            .AddMention(new UserMention(ctx.User))));

                var interactivity = Kosuru.Client.GetShard(ctx.Guild).GetInteractivity();
                var selectionArray = Task.WhenAll(interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "websiteDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "stockStatusFilterDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "membershipDropdown"));

                if (!selectionArray.Result.Any(component => component.Result == null))
                {
                    // Start scrape
                    string[] websiteSelection = selectionArray.Result[0].Result.Values;
                    string[] stockStatusFilterSelection = selectionArray.Result[1].Result.Values;
                    string[] membershipSelection = selectionArray.Result[2].Result.Values;
                    Kosuru.Client.Logger.LogDebug($"Getting Information For -> Title = \"{title}\", Region = {region}, Format = {bookType}, Websites = [{string.Join(" , ", websiteSelection)}], Stock Status = [{string.Join(" , ", stockStatusFilterSelection)}], Memberships = [{string.Join(" , ", membershipSelection)}]");

                    // Delete the Select Menus
                    scrape = new MasterScrape(GetStockStatus(stockStatusFilterSelection), curRegion);
                    await scrape.InitializeScrapeAsync(
                        title,
                        bookType,
                        scrape.GenerateWebsiteList(websiteSelection),
                        IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection),
                        IsMember(BooksAMillion.WEBSITE_TITLE, membershipSelection),
                        IsMember(KinokuniyaUSA.WEBSITE_TITLE, membershipSelection),
                        IsMember(Indigo.WEBSITE_TITLE, membershipSelection)
                    );

                    if (scrape.GetResults().Count > 0)
                    {
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

                        string titleLinePadding = "━".PadRight(longestTitle + 2, '━');
                        string priceLinePadding = "━".PadRight(longestPrice + 2, '━');
                        string stockStatusLinePadding = "━".PadRight(longestStockStatus + 2, '━');
                        string websiteLinePadding = "━".PadRight(longestWebsite + 2, '━');

                        StringBuilder resultField = new StringBuilder();
                        resultField.AppendFormat("Title: {0}", title).AppendLine();
                        resultField.AppendFormat("Book Type: {0}", bookType.ToString()).AppendLine();
                        resultField.AppendFormat("Region: {0}", curRegion.ToString()).AppendLine();
                        if (!membershipSelection.Contains("NONE")) { resultField.AppendFormat("Memberships: {0}", string.Join(" & ", membershipSelection)).AppendLine(); }
                        resultField.AppendFormat("┏{0}┳{1}┳{2}┳{3}┓", titleLinePadding, priceLinePadding, stockStatusLinePadding, websiteLinePadding).AppendLine();
                        resultField.AppendFormat("┃ {0} ┃ {1} ┃ {2} ┃ {3} ┃", "Title".PadRight(longestTitle), "Price".PadRight(longestPrice), "Status".PadRight(longestStockStatus), "Website".PadRight(longestWebsite)).AppendLine();
                        resultField.AppendFormat("┣{0}╋{1}╋{2}╋{3}┫", titleLinePadding, priceLinePadding, stockStatusLinePadding, websiteLinePadding).AppendLine();
                        foreach (EntryModel entry in scrape.GetResults()) { resultField.AppendFormat("┃ {0} ┃ {1} ┃ {2} ┃ {3} ┃", entry.Entry.PadRight(longestTitle), entry.Price.PadRight(longestPrice), entry.StockStatus.ToString().PadRight(longestStockStatus), entry.Website.PadRight(longestWebsite)).AppendLine(); }
                        resultField.AppendFormat("┗{0}┻{1}┻{2}┻{3}┛", titleLinePadding, priceLinePadding, stockStatusLinePadding, websiteLinePadding).AppendLine();

                        StringBuilder websites = new StringBuilder();
                        foreach (var resultUrl in scrape.GetResultUrls()) { websites.AppendFormat("[{0}](<{1}>)", resultUrl.Key, resultUrl.Value).AppendLine(); }

                        await ctx.DeleteResponseAsync();
                        await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent($">>> **{websites}**")
                                    .AddFile("KosuruResults.txt", new MemoryStream(Encoding.UTF8.GetBytes(resultField.ToString()))));
                    }
                    else // Kosuru found no data from user inputs
                    {
                        DiscordEmbedBuilder results = new DiscordEmbedBuilder
                        {
                            Title = ":frowning2: Kosuru Found No Data",
                            Color = ctx.User.BannerColor ?? new DiscordColor("#49576F"),
                            Timestamp = DateTimeOffset.UtcNow,
                            Description = "*Kosuru Found No Data for Below Inputs*"
                        };
                        results.WithThumbnail(Kosuru.Client.CurrentUser.AvatarUrl, 25, 25);
                        results.AddField("Title", title, true);
                        results.AddField("Region", curRegion.ToString(), true);
                        results.AddField("Book Type", bookType.ToString(), true);
                        if (!membershipSelection.Contains("NONE")) { results.AddField("Membership(s)", string.Join("\n", membershipSelection), membershipSelection.Length == 1); }
                        results.AddField("Website(s)", string.Join("\n", websiteSelection));
                        results.WithAuthor("Kosuru", @"https://github.com/Sigrec/Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);
                        results.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);

                        await ctx.DeleteResponseAsync();
                        await ctx.Channel.SendMessageAsync(
                            new DiscordMessageBuilder()
                                .WithContent(ctx.User.Mention)
                                .AddEmbed(results)
                                .AddMention(new UserMention(ctx.User)));
                    }
                }
                else // Delete input message if user doesn't respond in time and send a msg back
                {
                    Kosuru.Client.Logger.LogTrace($"User Not Responding in Time");
                    DiscordEmbedBuilder noResponseEmbed = new DiscordEmbedBuilder
                    {
                        Title = $":warning: {ctx.User.GlobalName} Took to long to Respond! Try Again",
                        Color = ctx.User.BannerColor ?? new DiscordColor("#49576F"),
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    noResponseEmbed.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);

                    await ctx.DeleteResponseAsync();
                    await ctx.Channel.SendMessageAsync(
                        new DiscordMessageBuilder()
                            .WithContent(string.Empty)
                            .AddEmbed(noResponseEmbed));
                }
            }
            catch (Exception ex)
            {

                Kosuru.Client.Logger.LogError(ex, "Kosuru Crashed w/ \"{}\"", ex.Message);
                DiscordEmbedBuilder errorEmbed = new DiscordEmbedBuilder
                {
                    Title = $":bangbang: Kosuru Crashed! Try Again",
                    Color = ctx.User.BannerColor ?? new DiscordColor("#49576F"),
                    Timestamp = DateTimeOffset.UtcNow
                };
                errorEmbed.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);

                await ctx.DeleteResponseAsync();
                await ctx.Channel.SendMessageAsync(
                    new DiscordMessageBuilder()
                        .WithContent(string.Empty)
                        .AddEmbed(errorEmbed));
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