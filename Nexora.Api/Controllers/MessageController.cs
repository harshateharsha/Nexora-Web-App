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
    public class MessageController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        private readonly IHubContext<ChatHub> _hub;
        public MessageController(NexoraDbContext db, IHubContext<ChatHub> hub) { _db = db; _hub = hub; }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] MessageCreateDto dto)
        {
            var from = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (from == null) return Unauthorized();

            var msg = new Message { SenderId = from, ReceiverId = dto.ReceiverId, Text = dto.Text, CreatedAt = DateTime.UtcNow };
            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            await _hub.Clients.Group($"u:{dto.ReceiverId}").SendAsync("dm", new { from = msg.SenderId, text = msg.Text, at = msg.CreatedAt });
            return Ok(msg);
        }

        [Authorize]
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> History(string otherUserId)
        {
            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (me == null) return Unauthorized();

            var messages = await _db.Messages
                .Where(m => (m.SenderId == me && m.ReceiverId == otherUserId) || (m.SenderId == otherUserId && m.ReceiverId == me))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }
    }

    public class MessageCreateDto { public string ReceiverId { get; set; } = null!; public string? Text { get; set; } }
}
