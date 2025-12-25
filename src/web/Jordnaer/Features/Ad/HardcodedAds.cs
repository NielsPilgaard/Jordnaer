namespace Jordnaer.Features.Ad;

/// <summary>
/// Temporary hardcoded ads until the database-backed ad system is implemented (Task 03).
/// This keeps ad data separate from UI logic.
/// </summary>
public static class HardcodedAds
{
    private static readonly List<AdData> _ads =
    [
        new AdData
        {
            Title = "Moon Creative",
            Description = "Professionel webudvikling og design",
            ImagePath = "images/ads/mooncreative_mobile.png",
            Link = "https://www.mooncreative.dk/"
        }
    ];

    /// <summary>
    /// Get ads for user search results.
    /// Returns multiple copies if needed to fill the requested count.
    /// </summary>
    public static List<AdData> GetAdsForSearch(int count)
    {
        if (count <= 0 || _ads.Count == 0)
        {
            return [];
        }

        var result = new List<AdData>();

        // Repeat ads to fill the requested count
        for (int i = 0; i < count; i++)
        {
            result.Add(_ads[i % _ads.Count]);
        }

        return result;
    }
}

public record AdData
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ImagePath { get; init; }
    public required string Link { get; init; }
}
