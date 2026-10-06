using MediatR;
namespace Launchify.Application.Features.LaunchifyPages.Commands.DeleteLandingPage
{
    public class DeleteLandingPageCommand : IRequest<DeleteLandingPageResponse>
    {
        public string PageId { get; set; }
        public string UserId { get; set; }
    }
}