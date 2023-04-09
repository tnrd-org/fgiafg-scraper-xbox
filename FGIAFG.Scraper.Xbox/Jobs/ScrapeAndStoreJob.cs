using FGIAFG.Scraper.Xbox.Database;
using FGIAFG.Scraper.Xbox.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Quartz;
using DbContext = FGIAFG.Scraper.Xbox.Database.DbContext;

namespace FGIAFG.Scraper.Xbox.Jobs;

internal class ScrapeAndStoreJob : IJob
{
    private readonly XboxScraper scraper;
    private readonly DbContext dbContext;

    public ScrapeAndStoreJob(XboxScraper scraper, DbContext dbContext)
    {
        this.scraper = scraper;
        this.dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;

        IEnumerable<FreeGame> freeGames = await scraper.Scrape(ct);
        if (ct.IsCancellationRequested)
            return;

        foreach (FreeGame freeGame in freeGames)
        {
            if (ct.IsCancellationRequested)
                return;

            string hash = freeGame.CalculatePersistentHash();

            if (await dbContext.Games.AnyAsync(x => x.Hash == hash, ct))
                continue;

            EntityEntry<GameModel> entry = dbContext.Games.Add(new GameModel()
            {
                Title = freeGame.Title,
                EndDate = freeGame.EndDate,
                Url = freeGame.Url,
                ImageUrl = freeGame.ImageUrl,
                StartDate = freeGame.StartDate,
                Hash = hash
            });
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
