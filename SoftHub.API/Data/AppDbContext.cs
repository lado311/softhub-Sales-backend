using Microsoft.EntityFrameworkCore;
using SoftHub.API.Models;

namespace SoftHub.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<Lead>(e =>
        {
            e.Property(l => l.PotentialValue).HasColumnType("numeric(18,2)");
            e.HasOne(l => l.AssignedTo)
             .WithMany(u => u.AssignedLeads)
             .HasForeignKey(l => l.AssignedToId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Note>(e =>
        {
            e.HasOne(n => n.Lead)
             .WithMany(l => l.Notes)
             .HasForeignKey(n => n.LeadId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Author)
             .WithMany(u => u.Notes)
             .HasForeignKey(n => n.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ActivityLog>(e =>
        {
            e.HasOne(a => a.Lead)
             .WithMany(l => l.Activities)
             .HasForeignKey(a => a.LeadId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed admin user  (password: Admin123!)
        b.Entity<User>().HasData(new User
        {
            Id = 1,
            FullName = "Admin User",
            Email = "admin@softhub.io",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
