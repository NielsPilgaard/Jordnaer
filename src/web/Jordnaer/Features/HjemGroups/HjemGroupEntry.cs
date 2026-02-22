namespace Jordnaer.Features.HjemGroups;

public record HjemGroupEntry
{
    public required string Name { get; init; }
    public required Uri WebsiteUrl { get; init; }
    public string? City { get; init; }
    public int? ZipCode { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required HjemGroupType Type { get; init; }
    public string? IconUrl { get; init; }
}

public enum HjemGroupType { Lokalafdeling, Lokalrepresentant }
