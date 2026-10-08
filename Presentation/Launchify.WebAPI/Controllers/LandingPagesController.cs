using Launchify.Application.DTOs;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Features.LaunchifyPages.Commands.DeleteLandingPage;
using Launchify.Application.Interfaces;
using Launchify.Infrastructure.Repositories;
using LaunchifyBackend.Hubs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingPagesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILandingPageRepository _repository;
        private readonly IHubContext<GenerationHub> _hubContext;

        public LandingPagesController(
            IMediator mediator,
            ILandingPageRepository repository,
            IHubContext<GenerationHub> hubContext)
        {
            _mediator = mediator;
            _repository = repository;
            _hubContext = hubContext;
        }

        [HttpPost("create")]
        [EnableRateLimiting("AiCreationLimit")]
        public async Task<IActionResult> CreateLandingPage([FromBody] CreateLandingPageCommand command)
        {
            command.LogCallback = async (logMessage) =>
            {
                if (!string.IsNullOrEmpty(command.ConnectionId))
                {
                    await _hubContext.Clients.Client(command.ConnectionId).SendAsync("ReceiveLog", logMessage);
                }
            };

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLandingPage(string id, [FromBody] UpdateLandingPageRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id parametresi gereklidir.");

            var existingPage = await _repository.GetByIdAsync(id);
            if (existingPage == null)
                return NotFound("Düzenlenecek proje bulunamadı.");

            if (!string.IsNullOrWhiteSpace(request.ProductName))
                existingPage.ProductName = request.ProductName;

            if (!string.IsNullOrWhiteSpace(request.TemplateType))
                existingPage.TemplateType = request.TemplateType;

            if (existingPage.AiConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(request.HeroTitle))
                    existingPage.AiConfig.AiGeneratedHeroTitle = request.HeroTitle;

                if (!string.IsNullOrWhiteSpace(request.MarketingCopy))
                    existingPage.AiConfig.AiGeneratedMarketingCopy = request.MarketingCopy;

                if (!string.IsNullOrWhiteSpace(request.CallToActionText))
                    existingPage.AiConfig.CallToActionText = request.CallToActionText;

                if (!string.IsNullOrWhiteSpace(request.AccentColor))
                    existingPage.AiConfig.AccentColor = request.AccentColor;
            }

            await _landingPageRepository.UpdateAsync(existingPage);

            return Ok(new { message = "Proje başarıyla güncellendi.", page = existingPage });
        }
    }
}