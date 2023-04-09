using System.Text.RegularExpressions;

namespace FGIAFG.Scraper.Xbox.Scraping;

internal partial class XboxScraper
{
    private readonly ILogger<XboxScraper> logger;
    private readonly HttpClient httpClient;

    public XboxScraper(ILogger<XboxScraper> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<IEnumerable<FreeGame>> Scrape(CancellationToken ct = default)
    {
        HttpResponseMessage response =
            await httpClient.GetAsync("https://www.xbox.com/en-US/live/gold/js/gwg-globalContent.js", ct);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Unable to get games with gold content");
            return Array.Empty<FreeGame>();
        }

        string content = await response.Content.ReadAsStringAsync(ct);

        if (!TryGetSectionMatch(content, out Match? sectionMatch))
            return Array.Empty<FreeGame>();

        if (!TryGetItemMatches(sectionMatch!.Value, out MatchCollection? itemMatches))
            return Array.Empty<FreeGame>();

        List<FreeGame> games = new List<FreeGame>();

        foreach (Match match in itemMatches!)
        {
            logger.LogInformation("Parsing match");
            FreeGame? freeGame = MatchParser.ParseMatch(match);

            if (freeGame == null)
            {
                logger.LogInformation("Skipping match because it didn't result in a free game");
            }
            else
            {
                games.Add(freeGame);
                logger.LogInformation("Added free game: {Title}", freeGame.Title);
            }
        }

        return games;
    }

    private bool TryGetSectionMatch(string content, out Match? sectionMatch)
    {
        logger.LogInformation("Trying to match section");
        sectionMatch = null;

        if (!SectionRegex().IsMatch(content))
        {
            logger.LogError("Unable to match section pattern");
            return false;
        }

        sectionMatch = SectionRegex().Match(content);
        return true;
    }

    private bool TryGetItemMatches(string content, out MatchCollection? itemMatches)
    {
        logger.LogInformation("Trying to match items");
        itemMatches = null;

        if (!ItemRegex().IsMatch(content))
        {
            logger.LogError("Unable to match item pattern");
            return false;
        }

        itemMatches = ItemRegex().Matches(content);
        return true;
    }

    [GeneratedRegex("(?<=(\"en-us\":\\s)){[\\s\\S]+?}")]
    private static partial Regex SectionRegex();

    [GeneratedRegex("(\"keyCopytitlenowgame)[\\s\\S]+?((?<=\"keyPlaysonnowgame).+,)")]
    private static partial Regex ItemRegex();
}
