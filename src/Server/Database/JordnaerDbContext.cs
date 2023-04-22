using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Database;

public class JordnaerDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<UserProfile> UserProfiles { get; set; } = default!;
    public DbSet<ChildProfile> ChildProfiles { get; set; } = default!;
    public DbSet<LookingFor> LookingFor { get; set; } = default!;

    //TODO
    //public DbSet<Group> Groups { get; set; } = default!;
    //public DbSet<ContactRequest> ContactRequests { get; set; } = default!;
    //public DbSet<Notification> Notifications { get; set; } = default!;
    //public DbSet<Event> Events { get; set; } = default!;

    public JordnaerDbContext(DbContextOptions<JordnaerDbContext> options) : base(options)
    { }
}
