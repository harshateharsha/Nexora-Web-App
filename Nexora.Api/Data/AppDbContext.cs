using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nexora.Api.Models;

namespace Nexora.Api.Data
{
    public class NexoraDbContext : IdentityDbContext<ApplicationUser>
    {
        public NexoraDbContext(DbContextOptions<NexoraDbContext> options) : base(options) { }

        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Like> Likes { get; set; } = null!;
        public DbSet<Follow> Follows { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<OtpCode> OtpCodes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Profile: PK is UserId (one-to-one)
            builder.Entity<Profile>()
                .HasKey(p => p.UserId);

            builder.Entity<Profile>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Post -> User (author)
            builder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment -> Post (restrict to avoid multiple cascade paths)
            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Comment -> User
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Like -> Post (restrict)
            builder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Like -> User
            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Follow relations (restrict)
            builder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Follow>()
                .HasOne(f => f.Followee)
                .WithMany()
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message sender/receiver (restrict)
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification -> User (who receives it)
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.Entity<Follow>().HasIndex(f => new { f.FolloweeId, f.Status });
            builder.Entity<Message>().HasIndex(m => new { m.ReceiverId, m.CreatedAt });
            builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });
            builder.Entity<Like>().HasIndex(l => l.PostId);
        }
    }
}
