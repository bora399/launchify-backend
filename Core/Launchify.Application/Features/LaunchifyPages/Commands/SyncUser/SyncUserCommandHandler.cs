using Launchify.Application.Interfaces;
using MediatR;

namespace Launchify.Application.Features.Users.Commands.SyncUser
{
    public class SyncUserCommandHandler : IRequestHandler<SyncUserCommand, SyncUserResponse>
    {
        private readonly IUserRepository _userRepository;

        public SyncUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<SyncUserResponse> Handle(SyncUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userRepository.SyncUserAsync(request.Uid, request.Email);

                return new SyncUserResponse
                {
                    IsSuccess = true,
                    Message = "Kullanıcı başarıyla senkronize edildi.",
                    RemainingCredits = user.RemainingCredits
                };
            }
            catch (Exception ex)
            {
                return new SyncUserResponse
                {
                    IsSuccess = false,
                    Message = $"Hata oluştu: {ex.Message}"
                };
            }
        }
    }
}