namespace Nexora.Api.Models
{
    public class Profile
    {
        // Primary key is the UserId (one-to-one with ApplicationUser)
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
