namespace Nexora.Api.DTOs
{
    public record RegisterDto
    {
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
    }

    public record LoginDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public record VerifyOtpDto
    {
        public string Address { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Purpose { get; init; } = string.Empty;
    }

    public record ResendOtpDto
    {
        public string Address { get; init; } = string.Empty;
        public string Channel { get; init; } = "Email";
        public string Purpose { get; init; } = "Register";
    }
}
