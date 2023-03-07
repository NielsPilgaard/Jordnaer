using System.Security.Claims;
using RemindMeApp.Server.Data;

namespace RemindMeApp.Server.Authorization;

// A scoped service that exposes the current user information
public class CurrentUser
{
    public ApplicationUser? User { get; set; }
    public ClaimsPrincipal? Principal { get; set; } = default!;

    public string Id => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public bool? IsAdmin => Principal?.IsInRole("admin");
}
