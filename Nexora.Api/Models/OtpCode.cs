namespace Nexora.Api.Models
{
    public class OtpCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? UserId { get; set; }            // optional
        public string Channel { get; set; } = "Email"; // Email | Phone
        public string Address { get; set; } = null!;   // email or phone
        public string Code { get; set; } = null!;
        public string Purpose { get; set; } = "Register";
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
