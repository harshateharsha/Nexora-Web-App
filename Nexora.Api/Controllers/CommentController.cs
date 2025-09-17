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
    public class CommentController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        private readonly IHubContext<ChatHub> _hub;

        public CommentController(NexoraDbContext db, IHubContext<ChatHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CommentCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var post = await _db.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == dto.PostId);
            if (post == null) return BadRequest("Post not found.");

            var c = new Comment { PostId = dto.PostId, UserId = userId, Text = dto.Text, CreatedAt = DateTime.UtcNow };
            _db.Comments.Add(c);

            Notification? notif = null;
            if (post.UserId != userId)
            {
                notif = new Notification
                {
                    UserId = post.UserId,
                    Type = "Comment",
                    PayloadJson = $"{{\"postId\":\"{post.Id}\",\"from\":\"{userId}\",\"commentId\":\"{c.Id}\"}}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _db.Notifications.Add(notif);
            }

            await _db.SaveChangesAsync();

            // push via SignalR to post owner
            if (post.UserId != userId)
            {
                await _hub.Clients.Group($"u:{post.UserId}")
                    .SendAsync("notification", new
                    {
                        type = "Comment",
                        from = userId,
                        postId = post.Id,
                        commentId = c.Id,
                        text = c.Text,
                        createdAt = c.CreatedAt
                    });
            }

            return Ok(c);
        }

        [HttpGet("post/{postId:guid}")]
        public async Task<IActionResult> GetForPost(Guid postId)
        {
            var list = await _db.Comments
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
            return Ok(list);
        }
    }

    public class CommentCreateDto { public Guid PostId { get; set; } public string? Text { get; set; } }
}
