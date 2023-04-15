using Jordnaer.Server.Features.Profile;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Database;

public class JordnaerDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Parent> Parents { get; set; } = default!;
    public DbSet<Child> Children { get; set; } = default!;
    //TODO
    //public DbSet<Group> Groups { get; set; } = default!;
    //public DbSet<ContactRequest> ContactRequests { get; set; } = default!;
    //public DbSet<Notification> Notifications { get; set; } = default!;
    //public DbSet<Event> Events { get; set; } = default!;

    public JordnaerDbContext(DbContextOptions<JordnaerDbContext> options) : base(options)
    { }
}
