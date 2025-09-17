namespace Nexora.Api.Models
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // who receives this notification
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        public string Type { get; set; } = null!;     // Like, Comment, FollowRequest, FollowAccepted, etc.
        public string? PayloadJson { get; set; }      // extra info (json)
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
