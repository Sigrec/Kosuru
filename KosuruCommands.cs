using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using MangaAndLightNovelWebScrape;
using MangaAndLightNovelWebScrape.Enums;
using MangaAndLightNovelWebScrape.Models;
using MangaAndLightNovelWebScrape.Websites;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Kosuru
{
    [SlashCommandGroup(Kosuru.NAME, "Kosuru Commands")]
    public class KosuruCommands : ApplicationCommandModule
    {
        public MasterScrape Scrape { private get; set; }
        public DiscordInteraction WebsiteDropdownInteraction { get; set; }

        // TODO - User unable (only mod) to delete final messages, find a better way of outputting messages?
        // TODO - Add link to changelog/updates to help command or create new command?
        // TODO - Issue where "Is Running" stays when there is a error
        [SlashCommand("start", "Start Kosuru")]
        [SlashCooldown(1, 120, SlashCooldownBucketType.User)]
        public async Task KosuruCommand(InteractionContext ctx, [Option("Title", "Enter Title")] string title, [Choice("America", "America")][Choice("Australia", "Australia")][Choice("Britain", "Britain")][Choice("Canada", "Canada")][Choice("Europe", "Europe")][Option("Region", "Select Region")] string region, [Choice("Manga", "MANGA")][Choice("Light Novel", "NOVEL")][Option("Format", "Manga or Light Novel")] string format, [Option("DM", "Direct Message Results?")] bool dm, [Option("Mobile", "Print results in a mobile friendly format")] bool mobile = false)
        {
            // Get input for the scrape from user
            ctx.SlashCommandsExtension.SlashCommandErrored += OnErrorOccured;
            await ctx.DeferAsync(true);
            Region curRegion = Helpers.GetRegionFromString(region);
            BookType bookType = format == "MANGA" ? BookType.Manga : BookType.LightNovel;

            // Create WebsiteDropdown Component
            List<DiscordSelectComponentOption> optionsList = new List<DiscordSelectComponentOption>();
            foreach (string website in Helpers.GetRegionWebsiteListAsString(curRegion)) { optionsList.Add(new DiscordSelectComponentOption(website, website)); }

            var dropdownComponents = new List<DiscordActionRowComponent>
            {
                new([new DiscordSelectComponent("websiteDropdown", "Select Website(s)", optionsList, false, 1, optionsList.Count)]),
                new([Kosuru.StockStatusFilterDropdownComponent])
            };

            // Create Membership Dropdown
            optionsList.Clear();
            string[] websiteMemberships = Helpers.GetMembershipWebsitesForRegionAsString(curRegion);
            if (websiteMemberships.Length != 0)
            {
                optionsList.Add(new DiscordSelectComponentOption("None", "NONE"));
                foreach (string website in websiteMemberships) { optionsList.Add(new DiscordSelectComponentOption(website, website)); }
                dropdownComponents.Add(new([new DiscordSelectComponent("membershipDropdown", "Select Membership(s)", optionsList, false, 0, optionsList.Count)]));
            }

            var selectScrapeOptionsResponse = await ctx.EditResponseAsync(
                new DiscordWebhookBuilder(
                    new DiscordInteractionResponseBuilder()
                        .AddComponents(dropdownComponents)));

            var interactivity = Kosuru.Client.GetShard(ctx.Guild).GetInteractivity();
            var selectionArray = websiteMemberships.Length != 0 ? await Task.WhenAll(interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "websiteDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "stockStatusFilterDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "membershipDropdown")) : await Task.WhenAll(interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "websiteDropdown"), interactivity.WaitForSelectAsync(selectScrapeOptionsResponse, ctx.User, "stockStatusFilterDropdown"));

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder(
                    new DiscordInteractionResponseBuilder().WithContent("**Kosuru Running...**").AsEphemeral(true)));

            if (!selectionArray.Any(component => component.Result == null)) 
            {
                // Start scrape
                string[] websiteSelection = selectionArray[0].Result.Values;
                string[] stockStatusFilterSelection = selectionArray[1].Result.Values;
                string[] membershipSelection = [];

                Scrape = new MasterScrape(GetStockStatus(stockStatusFilterSelection), curRegion);
                if (websiteMemberships.Length != 0)
                {
                    membershipSelection = selectionArray[2].Result.Values;
                    Scrape.IsBarnesAndNobleMember = IsMember(BarnesAndNoble.WEBSITE_TITLE, membershipSelection);
                    Scrape.IsBooksAMillionMember = IsMember(BooksAMillion.WEBSITE_TITLE, membershipSelection);
                    Scrape.IsKinokuniyaUSAMember = IsMember(KinokuniyaUSA.WEBSITE_TITLE, membershipSelection);
                    Scrape.IsIndigoMember = IsMember(Indigo.WEBSITE_TITLE, membershipSelection);
                }

                Kosuru.Client.Logger.LogDebug($"Getting Information For -> Title = \"{title}\", Region = {region}, Format = {bookType}, Websites = [{string.Join(" , ", websiteSelection)}], Stock Status = [{string.Join(" , ", stockStatusFilterSelection)}]{(membershipSelection.Length != 0 ? $", Memberships = [{string.Join(" , ", membershipSelection)}]" : string.Empty)}");
                await Scrape.InitializeScrapeAsync(title, bookType, Scrape.GenerateWebsiteList(websiteSelection));

                if (Scrape.GetResults().Count > 0)
                {
                    string scrapeResults;
                    if (!mobile)
                    {
                        scrapeResults = Scrape.GetResultsAsAsciiTable(title, bookType, false);
                    }
                    else
                    {
                        StringBuilder results = new StringBuilder();
                        results.AppendFormat("Title: \"{0}\"", title).AppendLine();
                        results.AppendFormat("BookType: {0}", bookType.ToString()).AppendLine();
                        results.AppendFormat("Region: {0}", region).AppendLine();
                        Scrape.GetResults().ForEach(entry => results.AppendLine(entry.ToString()));
                        scrapeResults = results.ToString();
                    }

                    StringBuilder websites = new StringBuilder();
                    foreach (var resultUrl in Scrape.GetResultUrls()) { websites.AppendFormat("[{0}](<{1}>)", resultUrl.Key, resultUrl.Value).AppendLine(); }

                    await ctx.DeleteResponseAsync();
                    var resultMessage = new DiscordMessageBuilder()
                                .WithContent($">>> **{websites}**")
                                .AddFile("KosuruResults.txt", new MemoryStream(Encoding.UTF8.GetBytes(scrapeResults)));
                    if (dm)
                    {
                        var dmChannel = await ctx.Member.CreateDmChannelAsync();
                        await dmChannel.SendMessageAsync(resultMessage);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync(resultMessage);
                    }
                }
                else // Kosuru found no data from user inputs
                {
                    DiscordEmbedBuilder results = new DiscordEmbedBuilder
                    {
                        Color = Kosuru.COLOR,
                        Timestamp = DateTimeOffset.UtcNow,
                        Description = "### :x: Kosuru Found No Data\n*Kosuru Found No Data for Below Inputs*"
                    };
                    results.WithThumbnail(Kosuru.Client.CurrentUser.AvatarUrl, 25, 25);
                    results.AddField("Title", title, true);
                    results.AddField("Region", curRegion.ToString(), true);
                    results.AddField("Book Type", bookType.ToString(), true);
                    if (!membershipSelection.Contains("NONE")) { results.AddField("Membership(s)", string.Join("\n", membershipSelection), true); }
                    results.AddField("Website(s)", string.Join("\n", websiteSelection), true);
                    results.WithAuthor(Kosuru.NAME, @"https://github.com/Sigrec/Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);
                    results.WithFooter(Kosuru.NAME, Kosuru.Client.CurrentUser.AvatarUrl);

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
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordMessageBuilder()
                            .WithContent(string.Empty)
                            .AddEmbed(Kosuru.NoResponseEmbed)));
            }
        }

        [SlashCommand("list", "List the Current Available Websites for a Region")]
        [SlashCooldown(1, 60, SlashCooldownBucketType.User)]
        public async Task ListKosuruWebsitesCommand(InteractionContext ctx, [Choice("America", "America")][Choice("Australia", "Australia")][Choice("Britain", "Britain")][Choice("Canada", "Canada")][Choice("Europe", "Europe")][Option("Region", "Select Region")] string region)
        {
            await ctx.DeferAsync();
            ctx.SlashCommandsExtension.SlashCommandErrored += OnErrorOccured;
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
                    TravellingMan.WEBSITE_TITLE => @"https://travellingman.com/",
                    Waterstones.WEBSITE_TITLE => @"https://www.waterstones.com/",
                    // Wordery.WEBSITE_TITLE => @"https://wordery.com/",
                    _ => throw new NotImplementedException()
                };
                websites.AppendFormat("[{0}](<{1}>)", website, link).AppendLine();
            }

            DiscordEmbedBuilder results = new DiscordEmbedBuilder
            {
                Color = Kosuru.COLOR,
                Timestamp = DateTimeOffset.UtcNow,
                Description = $"### {GetRegionEmoji(curRegion)} Kosuru {region} Websites\n{websites}"
            };
            results.WithAuthor(Kosuru.NAME, @"https://github.com/Sigrec/Kosuru", Kosuru.Client.CurrentUser.AvatarUrl);
            results.WithFooter(Kosuru.NAME, Kosuru.Client.CurrentUser.AvatarUrl);

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder(
                    new DiscordMessageBuilder()
                        .AddEmbed(results)));
        }

        [SlashCommand("help", "Information About Kosuru")]
        [SlashCooldown(1, 30, SlashCooldownBucketType.User)]
        public async Task KosuruHelpCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            ctx.SlashCommandsExtension.SlashCommandErrored += OnErrorOccured;
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder(
                    new DiscordMessageBuilder()
                        .AddEmbed(Kosuru.HelpEmbed)));
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

        private static string GetRegionEmoji(Region region)
        {
            return region switch
            {
                Region.America => ":flag_us:",
                Region.Australia => ":flag_au:",
                Region.Britain => ":flag_gb:",
                Region.Canada => ":flag_ca:",
                Region.Europe => ":flag_eu:",
                Region.Japan => ":flag_jp:",
                _ => throw new NotImplementedException(),
            };
        }

        private static async Task OnErrorOccured(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            if (e.Exception is SlashExecutionChecksFailedException)
            {
                TimeSpan rawTime = ((SlashCooldownAttribute)(e.Exception as SlashExecutionChecksFailedException).FailedChecks[0]).GetRemainingCooldown(e.Context);
                string timeLeft = $"{rawTime.Seconds}s";

                await e.Context.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordMessageBuilder()
                        .AddEmbed(Kosuru.CooldownEmbed.WithDescription($"### :hourglass_flowing_sand: Kosuru Command on Cooldown, Wait {(rawTime.Minutes != 0 ? timeLeft.Insert(0, $"{rawTime.Minutes}m ") : timeLeft)}"))));
            }
            else
            {
                // Kosuru.Client.Logger.LogError(e.Exception, "Kosuru Slash Command Error -> \"{}\"", e.Exception.Message);
                await e.Context.EditResponseAsync(
                    new DiscordWebhookBuilder(
                        new DiscordMessageBuilder()
                            .AddEmbed(Kosuru.CrashEmbed)));
            }
            throw e.Exception;
        }
    }
}