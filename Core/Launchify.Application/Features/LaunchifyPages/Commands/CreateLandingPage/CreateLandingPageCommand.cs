using MediatR;
using System.Collections.Generic;

namespace Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage
{
    public class CreateLandingPageCommand : IRequest<CreateLandingResponse>
    {
        public string ProductName { get; set; }
        public string ThemeType { get; set; }
        public string ContactEmail { get; set; }
        public string AdminPin { get; set; }
        public string DemoLink { get; set; }
        public string ProductDescription { get; set; }
    }

    public class CreateLandingResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string GeneratedPageId { get; set; }

        public string Slug { get; set; }
    }
}