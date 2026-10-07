using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LaunchifyBackend.Hubs
{
    public class GenerationHub : Hub
    {
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}