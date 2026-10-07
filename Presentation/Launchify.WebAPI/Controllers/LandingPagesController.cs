using Launchify.Application.DTOs;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Features.LaunchifyPages.Commands.DeleteLandingPage;
using Launchify.Application.Interfaces;
using Launchify.Infrastructure.Services;
using LaunchifyBackend.Hubs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingPagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILandingPageRepository _repository;

        private readonly GeminiAiService _aiService;
        private readonly IHubContext<GenerationHub> _hubContext;

        public LandingPagesController(IMediator mediator, ILandingPageRepository repository, GeminiAiService aiService, IHubContext<GenerationHub> hubContext)
        {
            _mediator = mediator;
            _repository = repository;
            _aiService = aiService;
            _hubContext = hubContext;
        }

        [HttpPost("generate")]
        public IActionResult Generate([FromBody] GeneratePageRequest request)
        {
            _ = Task.Run(async () =>
            {
                await _aiService.GenerateContentAsync(
                    request.ProductName,
                    request.ThemeType,
                    request.ProductDescription,
                    async (logMessage) =>
                    {
                        await _hubContext.Clients.Client(request.ConnectionId).SendAsync("ReceiveLog", logMessage);
                    });
            });

            return Ok(new { message = "Süreç başlatıldı, loglar SignalR üzerinden akıyor..." });
        }

        [HttpPost("create")]
        [EnableRateLimiting("AiCreationLimit")]
        public async Task<IActionResult> CreateLandingPage([FromBody] CreateLandingPageCommand command)
        {
            var response = await _mediator.Send(command);
            if (response.IsSuccess)
            {
                return Ok(new { id = response.GeneratedPageId, slug = response.Slug, message = response.Message });
            }
            return BadRequest(new { message = response.Message });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserProjects(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { message = "UserId parametresi gereklidir." });
            }

            var projects = await _repository.GetByUserIdAsync(userId);
            return Ok(projects);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLandingPage(string id, [FromQuery] string userId)
        {
            var command = new DeleteLandingPageCommand { PageId = id, UserId = userId };
            var response = await _mediator.Send(command);

            if (response.IsSuccess) return Ok(response);

            return BadRequest(new { message = response.Message });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetLandingPageBySlug(string slug)
        {
            var pageData = await _repository.GetBySlugAsync(slug);
            if (pageData == null)
            {
                return NotFound(new { message = "Bu isme ait bir platform bulunamadı." });
            }

            return Ok(new
            {
                id = pageData.Id.ToString(),
                slug = pageData.Slug,
                productName = pageData.ProductName,
                aiGeneratedHeroTitle = pageData.AiConfig?.AiGeneratedHeroTitle,
                aiGeneratedMarketingCopy = pageData.AiConfig?.AiGeneratedMarketingCopy,
                accentColor = pageData.AiConfig?.AccentColor,
                demoLink = pageData.DemoLink,
                templateType = pageData.TemplateType
            });
        }
    }
}