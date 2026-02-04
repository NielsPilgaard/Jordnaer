using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jordnaer.Shared.Validation;

namespace Jordnaer.Shared;

public class Partner
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public Guid Id { get; set; }

	/// <summary>
	/// Partner name. Optional. Displayed on partner card if set, and <see cref="CanHavePartnerCard"/> is <c>true</c>.
	/// </summary>
	[MinLength(2, ErrorMessage = "Partner navn skal være mindst 2 karakterer langt.")]
	[MaxLength(128, ErrorMessage = "Partner navn må højest være 128 karakterer langt.")]
	public string? Name { get; set; }

	/// <summary>
	/// Partner description. Optional. Displayed on partner card if set, and <see cref="CanHavePartnerCard"/> is <c>true</c>.
	/// </summary>
	[MaxLength(500, ErrorMessage = "Partner beskrivelse må højest være 500 karakterer lang.")]
	public string? Description { get; set; }

	/// <summary>
	/// Partner logo URL. Optional. Displayed on partner card if set, and <see cref="CanHavePartnerCard"/> is <c>true</c>.
	/// </summary>
	[Url(ErrorMessage = "Logo URL skal være en gyldig URL.")]
	public string? LogoUrl { get; set; }

	/// <summary>
	/// Partner website link. Optional. Clicking on partner card directs to this if set, and <see cref="CanHavePartnerCard"/> is <c>true</c>.
	/// </summary>
	[Url(ErrorMessage = "Partner side URL skal være en gyldig URL.")]
	public string? PartnerPageLink { get; set; }

	/// <summary>
	/// Link used when users click on ads in search results. If not set, falls back to <see cref="PartnerPageLink"/>.
	/// </summary>
	[Url(ErrorMessage = "Annonce URL skal være en gyldig URL.")]
	public string? AdLink { get; set; }

	/// <summary>
	/// Background color for the "Annonce" label on ads. Stores hex color like "#FFFFFF".
	/// If not set, uses the default dark semi-transparent background.
	/// </summary>
	[MaxLength(7)]
	[HexColor]
	public string? AdLabelColor { get; set; }

	/// <summary>
	/// Ad image URL (9:16 or 1:1 aspect ratio recommended)
	/// </summary>
	[Url(ErrorMessage = "Annonce billede URL skal være en gyldig URL.")]
	public string? AdImageUrl { get; set; }

	/// <summary>
	/// Pending ad image URL awaiting admin approval
	/// </summary>
	[Url(ErrorMessage = "Annonce billede URL skal være en gyldig URL.")]
	public string? PendingAdImageUrl { get; set; }

	/// <summary>
	/// Pending partner name awaiting admin approval
	/// </summary>
	[MinLength(2, ErrorMessage = "Partner navn skal være mindst 2 karakterer langt.")]
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
	[Url(ErrorMessage = "Logo URL skal være en gyldig URL.")]
	public string? PendingLogoUrl { get; set; }

	/// <summary>
	/// Pending partner page link awaiting admin approval
	/// </summary>
	[Url(ErrorMessage = "Partner side URL skal være en gyldig URL.")]
	public string? PendingPartnerPageLink { get; set; }

	/// <summary>
	/// Pending ad link awaiting admin approval
	/// </summary>
	[Url(ErrorMessage = "Annonce URL skal være en gyldig URL.")]
	public string? PendingAdLink { get; set; }

	/// <summary>
	/// Pending ad label color awaiting admin approval
	/// </summary>
	[MaxLength(7)]
	[HexColor]
	public string? PendingAdLabelColor { get; set; }

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

	/// <summary>
	/// Whether this partner is allowed to have ad images
	/// </summary>
	public bool CanHaveAd { get; set; } = true;

	/// <summary>
	/// Whether this partner is allowed to have a partner card on the /partners page
	/// </summary>
	public bool CanHavePartnerCard { get; set; } = true;

	public DateTime CreatedUtc { get; set; }

	public List<PartnerAnalytics> Analytics { get; set; } = [];

	/// <summary>
	/// Determines if this partner has a partner card (displayed on /partners page)
	/// <para>
	/// The link must be set, and at least one of logo URL, description, or name must be set.
	/// </para>
	/// </summary>
	public bool HasPartnerCard => CanHavePartnerCard &&
								   !string.IsNullOrWhiteSpace(PartnerPageLink) &&
								   (!string.IsNullOrWhiteSpace(LogoUrl) ||
								   !string.IsNullOrWhiteSpace(Description) ||
								   !string.IsNullOrWhiteSpace(Name));

	/// <summary>
	/// Determines if this partner has an ad image (for ad display)
	/// </summary>
	public bool HasAdImage => CanHaveAd && !string.IsNullOrWhiteSpace(AdImageUrl);

	/// <summary>
	/// Validates that the partner has at least one type of presence (partner card or ad image)
	/// </summary>
	public bool IsValid()
	{
		// Must have either a partner card or an ad image
		var hasValidPartnerCard = HasPartnerCard;
		var hasValidAdImage = HasAdImage;

		if (!hasValidPartnerCard && !hasValidAdImage)
		{
			return false;
		}

		return true;
	}
}
