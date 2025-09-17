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
    public class NotificationController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        public NotificationController(NexoraDbContext db) => _db = db;

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = await _db.Notifications.Where(n => n.UserId == me).OrderByDescending(n => n.CreatedAt).ToListAsync();
            return Ok(list);
        }

        [Authorize]
        [HttpPost("{id:guid}/mark-read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var me = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var n = await _db.Notifications.FindAsync(id);
            if (n == null) return NotFound();
            if (n.UserId != me) return Forbid();
            n.IsRead = true;
            _db.Notifications.Update(n);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
