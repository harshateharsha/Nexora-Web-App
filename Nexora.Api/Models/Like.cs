namespace Nexora.Api.Models
{
    public class Like
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PostId { get; set; }
        public Post? Post { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
