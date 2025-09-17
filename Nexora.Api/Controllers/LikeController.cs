using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nexora.Api.Data;
using Nexora.Api.Hubs;
using Nexora.Api.Models;

namespace Nexora.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LikeController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        private readonly IHubContext<ChatHub> _hub;

        public LikeController(NexoraDbContext db, IHubContext<ChatHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Like([FromBody] LikeCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var post = await _db.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == dto.PostId);
            if (post == null) return BadRequest("Post not found.");

            var exists = await _db.Likes.AnyAsync(l => l.PostId == dto.PostId && l.UserId == userId);
            if (exists) return BadRequest("Already liked.");

            var like = new Like { PostId = dto.PostId, UserId = userId, CreatedAt = DateTime.UtcNow };
            _db.Likes.Add(like);

            // create notification only if liking someone else's post
            Notification? notif = null;
            if (post.UserId != userId)
            {
                notif = new Notification
                {
                    UserId = post.UserId,
                    Type = "Like",
                    PayloadJson = $"{{\"postId\":\"{post.Id}\",\"from\":\"{userId}\",\"likeId\":\"{like.Id}\"}}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _db.Notifications.Add(notif);
            }

            await _db.SaveChangesAsync();

            // push via SignalR to post owner (if they are not the liker)
            if (post.UserId != userId)
            {
                await _hub.Clients.Group($"u:{post.UserId}")
                    .SendAsync("notification", new
                    {
                        type = "Like",
                        from = userId,
                        postId = post.Id,
                        likeId = like.Id,
                        createdAt = like.CreatedAt
                    });
            }

            return Ok(like);
        }

        [Authorize]
        [HttpDelete("post/{postId:guid}")]
        public async Task<IActionResult> Unlike(Guid postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var like = await _db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
            if (like == null) return NotFound();

            // optionally delete corresponding notification(s) for this like (if desired)
            var relatedNotifs = await _db.Notifications
                .Where(n => n.Type == "Like" && n.PayloadJson!.Contains(like.Id.ToString()))
                .ToListAsync();
            if (relatedNotifs.Any())
            {
                _db.Notifications.RemoveRange(relatedNotifs);
            }

            _db.Likes.Remove(like);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("count/{postId:guid}")]
        public async Task<IActionResult> Count(Guid postId)
        {
            var c = await _db.Likes.CountAsync(l => l.PostId == postId);
            return Ok(new { count = c });
        }
    }

    public class LikeCreateDto { public Guid PostId { get; set; } }
}
