using MediatR;
using System.Collections.Generic;

namespace Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage
{
    public class CreateLandingPageCommand : IRequest<CreateLandingResponse>
    {
        public string UserId { get; set; }
        public string ProductName { get; set; }
        public string ThemeType { get; set; }
        public string ContactEmail { get; set; }
        public string AdminPin { get; set; }
        public string DemoLink { get; set; }
        public string ProductDescription { get; set; }
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();
    }

    public class PhotoDto
    {
        public string ImageUrl { get; set; }
        public string Note { get; set; }
        public int OrderIndex { get; set; }
    }

    public class CreateLandingResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string GeneratedPageId { get; set; }

        // YENİ EKLENDİ: Next.js'in isme göre yönlendirme yapabilmesi için
        public string Slug { get; set; }
    }
}