using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jordnaer.Components.Account.Pages;

public class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
	[BindProperty(SupportsGet = true)]
	public string? ReturnUrl { get; set; }
	public void OnGet(){}

	public async Task<IActionResult> OnPostAsync()
	{
		await HttpContext.SignOutAsync();
		await signInManager.SignOutAsync();

		JordnaerMetrics.LogoutCounter.Add(1);

		return LocalRedirect(ReturnUrl ?? "~/");
	}
}