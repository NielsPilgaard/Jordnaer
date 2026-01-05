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
	/// Mobile image URL for screens < 768px
	/// </summary>
	[Url]
	public string? MobileImageUrl { get; set; }

	/// <summary>
	/// Desktop image URL for screens >= 768px
	/// </summary>
	[Url]
	public string? DesktopImageUrl { get; set; }

	/// <summary>
	/// Pending mobile image URL awaiting admin approval
	/// </summary>
	[Url]
	public string? PendingMobileImageUrl { get; set; }

	/// <summary>
	/// Pending desktop image URL awaiting admin approval
	/// </summary>
	[Url]
	public string? PendingDesktopImageUrl { get; set; }

	/// <summary>
	/// Timestamp of the last image update (UTC)
	/// </summary>
	public DateTime? LastImageUpdateUtc { get; set; }

	/// <summary>
	/// Indicates whether there are pending images awaiting approval
	/// </summary>
	public bool HasPendingImageApproval { get; set; }

	/// <summary>
	/// The user ID associated with this partner account
	/// </summary>
	[Required]
	public required string UserId { get; set; }

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

	public List<PartnerAnalytics> Analytics { get; set; } = [];
}
