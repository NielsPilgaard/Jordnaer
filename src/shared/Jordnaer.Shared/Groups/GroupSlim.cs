namespace Jordnaer.Shared;

public class GroupSlim
{
	public Guid Id { get; set; }

	public required string Name { get; set; }

	public string? ProfilePictureUrl { get; set; }

	public required string ShortDescription { get; set; }
	public required string? Description { get; set; }

	public string? Address { get; set; }
	public int? ZipCode { get; set; }
	public string? City { get; set; }

	/// <summary>
	/// Latitude of the exact location (from Address) or zip code center.
	/// </summary>
	public double? Latitude { get; set; }

	/// <summary>
	/// Longitude of the exact location (from Address) or zip code center.
	/// </summary>
	public double? Longitude { get; set; }

	/// <summary>
	/// Latitude of the zip code center. Used for non-members when full address exists.
	/// </summary>
	public double? ZipCodeLatitude { get; set; }

	/// <summary>
	/// Longitude of the zip code center. Used for non-members when full address exists.
	/// </summary>
	public double? ZipCodeLongitude { get; set; }

	public int MemberCount { get; set; }

	public required string[] Categories { get; set; }
}
