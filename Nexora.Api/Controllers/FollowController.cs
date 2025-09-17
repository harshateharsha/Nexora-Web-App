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
    public class FollowController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        public FollowController(NexoraDbContext db) => _db = db;

        [Authorize]
        [HttpPost("{targetUserId}")]
        public async Task<IActionResult> SendRequest(string targetUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            if (userId == targetUserId) return BadRequest("Cannot follow yourself.");

            var existing = await _db.Follows.FirstOrDefaultAsync(f => f.FollowerId == userId && f.FolloweeId == targetUserId);
            if (existing != null) return BadRequest("Request already exists.");

            var f = new Follow { FollowerId = userId, FolloweeId = targetUserId, Status = "Pending", CreatedAt = DateTime.UtcNow };
            _db.Follows.Add(f);
            await _db.SaveChangesAsync();

            // create notification for the followee
            _db.Notifications.Add(new Notification
            {
                UserId = targetUserId,
                Type = "FollowRequest",
                PayloadJson = $"{{\"from\":\"{userId}\",\"followId\":\"{f.Id}\"}}",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return Ok(f);
        }

        [Authorize]
        [HttpPost("{id:guid}/accept")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var current = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var req = await _db.Follows.FindAsync(id);
            if (req == null) return NotFound();
            if (req.FolloweeId != current) return Forbid();

            req.Status = "Accepted";
            _db.Follows.Update(req);

            // notify follower that accepted
            _db.Notifications.Add(new Notification
            {
                UserId = req.FollowerId,
                Type = "FollowAccepted",
                PayloadJson = $"{{\"from\":\"{req.FolloweeId}\",\"followId\":\"{req.Id}\"}}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpPost("{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id)
        {
            var current = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var req = await _db.Follows.FindAsync(id);
            if (req == null) return NotFound();
            if (req.FolloweeId != current) return Forbid();

            req.Status = "Rejected";
            _db.Follows.Update(req);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [Authorize]
        [HttpGet("requests")]
        public async Task<IActionResult> MyRequests()
        {
            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = await _db.Follows.Where(f => f.FolloweeId == me && f.Status == "Pending").ToListAsync();
            return Ok(list);
        }
    }
}
