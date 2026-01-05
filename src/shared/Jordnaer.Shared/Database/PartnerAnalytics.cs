using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

/// <summary>
/// Daily aggregated analytics data for partner ads
/// </summary>
[Index(nameof(PartnerId), nameof(Date), IsUnique = true)]
public class PartnerAnalytics
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	public required Guid PartnerId { get; set; }

	/// <summary>
	/// The date for which these analytics apply (UTC date only, no time component)
	/// </summary>
	[Required]
	public required DateTime Date { get; set; }

	/// <summary>
	/// Total number of times the ad was displayed on this date
	/// </summary>
	public int Impressions { get; set; }

	/// <summary>
	/// Total number of clicks on the ad on this date
	/// </summary>
	public int Clicks { get; set; }

	/// <summary>
	/// Navigation property to the partner
	/// </summary>
	public Partner? Partner { get; set; }
}
