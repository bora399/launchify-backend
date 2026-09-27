using MediatR;
using Microsoft.AspNetCore.Mvc;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Interfaces;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingPagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILandingPageRepository _repository;

        public LandingPagesController(IMediator mediator, ILandingPageRepository repository)
        {
            _mediator = mediator;
            _repository = repository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateLandingPage([FromBody] CreateLandingPageCommand command)
        {
            var response = await _mediator.Send(command);

            if (response.IsSuccess)
            {
                // Frontend form sayfasında result.slug bekliyor, onu burada dönüyoruz
                return Ok(new { id = response.GeneratedPageId, slug = response.Slug, message = response.Message });
            }

            return BadRequest(new { message = response.Message });
        }

        // DİKKAT: Artık Guid {id} değil, string {slug} dinliyor (Örn: /api/LandingPages/videocu)
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetLandingPageBySlug(string slug)
        {
            // Repository katmanında Firestore'da WhereEqualTo("Slug", slug) atacak metot
            var pageData = await _repository.GetBySlugAsync(slug);

            if (pageData == null)
            {
                return NotFound(new { message = "Bu isme ait bir platform bulunamadı." });
            }

            // Frontend için birebir Mapping işlemi (JS camelCase formatında)
            return Ok(new
            {
                id = pageData.Id.ToString(),
                slug = pageData.Slug,
                productName = pageData.ProductName,
                aiGeneratedHeroTitle = pageData.AiConfig?.AiGeneratedHeroTitle,
                aiGeneratedMarketingCopy = pageData.AiConfig?.AiGeneratedMarketingCopy,
                accentColor = pageData.AiConfig?.AccentColor,
                demoLink = pageData.DemoLink
            });
        }
    }
}