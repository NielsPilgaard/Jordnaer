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
        },
        // new AdData
        // {
        //     Title = "Hjemmeunger af Mie Storm",
        //     Description = "Hjemmeunger - En guide til livet uden institution",
        //     ImagePath = "images/ads/hjemmeunger.jpg",
        //     Link = "https://muusmann-forlag.dk/hjemmeunger/",
        //     LabelColor = "#3d3737"
        // },

    ];

    /// <summary>
    /// Get all available hardcoded ads.
    /// </summary>
    public static List<AdData> GetAll() => [.. _ads];
}

public record AdData
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ImagePath { get; init; }
    public required string Link { get; init; }
    /// <summary>
    /// Partner ID for analytics tracking. Null for hardcoded ads.
    /// </summary>
    public Guid? PartnerId { get; init; }
    /// <summary>
    /// Optional custom background color for the "Annonce" label (hex format like #FFFFFF).
    /// </summary>
    public string? LabelColor { get; init; }
}
