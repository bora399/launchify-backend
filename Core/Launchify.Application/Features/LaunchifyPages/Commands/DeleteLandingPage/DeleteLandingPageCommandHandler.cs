using Launchify.Application.Interfaces;
using MediatR;

namespace Launchify.Application.Features.LaunchifyPages.Commands.DeleteLandingPage
{
    public class DeleteLandingPageCommandHandler : IRequestHandler<DeleteLandingPageCommand, DeleteLandingPageResponse>
    {
        private readonly ILandingPageRepository _repository;
        private readonly IUserRepository _userRepository;

        public DeleteLandingPageCommandHandler(ILandingPageRepository repository, IUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
        }

        public async Task<DeleteLandingPageResponse> Handle(DeleteLandingPageCommand request, CancellationToken cancellationToken)
        {
            var page = await _repository.GetByIdAsync(request.PageId);

            if (page == null || page.UserId != request.UserId)
            {
                return new DeleteLandingPageResponse { IsSuccess = false, Message = "Proje bulunamadı veya yetkiniz yok." };
            }

            await _repository.DeleteAsync(request.PageId);

            await _userRepository.RefundCreditAsync(request.UserId);

            return new DeleteLandingPageResponse { IsSuccess = true, Message = "Proje başarıyla kaldırıldı ve 1 kredi iade edildi." };
        }
    }
}