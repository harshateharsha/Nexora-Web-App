using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Api.Data;
using Nexora.Api.Models;

namespace Nexora.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        public ProfileController(NexoraDbContext db) => _db = db;

        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(string userId)
        {
            var profile = await _db.Profiles.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProfileUpdateDto dto)
        {
            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.UserId == me);
            if (profile == null) return NotFound();

            profile.DisplayName = dto.DisplayName ?? profile.DisplayName;
            profile.Bio = dto.Bio ?? profile.Bio;
            profile.AvatarUrl = dto.AvatarUrl ?? profile.AvatarUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            _db.Profiles.Update(profile);
            await _db.SaveChangesAsync();
            return Ok(profile);
        }
    }

    public class ProfileUpdateDto { public string? DisplayName { get; set; } public string? Bio { get; set; } public string? AvatarUrl { get; set; } }
}
