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
        private readonly ILandingPageRepository _repository; // GET metodu için direkt repo kullanabiliriz veya ona da bir Query yazabilirsin

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
                return Ok(new { id = response.GeneratedPageId, message = response.Message });
            }

            return BadRequest(new { message = response.Message });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLandingPage(string id)
        {
            var pageData = await _repository.GetByIdAsync(id);

            if (pageData == null)
            {
                return NotFound(new { message = "Bu ID'ye ait bir proje bulunamadı." });
            }

            // Frontend için düzleştirilmiş obje dönüyoruz
            return Ok(new
            {
                id = pageData.Id.ToString(),
                productName = pageData.ProductName,
                aiGeneratedHeroTitle = pageData.AiConfig?.AiGeneratedHeroTitle,
                aiGeneratedMarketingCopy = pageData.AiConfig?.AiGeneratedMarketingCopy,
                accentColor = pageData.AiConfig?.AccentColor,
                demoLink = pageData.DemoLink
            });
        }
    }
}