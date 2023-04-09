using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Data;

public class JordnaerDbContext : IdentityDbContext<ApplicationUser>
{
    public JordnaerDbContext(
        DbContextOptions<JordnaerDbContext> options) : base(options)
    {
    }
}
