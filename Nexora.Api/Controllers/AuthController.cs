using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Api.Data;
using Nexora.Api.Models;
using Nexora.Api.Services;
using Nexora.Api.Utils;
using Nexora.Api.DTOs;


namespace Nexora.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly NexoraDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            NexoraDbContext db,
            IConfiguration config,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) && string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest("Provide email or phone.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Password is required.");

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var existing = await _userManager.FindByEmailAsync(dto.Email);
                if (existing != null) return BadRequest("User already exists with this email.");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email ?? dto.PhoneNumber,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));

            // create profile
            _db.Profiles.Add(new Profile
            {
                UserId = user.Id,
                DisplayName = dto.DisplayName ?? user.UserName,
                CreatedAt = DateTime.UtcNow
            });

            // create OTP
            var code = new Random().Next(100000, 999999).ToString();
            var otp = new OtpCode
            {
                UserId = user.Id,
                Channel = !string.IsNullOrEmpty(dto.Email) ? "Email" : "Phone",
                Address = dto.Email ?? dto.PhoneNumber ?? "",
                Code = code,
                Purpose = "Register",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };
            _db.OtpCodes.Add(otp);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(dto.Email))
            {
                await _emailService.SendOtpAsync(dto.Email, code, "Nexora - Verify your email");
            }

            return Ok(new { message = "User created. OTP sent to provided channel." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            var otp = await _db.OtpCodes.FirstOrDefaultAsync(o =>
                o.Address == dto.Address && o.Code == dto.Code && o.Purpose == dto.Purpose &&
                !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

            if (otp == null) return BadRequest("Invalid or expired code.");

            otp.IsUsed = true;
            _db.OtpCodes.Update(otp);

            if (!string.IsNullOrEmpty(otp.UserId))
            {
                var user = await _userManager.FindByIdAsync(otp.UserId);
                if (user != null)
                {
                    if (otp.Channel == "Email") user.EmailConfirmed = true;
                    if (otp.Channel == "Phone") user.PhoneNumberConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Verified" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Email and Password are required.");

            ApplicationUser? user = await _userManager.FindByEmailAsync(dto.Email)
                                   ?? await _userManager.FindByNameAsync(dto.Email);

            if (user == null) return Unauthorized("Invalid credentials.");

            var check = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!check) return Unauthorized("Invalid credentials.");

            if (!string.IsNullOrEmpty(user.Email) && !user.EmailConfirmed)
                return Unauthorized("Email not verified. Please check your email for OTP.");

            var token = JwtHelper.GenerateToken(user, _config);
            return Ok(new { token, userId = user.Id, username = user.UserName });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp(ResendOtpDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Address);
            if (user == null) return BadRequest("User not found.");

            var code = new Random().Next(100000, 999999).ToString();
            var otp = new OtpCode
            {
                UserId = user.Id,
                Channel = dto.Channel ?? "Email",
                Address = dto.Address,
                Code = code,
                Purpose = dto.Purpose ?? "Register",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _db.OtpCodes.Add(otp);
            await _db.SaveChangesAsync();

            if (otp.Channel == "Email")
                await _emailService.SendOtpAsync(dto.Address, code, "Nexora - Verify your email");

            return Ok(new { message = "OTP resent." });
        }
    }

    // DTOs (kept inside controller for now)
    public record RegisterDto(string? Email, string? PhoneNumber, string Password, string? DisplayName);
    public record VerifyOtpDto(string Address, string Code, string Purpose);
    public record LoginDto(string Email, string Password);
    public record ResendOtpDto(string Address, string? Channel, string? Purpose);
}
