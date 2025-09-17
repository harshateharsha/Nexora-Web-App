using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Nexora.Api.Models
{
    public class Post
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Author
        public string UserId { get; set; } = null!;
        public ApplicationUser? User { get; set; }

        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; } // e.g. Image, Video, None

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation collections
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}
