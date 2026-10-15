using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<User> Users => Set<User>();
	public DbSet<EventType> EventTypes => Set<EventType>();
	public DbSet<Event> Events => Set<Event>();
	public DbSet<Category> Categories => Set<Category>();
	public DbSet<Enrolment> Enrolments => Set<Enrolment>();
	public DbSet<Result> Results => Set<Result>();

	protected override void OnModelCreating(ModelBuilder b)
	{
		b.Entity<User>(e =>
		{
			e.Property(x => x.FullName).HasMaxLength(100);
			e.Property(x => x.Email).HasMaxLength(150);
			e.Property(x => x.PasswordHash).HasMaxLength(255);
			e.Property(x => x.Role).HasMaxLength(20);
			e.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
			e.HasIndex(x => x.Email).IsUnique();
			e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
		});

		b.Entity<EventType>(e =>
		{
			e.Property(x => x.Name).HasMaxLength(30);
			e.HasIndex(x => x.Name).IsUnique();
			e.HasData(
				new EventType { EventTypeId = 1, Name = "Run" },
				new EventType { EventTypeId = 2, Name = "Walk" },
				new EventType { EventTypeId = 3, Name = "Cycle" });
		});

		b.Entity<Event>(e =>
		{
			e.Property(x => x.Name).HasMaxLength(150);
			e.Property(x => x.Description).HasMaxLength(1000);
			e.Property(x => x.Location).HasMaxLength(150);
			e.Property(x => x.Distance).HasColumnType("decimal(6,2)");
			e.Property(x => x.BannerImageUrl).HasMaxLength(500);
			e.HasOne(x => x.EventType).WithMany(t => t.Events)
				.HasForeignKey(x => x.EventTypeId).OnDelete(DeleteBehavior.Restrict);
			e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
				.HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
			e.ToTable(t => t.HasCheckConstraint("CK_Events_Distance", "[Distance] > 0"));
		});

		b.Entity<Category>(e =>
		{
			e.Property(x => x.Name).HasMaxLength(50);
			e.HasIndex(x => new { x.EventId, x.Name }).IsUnique();
			e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
				.HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
		});

		b.Entity<Enrolment>(e =>
		{
			e.Property(x => x.EnrolmentStatus).HasMaxLength(20);
			e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();
			e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
				.HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
			e.HasOne(x => x.Event).WithMany(ev => ev.Enrolments)
				.HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
			e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
				.HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
			e.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending','Confirmed')"));
		});

		b.Entity<Result>(e =>
		{
			e.Property(x => x.FinishTime).HasColumnType("time(0)");
			e.HasIndex(x => x.EnrolmentId).IsUnique();
			e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
				.HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
			e.ToTable(t => t.HasCheckConstraint("CK_Results_Position", "[FinishingPosition] > 0"));
		});
	}
}