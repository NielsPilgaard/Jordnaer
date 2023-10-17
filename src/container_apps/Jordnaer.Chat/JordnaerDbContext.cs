using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Chat;

public class JordnaerDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; } = default!;
    public DbSet<ChildProfile> ChildProfiles { get; set; } = default!;
    public DbSet<LookingFor> LookingFor { get; set; } = default!;
    public DbSet<UserProfileLookingFor> UserProfileLookingFor { get; set; } = default!;
    public DbSet<UserContact> UserContacts { get; set; } = default!;
    public DbSet<Shared.Chat> Chats { get; set; } = default!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = default!;
    public DbSet<UnreadMessage> UnreadMessages { get; set; } = default;
    public DbSet<UserChat> UserChats { get; set; } = default!;

    //TODO
    //public DbSet<Group> Groups { get; set; } = default!;
    //public DbSet<ContactRequest> ContactRequests { get; set; } = default!;
    //public DbSet<Notification> Notifications { get; set; } = default!;
    //public DbSet<Event> Events { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>()
            .HasMany(userProfile => userProfile.LookingFor)
            .WithMany()
            .UsingEntity<UserProfileLookingFor>();

        modelBuilder.Entity<UserProfile>()
            .HasMany(userProfile => userProfile.Contacts)
            .WithMany()
            .UsingEntity<UserContact>(
                builder => builder
                    .HasOne<UserProfile>()
                    .WithMany()
                    .HasForeignKey(userContact => userContact.UserProfileId),
                builder => builder
                    .HasOne<UserProfile>()
                    .WithMany()
                    .HasForeignKey(userContact => userContact.ContactId));

        modelBuilder.Entity<Shared.Chat>()
            .HasMany(userProfile => userProfile.Recipients)
            .WithMany()
            .UsingEntity<UserChat>();

        modelBuilder.Entity<UserProfile>()
            .Property(user => user.SearchableName)
            .HasComputedColumnSql(
                $"ISNULL([{nameof(UserProfile.FirstName)}], '') + ' ' + " +
                $"ISNULL([{nameof(UserProfile.LastName)}], '') + ' ' + " +
                $"ISNULL([{nameof(UserProfile.UserName)}], '')",
                stored: true);


        modelBuilder.Entity<UserProfile>()
            .Property(user => user.Age)
            .HasComputedColumnSql(
                $"DATEDIFF(YY, [{nameof(UserProfile.DateOfBirth)}], GETDATE()) - " +
                $"CASE WHEN MONTH([{nameof(UserProfile.DateOfBirth)}]) > MONTH(GETDATE()) " +
                $"OR MONTH([{nameof(UserProfile.DateOfBirth)}]) = MONTH(GETDATE()) " +
                $"AND DAY([{nameof(UserProfile.DateOfBirth)}]) > DAY(GETDATE()) " +
                $"THEN 1 ELSE 0 END");

        modelBuilder.Entity<ChildProfile>()
            .Property(child => child.Age)
            .HasComputedColumnSql(
                $"DATEDIFF(YY, [{nameof(ChildProfile.DateOfBirth)}], GETDATE()) - " +
                $"CASE WHEN MONTH([{nameof(ChildProfile.DateOfBirth)}]) > MONTH(GETDATE()) " +
                $"OR MONTH([{nameof(ChildProfile.DateOfBirth)}]) = MONTH(GETDATE()) " +
                $"AND DAY([{nameof(ChildProfile.DateOfBirth)}]) > DAY(GETDATE()) " +
                $"THEN 1 ELSE 0 END");

        base.OnModelCreating(modelBuilder);
    }

    public JordnaerDbContext(DbContextOptions<JordnaerDbContext> options) : base(options)
    { }
}
