using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Models;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Text;

namespace Kosuru
{
    [SlashCommandGroup("kosuru", "Kosuru Commands")]
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

        // TODO - Figure out how to reset Cooldown and handle cooldowns on user side
        // TODO ? - Figure out how to see how many guilds have access to my bot then stop and increase shard amount by 1 in Client Config & start again
        // TODO - Exception Handling
        // TODO - Create form in github fpr website requests
        // TODO = Finalize non Kosuru result message layouts
        [SlashCommand("start", "Start Kosuru")]
        // [SlashCooldown(1, Kosuru.INTERACTION_TIMEOUT, SlashCooldownBucketType.User)]
        public async Task KosuruCommand(InteractionContext ctx, [Option("Title", "Enter Title")] string title, [Choice("America", "America")][Choice("Australia", "Australia")][Choice("Britain", "Britain")][Choice("Canada", "Canada")][Choice("Europe", "Europe")][Option("Region", "Select Region")] string region, [Choice("Manga", "MANGA")][Choice("Light Novel", "NOVEL")][Option("Format", "Manga or Light Novel")] string format, [Option("DM", "Direct Message Results?")] bool dm)
        {
            // Get input for the scrape from user
            try
            {
                await ctx.DeferAsync(true);
                Region curRegion = Helpers.GetRegionFromString(region);
                BookType bookType = format == "MANGA" ? BookType.Manga : BookType.LightNovel;

                // Create WebsiteDropdown Component
                List<DiscordSelectComponentOption> optionsList = new List<DiscordSelectComponentOption>();
                foreach (string website in Helpers.GetRegionWebsiteListAsString(curRegion))
                {
                    optionsList.Add(new DiscordSelectComponentOption(website, website));
                }

                var dropdownComponents = new List<DiscordActionRowComponent>
                {
                    new([new DiscordSelectComponent("websiteDropdown", "Select Website(s)", optionsList, false, 1, optionsList.Count)]),
                    new([StockStatusFilterDropdownComponent])
                };

                // Create Membership Dropdown
                optionsList.Clear();
                string[] websiteMemberships = Helpers.GetMembershipWebsitesForRegionAsString(curRegion);
                if (websiteMemberships.Length != 0)
                {
                    optionsList.Add(new DiscordSelectComponentOption("None", "NONE"));
                    foreach (string website in websiteMemberships)
                    {
                        optionsList.Add(new DiscordSelectComponentOption(website, website));
                    }
                    dropdownComponents.Add(new([new DiscordSelectComponent("membershipDropdown", "Select Membership(s)", optionsList, false, 0, optionsList.Count)]));
                }

                var selectScrapeOptionsResponse = await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordMessageBuilder()
                            .WithContent("> **Enter Inputs**")
                            .AddComponents(dropdownComponents)));

                var interactivity = Kosuru.Client.GetShard(ctx.Guild).GetInteractivity();
                var selectionArray = await Task.WhenAll(interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "websiteDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "stockStatusFilterDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "membershipDropdown"));
                var thinkingResponse = await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordInteractionResponseBuilder().WithContent("**Kosuru Running...**").AsEphemeral(true)));

                if (!selectionArray.Any(component => component.Result == null))
                {
                    // Start scrape
                    string[] websiteSelection = selectionArray[0].Result.Values;
                    string[] stockStatusFilterSelection = selectionArray[1].Result.Values;
                    string[] membershipSelection = selectionArray[2].Result.Values;

                    Kosuru.Client.Logger.LogDebug($"Getting Information For -> Title = \"{title}\", Region = {region}, Format = {bookType}, Websites = [{string.Join(" , ", websiteSelection)}], Stock Status = [{string.Join(" , ", stockStatusFilterSelection)}], Memberships = [{string.Join(" , ", membershipSelection)}]");

                    // Delete the Select Menus
                    MasterScrape Scrape = new MasterScrape(GetStockStatus(stockStatusFilterSelection), curRegion)
                    {
                        IsBarnesAndNobleMember = IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection),
                        IsBooksAMillionMember = IsMember(BooksAMillion.WEBSITE_TITLE, membershipSelection),
                        IsKinokuniyaUSAMember = IsMember(KinokuniyaUSA.WEBSITE_TITLE, membershipSelection),
                        IsIndigoMember = IsMember(Indigo.WEBSITE_TITLE, membershipSelection)
                    };
                    await Scrape.InitializeScrapeAsync(title, bookType, Scrape.GenerateWebsiteList(websiteSelection));

                    if (Scrape.GetResults().Count > 0)
                    {
                        string scrapeResults = Scrape.GetResultsAsAsciiTable(title, bookType, false);
                        Debug.WriteLine(scrapeResults);

                        StringBuilder websites = new StringBuilder();
                        foreach (var resultUrl in Scrape.GetResultUrls()) { websites.AppendFormat("[{0}](<{1}>)", resultUrl.Key, resultUrl.Value).AppendLine(); }

                        await ctx.DeleteResponseAsync();
                        Thread.Sleep(500);

                        if (dm)
                        {
                            var dmChannel = await ctx.Member.CreateDmChannelAsync();
                            await dmChannel.SendMessageAsync(
                                new DiscordMessageBuilder()
                                    .WithContent($">>> **{websites}**")
                                    .AddFile("KosuruResults.txt", new MemoryStream(Encoding.UTF8.GetBytes(scrapeResults))));
                        }
                        else
                        {
                            await ctx.Channel.SendMessageAsync(
                                new DiscordMessageBuilder()
                                    .WithContent($">>> **{websites}**")
                                    .AddFile("KosuruResults.txt", new MemoryStream(Encoding.UTF8.GetBytes(scrapeResults))));
                        };
                    }
                    else // Kosuru found no data from user inputs
                    {
                        DiscordEmbedBuilder results = new DiscordEmbedBuilder
                        {
                            Title = ":frowning2: Kosuru Found No Data",
                            Color = Kosuru.COLOR,
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

                        await ctx.EditResponseAsync(
                            new DiscordWebhookBuilder(
                                new DiscordMessageBuilder()
                                    .WithContent(string.Empty)
                                    .AddEmbed(results)));
                    }
                }
                else // Delete input message if user doesn't respond in time and send a msg back
                {
                    Kosuru.Client.Logger.LogTrace($"User Not Responding in Time");
                    DiscordEmbedBuilder noResponseEmbed = new DiscordEmbedBuilder
                    {
                        Title = $":warning: Took to long to Respond! Try Again",
                        Color = Kosuru.COLOR,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    noResponseEmbed.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);

                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder(
                            new DiscordMessageBuilder()
                                .WithContent(string.Empty)
                                .AddEmbed(noResponseEmbed)));
                }
            }
            catch (Exception ex)
            {

                Kosuru.Client.Logger.LogError(ex, "Kosuru Crashed w/ \"{}\"", ex.Message);
                DiscordEmbedBuilder errorEmbed = new DiscordEmbedBuilder
                {
                    Title = $":bangbang: Kosuru Crashed! Try Again",
                    Color = Kosuru.COLOR,
                    Timestamp = DateTimeOffset.UtcNow
                };
                errorEmbed.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);

                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordMessageBuilder()
                            .WithContent(string.Empty)
                            .AddEmbed(errorEmbed)));
            }
        }

        [SlashCommand("list", "List the Current Available Websites for a Region")]
        [SlashCooldown(1, 5, SlashCooldownBucketType.User)]
        public async Task ListKosuruWebsitesCommand(InteractionContext ctx, [Choice("America", "America")][Choice("Australia", "Australia")][Choice("Britain", "Britain")][Choice("Canada", "Canada")][Choice("Europe", "Europe")][Option("Region", "Select Region")] string region)
        {
            await ctx.DeferAsync();
            Region curRegion = Helpers.GetRegionFromString(region);
            StringBuilder websites = new StringBuilder();

            foreach (string website in Helpers.GetRegionWebsiteListAsString(curRegion))
            {
                string link = website switch
                {
                    AmazonJapan.WEBSITE_TITLE => @"https://www.amazon.co.jp/",
                    AmazonUSA.WEBSITE_TITLE => @"https://www.amazon.com/",
                    BarnesAndNoble.WEBSITE_TITLE => @"https://www.barnesandnoble.com/",
                    BooksAMillion.WEBSITE_TITLE => @"https://www.booksamillion.com/",
                    CDJapan.WEBSITE_TITLE => @"https://www.cdjapan.co.jp/",
                    Crunchyroll.WEBSITE_TITLE => @"https://store.crunchyroll.com/",
                    ForbiddenPlanet.WEBSITE_TITLE => @"https://forbiddenplanet.com/",
                    Indigo.WEBSITE_TITLE => @"https://www.indigo.ca/en-ca/",
                    InStockTrades.WEBSITE_TITLE => @"https://www.instocktrades.com/",
                    KinokuniyaUSA.WEBSITE_TITLE => @"https://united-states.kinokuniya.com/",
                    MangaMate.WEBSITE_TITLE => @"https://mangamate.shop/",
                    MerryManga.WEBSITE_TITLE => @"https://www.merrymanga.com/",
                    RobertsAnimeCornerStore.WEBSITE_TITLE => @"https://www.animecornerstore.com/graphicnovels1.html",
                    SciFier.WEBSITE_TITLE => @$"https://scifier.com/?setCurrencyId={curRegion switch
                    {
                        Region.Britain => 1,
                        Region.America => 2,
                        Region.Australia => 3,
                        Region.Europe => 5,
                        Region.Canada => 6,
                        _ => throw new NotImplementedException()
                    }}",
                    SpeedyHen.WEBSITE_TITLE => @"https://www.speedyhen.com/",
                    Waterstones.WEBSITE_TITLE => @"https://www.waterstones.com/",
                    Wordery.WEBSITE_TITLE => @"https://wordery.com/",
                    _ => throw new NotImplementedException()
                };
                websites.AppendLine($"[{website}](<{link}>)");
            }

            DiscordEmbedBuilder results = new DiscordEmbedBuilder
            {
                Title = $"{GetRegionEmoji(curRegion)} Kosuru {region} Websites",
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow,
                Description = websites.ToString()
            };
            // results.WithThumbnail(Kosuru.Client.CurrentUser.AvatarUrl, 25, 25);
            results.WithAuthor("Kosuru", @"https://github.com/Sigrec/Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);
            results.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder(new DiscordMessageBuilder().AddEmbed(results)));
        }

        [SlashCommand("help", "Information About Kosuru")]
        [SlashCooldown(1, 5, SlashCooldownBucketType.User)]
        public async Task KosuruHelpCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            DiscordEmbedBuilder helpEmbed = new DiscordEmbedBuilder
            {
                Title = $":notepad_spiral: Kosuru Info",
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow,
                Description = "Scrapes websites from a given region for a manga or light novel series, and returns a list of compared prices for an entry in that series and outputs a list of the volumes with the lowest price. Users can additionally filter out entries based on stock status."
            };
            helpEmbed.WithThumbnail(Kosuru.Client.CurrentUser.AvatarUrl);
            helpEmbed.WithAuthor("Kosuru", @"https://github.com/Sigrec/Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);
            helpEmbed.WithFooter("Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);
            helpEmbed.AddField("Commands", "**/start** Starts Kosuru\n**/list** Lists the Current Available Websites for a Region");
            helpEmbed.AddField("Stock Status Filters", "**OOS** (Out of Stock)\n**PO** (Pre-Order)\n**BO** (Backorder)");
            helpEmbed.AddField("Website Requests", "If you want a website to be added submit a [form request](<https://google.com/>), I will respond at a later time whether this website is doable and will be added to the queue");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder(new DiscordMessageBuilder().AddEmbed(helpEmbed)));
        }

        public static StockStatus[] GetStockStatus(string[] selectedFilters)
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

        public static bool IsMember(string website, string[] memberships)
        {
            return !memberships.Contains("NONE") && memberships.Contains(website);
        }

        public static string GetRegionEmoji(Region region)
        {
            return region switch
            {
                Region.America => ":flag_us:",
                Region.Australia => ":flag_au:",
                Region.Britain => ":flag_gb:",
                Region.Canada => ":flag_ca:",
                Region.Europe => ":flag_eu:",
                Region.Japan => ":flag_jp:s",
                _ => throw new NotImplementedException(),
            };
        }
    }
}