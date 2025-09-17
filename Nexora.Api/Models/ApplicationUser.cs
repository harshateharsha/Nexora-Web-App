using Microsoft.AspNetCore.Identity;

namespace Nexora.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
    }
}
