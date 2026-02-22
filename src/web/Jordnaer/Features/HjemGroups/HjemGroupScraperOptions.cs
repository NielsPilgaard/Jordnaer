namespace Jordnaer.Features.HjemGroups;

public class HjemGroupScraperOptions
{
    public const string SectionName = "HjemGroupScraper";

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);
}
