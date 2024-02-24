using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Database;

public class ApplicationUser : IdentityUser
{
	/// <summary>
	/// Cookies may not be larger than 4096 bytes, so StringLength 10_000 should be plenty.
	/// <para>
	/// <seealso href="https://chromestatus.com/feature/4946713618939904"/>
	/// </para>
	/// </summary>
	[StringLength(10_000)]
	public string? Cookie { get; set; }
}
