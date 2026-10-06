using MediatR;

namespace Launchify.Application.Features.Users.Commands.SyncUser
{
    public class SyncUserCommand : IRequest<SyncUserResponse>
    {
        public string Uid { get; set; }
        public string Email { get; set; }
    }
}