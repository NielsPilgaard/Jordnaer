using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(Name), IsUnique = true)]
public class Partner
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	[Required]
	[MinLength(2, ErrorMessage = "Partner navn skal være mindst 2 karakterer langt.")]
	[MaxLength(128, ErrorMessage = "Partner navn må højest være 128 karakterer langt.")]
	public required string Name { get; set; }

	[Required]
	[MaxLength(500, ErrorMessage = "Partner beskrivelse må højest være 500 karakterer lang.")]
	public required string Description { get; set; }

	[Url]
	public string? LogoUrl { get; set; }

	[Url]
	[Required(ErrorMessage = "Partner link er påkrævet.")]
	public required string Link { get; set; }

	/// <summary>
	/// Ad image URL (9:16 or 1:1 aspect ratio recommended)
	/// </summary>
	[Url]
	public string? AdImageUrl { get; set; }

	/// <summary>
	/// Pending ad image URL awaiting admin approval
	/// </summary>
	[Url]
	public string? PendingAdImageUrl { get; set; }

	/// <summary>
	/// Pending partner name awaiting admin approval
	/// </summary>
	[MaxLength(128)]
	public string? PendingName { get; set; }

	/// <summary>
	/// Pending partner description awaiting admin approval
	/// </summary>
	[MaxLength(500)]
	public string? PendingDescription { get; set; }

	/// <summary>
	/// Pending logo URL awaiting admin approval
	/// </summary>
	[Url]
	public string? PendingLogoUrl { get; set; }

	/// <summary>
	/// Pending partner link awaiting admin approval
	/// </summary>
	[Url]
	public string? PendingLink { get; set; }

	/// <summary>
	/// Timestamp of the last update (UTC)
	/// </summary>
	public DateTime? LastUpdateUtc { get; set; }

	/// <summary>
	/// Indicates whether there are pending changes awaiting approval
	/// </summary>
	public bool HasPendingApproval { get; set; }

	/// <summary>
	/// The user ID associated with this partner account
	/// </summary>
	[Required]
	public required string UserId { get; set; }

	public DateTime CreatedUtc { get; set; }

	public List<PartnerAnalytics> Analytics { get; set; } = [];
}
