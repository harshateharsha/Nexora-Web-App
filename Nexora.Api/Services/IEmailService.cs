namespace Nexora.Api.Services
{
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string code, string purpose = "Nexora OTP");
    }
}
