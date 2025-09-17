using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nexora.Api.Data;
using Nexora.Api.Models;

namespace Nexora.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly NexoraDbContext _db;
        public ChatHub(NexoraDbContext db) { _db = db; }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"u:{userId}");
            }
            await base.OnConnectedAsync();
        }

        // server-side message send (clients can call this)
        public async Task SendToUser(string receiverUserId, string text)
        {
            var from = Context.UserIdentifier;
            if (string.IsNullOrEmpty(from)) return;

            var msg = new Message
            {
                SenderId = from,
                ReceiverId = receiverUserId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            // push to receiver
            await Clients.Group($"u:{receiverUserId}").SendAsync("dm", new { from = msg.SenderId, text = msg.Text, at = msg.CreatedAt });
            // echo to sender
            await Clients.Caller.SendAsync("dmSent", new { to = receiverUserId, text = msg.Text, at = msg.CreatedAt });
        }
    }
}
