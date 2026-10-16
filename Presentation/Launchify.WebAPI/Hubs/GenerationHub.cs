using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LaunchifyBackend.Hubs
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class GenerationHub : Hub
    {
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}