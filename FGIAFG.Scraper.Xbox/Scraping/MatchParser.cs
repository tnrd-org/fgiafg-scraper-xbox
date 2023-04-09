using System.Text.RegularExpressions;

namespace FGIAFG.Scraper.Xbox.Scraping;

internal class MatchParser
{
    public static FreeGame? ParseMatch(Capture match)
    {
        string[] splits = match.Value.Split('\n');
        string title = ParseTitle(splits);
        if (string.IsNullOrEmpty(title) || title == "####")
            return null;

        string image = ParseImage(splits);
        string url = ParseUrl(splits);
        ParseDates(splits, out DateTime startDate, out DateTime endDate);

        return new FreeGame(title, image, url, startDate, endDate);
    }

    private static string ParseTitle(string[] splits)
    {
        return GetContentFromLines("keyCopytitlenowgame", splits);
    }

    private static string ParseImage(string[] splits)
    {
        return GetContentFromLines("keyImagenowgame", splits);
    }

    private static string ParseUrl(string[] splits)
    {
        return GetContentFromLines("keyLinknowgame", splits);
    }

    private static void ParseDates(string[] splits, out DateTime startDate, out DateTime endDate)
    {
        string content = GetContentFromLines("keyCopydatesnowgame", splits);
        string[] dates = content.Split('–');

        startDate = DateTime.ParseExact(dates[0].Trim(), "M/d", null);
        endDate = DateTime.ParseExact(dates[1].Trim(), "M/d", null);

        if (startDate.Month > endDate.Month)
        {
            endDate = endDate.AddYears(1);
        }
    }

    private static string GetContentFromLines(string key, string[] lines)
    {
        foreach (string line in lines)
        {
            string substring = line[..line.IndexOf(':')]
                .Trim(' ', '"');

            if (!substring.StartsWith(key))
                continue;

            substring = line[(line.IndexOf(':') + 1)..]
                .Trim(' ', ',', '"');

            return substring;
        }

        return string.Empty;
    }
}
