namespace Nexora.Api.Models
{
    public class Comment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // which post
        public Guid PostId { get; set; }
        public Post? Post { get; set; }

        // who commented
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
