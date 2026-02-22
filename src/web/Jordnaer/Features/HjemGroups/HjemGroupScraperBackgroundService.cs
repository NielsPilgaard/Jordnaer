using Microsoft.Extensions.Options;

namespace Jordnaer.Features.HjemGroups;

public class HjemGroupScraperBackgroundService(
    HjemGroupScraperService scraperService,
    IOptions<HjemGroupScraperOptions> options,
    ILogger<HjemGroupScraperBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await scraperService.ScrapeAndSaveAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in HJEM group scraper background service.");
            }

            try
            {
                await Task.Delay(options.Value.Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
