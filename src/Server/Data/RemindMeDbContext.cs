using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RemindMeApp.Server.Reminders;

namespace RemindMeApp.Server.Data;

public class RemindMeDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Reminder> Reminders => Set<Reminder>();

    public RemindMeDbContext(
        DbContextOptions<RemindMeDbContext> options) : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Reminder>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(reminder => reminder.OwnerId)
            .HasPrincipalKey(applicationUser => applicationUser.UserName);

        base.OnModelCreating(builder);
    }
}
