using Jordnaer.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Jordnaer.Components.Account.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
	private readonly SignInManager<ApplicationUser> _signInManager;
	private readonly ILogger<LogoutModel> _logger;

	[BindProperty(SupportsGet = true)]
	public string? ReturnUrl { get; set; }

	public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
	{
		_signInManager = signInManager;
		_logger = logger;
	}

	public async Task<IActionResult> OnPost()
	{
		await _signInManager.SignOutAsync();

		_logger.LogInformation("User logged out.");

		return ReturnUrl is not null
				   ? LocalRedirect($"~/{ReturnUrl}")
				   : LocalRedirect("~/");
	}
}