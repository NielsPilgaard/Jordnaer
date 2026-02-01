using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Database;

public class JordnaerDbContext : IdentityDbContext<ApplicationUser>
{
	public DbSet<UserProfile> UserProfiles { get; set; } = default!;
	public DbSet<ChildProfile> ChildProfiles { get; set; } = default!;
	public DbSet<Category> Categories { get; set; } = default!;
	public DbSet<UserProfileCategory> UserProfileCategories { get; set; } = default!;
	public DbSet<UserContact> UserContacts { get; set; } = default!;
	public DbSet<Chat> Chats { get; set; } = default!;
	public DbSet<ChatMessage> ChatMessages { get; set; } = default!;
	public DbSet<UnreadMessage> UnreadMessages { get; set; } = default;
	public DbSet<UserChat> UserChats { get; set; } = default!;

	public DbSet<Group> Groups { get; set; } = default!;
	public DbSet<GroupMembership> GroupMemberships { get; set; } = default!;
	public DbSet<GroupCategory> GroupCategories { get; set; } = default!;

	public DbSet<Post> Posts { get; set; } = default!;
	public DbSet<GroupPost> GroupPosts { get; set; } = default!;

	public DbSet<Partner> Partners { get; set; } = default!;
	public DbSet<PartnerAnalytics> PartnerAnalytics { get; set; } = default!;

	public DbSet<PendingGroupInvite> PendingGroupInvites { get; set; } = default!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Post>()
					.HasOne(e => e.UserProfile)
					.WithMany();

		modelBuilder.Entity<Post>()
					.HasMany(e => e.Categories)
					.WithMany()
					.UsingEntity<PostCategory>();

		modelBuilder.Entity<GroupPost>()
					.HasOne(e => e.UserProfile)
					.WithMany();

		modelBuilder.Entity<GroupPost>()
					.HasOne(e => e.Group)
					.WithMany();

		modelBuilder.Entity<Group>()
			.HasMany(e => e.Members)
			.WithMany(e => e.Groups)
			.UsingEntity<GroupMembership>(
				builder =>
				{
					builder.Property(e => e.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
					builder.Property(e => e.EmailOnNewPost).HasDefaultValue(true);
				});

		modelBuilder.Entity<Group>()
			.HasMany(e => e.Categories)
			.WithMany()
			.UsingEntity<GroupCategory>();

		modelBuilder.Entity<UserProfile>()
			.HasMany(userProfile => userProfile.Groups)
			.WithMany(group => group.Members)
			.UsingEntity<GroupMembership>(
				builder =>
				{
					builder.Property(e => e.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
					builder.Property(e => e.EmailOnNewPost).HasDefaultValue(true);
				});

		modelBuilder.Entity<UserProfile>()
			.HasMany(userProfile => userProfile.Categories)
			.WithMany()
			.UsingEntity<UserProfileCategory>();

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

		modelBuilder.Entity<Partner>()
			.HasMany(partner => partner.Analytics)
			.WithOne(analytics => analytics.Partner)
			.HasForeignKey(analytics => analytics.PartnerId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Partner>()
			.HasOne<ApplicationUser>()
			.WithOne()
			.HasForeignKey<Partner>(partner => partner.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Partner>()
			.Property(partner => partner.UserId)
			.HasMaxLength(450);

		modelBuilder.Entity<Partner>()
			.Property(partner => partner.CreatedUtc)
			.HasDefaultValueSql("GETUTCDATE()");

		modelBuilder.Entity<PendingGroupInvite>()
			.HasOne(invite => invite.Group)
			.WithMany()
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<PendingGroupInvite>()
			.HasOne(invite => invite.InvitedByUser)
			.WithMany()
			.OnDelete(DeleteBehavior.SetNull);

		modelBuilder.Entity<PendingGroupInvite>()
			.Property(invite => invite.CreatedUtc)
			.HasDefaultValueSql("GETUTCDATE()");

		base.OnModelCreating(modelBuilder);
	}

	public JordnaerDbContext(DbContextOptions<JordnaerDbContext> options) : base(options)
	{ }
}
