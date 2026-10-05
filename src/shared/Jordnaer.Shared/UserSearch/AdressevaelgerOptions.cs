using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class AdressevaelgerOptions
{
	public const string SectionName = "Adressevaelger";

	[Url]
	[Required(ErrorMessage = "Påkrævet.")]
	public required string BaseUrl { get; set; }

	/// <summary>
	/// Required on every request. Until Klimadatastyrelsen introduces user management,
	/// any string of at least 10 characters is accepted, and they recommend <c>adressevaelger123</c>.
	/// </summary>
	[Required(ErrorMessage = "Påkrævet.")]
	[MinLength(10)]
	public required string Token { get; set; }
}
