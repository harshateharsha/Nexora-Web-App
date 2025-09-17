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
    public class PostController : ControllerBase
    {
        private readonly NexoraDbContext _db;
        public PostController(NexoraDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _db.Posts.Include(p => p.User).OrderByDescending(p => p.CreatedAt);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var post = await _db.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();
            return Ok(post);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PostCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var post = new Post
            {
                UserId = userId,
                Content = dto.Content,
                MediaUrl = dto.MediaUrl,
                MediaType = dto.MediaType,
                CreatedAt = DateTime.UtcNow
            };

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = post.Id }, post);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PostUpdateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();
            if (post.UserId != userId) return Forbid();

            post.Content = dto.Content ?? post.Content;
            post.MediaUrl = dto.MediaUrl ?? post.MediaUrl;
            post.MediaType = dto.MediaType ?? post.MediaType;
            post.UpdatedAt = DateTime.UtcNow;

            _db.Posts.Update(post);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();
            if (post.UserId != userId) return Forbid();

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    public class PostCreateDto { public string? Content { get; set; } public string? MediaUrl { get; set; } public string? MediaType { get; set; } }
    public class PostUpdateDto { public string? Content { get; set; } public string? MediaUrl { get; set; } public string? MediaType { get; set; } }
}
