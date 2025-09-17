namespace Nexora.Api.Models
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string SenderId { get; set; } = null!;
        public ApplicationUser? Sender { get; set; }

        public string ReceiverId { get; set; } = null!;
        public ApplicationUser? Receiver { get; set; }

        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SeenAt { get; set; }
    }
}
