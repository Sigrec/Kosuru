using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Kosuru.CommandAttributes
{
    public class RegionAttribute : CheckBaseAttribute
    {
        public string Region { get; private set; }

        public RegionAttribute(string Region)
        {
            this.Region = Region;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            try
            {
                MangaAndLightNovelWebScrape.Helpers.GetRegionFromString(Region);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
    }
}