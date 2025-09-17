namespace Nexora.Api.Models
{
    public class Follow
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FollowerId { get; set; } = null!; // who sent request
        public ApplicationUser? Follower { get; set; }

        public string FolloweeId { get; set; } = null!; // who is being followed
        public ApplicationUser? Followee { get; set; }

        public string Status { get; set; } = "Pending"; // Pending | Accepted | Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
