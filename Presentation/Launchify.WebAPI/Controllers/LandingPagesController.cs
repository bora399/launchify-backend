using Launchify.Application.DTOs;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Features.LaunchifyPages.Commands.DeleteLandingPage;
using Launchify.Application.Interfaces;
using LaunchifyBackend.Hubs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingPagesController(
        IMediator mediator,
        ILandingPageRepository repository,
        IHubContext<GenerationHub> hubContext) : ControllerBase
    {
        [HttpPost("create")]
        [EnableRateLimiting("AiCreationLimit")]
        public async Task<IActionResult> CreateLandingPage([FromBody] CreateLandingPageCommand command)
        {
            command.LogCallback = async (logMessage) =>
            {
                if (!string.IsNullOrEmpty(command.ConnectionId))
                {
                    await hubContext.Clients.Client(command.ConnectionId).SendAsync("ReceiveLog", logMessage);
                }
            };

            var response = await mediator.Send(command);

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

            var projects = await repository.GetByUserIdAsync(userId);
            return Ok(projects);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLandingPage(string id, [FromQuery] string userId)
        {
            var command = new DeleteLandingPageCommand { PageId = id, UserId = userId };
            var response = await mediator.Send(command);

            if (response.IsSuccess) return Ok(response);

            return BadRequest(new { message = response.Message });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetLandingPageBySlug(string slug)
        {
            var pageData = await repository.GetBySlugAsync(slug);
            if (pageData == null)
            {
                return NotFound(new { message = "Bu isme ait bir platform bulunamadı." });
            }

            return Ok(new
            {
                id = pageData.Id.ToString(),
                slug = pageData.Slug,
                productName = pageData.ProductName,
                templateType = pageData.TemplateType,
                demoLink = pageData.DemoLink,
                contactEmail = pageData.ContactEmail,
                aiConfig = new
                {
                    aiGeneratedHeroTitle = pageData.AiConfig?.AiGeneratedHeroTitle,
                    aiGeneratedMarketingCopy = pageData.AiConfig?.AiGeneratedMarketingCopy,
                    accentColor = pageData.AiConfig?.AccentColor,
                    callToActionText = pageData.AiConfig?.CallToActionText,
                    features = pageData.AiConfig?.Features
                }
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLandingPage([FromRoute] string id, [FromBody] UpdateLandingPageRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id parametresi gereklidir." });

            if (request == null)
                return BadRequest(new { message = "Güncelleme verisi boş olamaz." });

            try
            {
                var existingPage = await repository.GetByIdAsync(id);

                if (existingPage == null)
                {
                    existingPage = await repository.GetBySlugAsync(id);
                }

                if (existingPage == null)
                {
                    return NotFound(new { message = "Düzenlenecek proje bulunamadı." });
                }

                if (!string.IsNullOrWhiteSpace(request.ProductName))
                    existingPage.ProductName = request.ProductName;

                if (!string.IsNullOrWhiteSpace(request.TemplateType))
                    existingPage.TemplateType = request.TemplateType;

                if (existingPage.AiConfig == null)
                {
                    existingPage.AiConfig = new Launchify.Domain.Entities.AiPageConfig();
                }

                if (!string.IsNullOrWhiteSpace(request.HeroTitle))
                    existingPage.AiConfig.AiGeneratedHeroTitle = request.HeroTitle;

                if (!string.IsNullOrWhiteSpace(request.MarketingCopy))
                    existingPage.AiConfig.AiGeneratedMarketingCopy = request.MarketingCopy;

                if (!string.IsNullOrWhiteSpace(request.CallToActionText))
                    existingPage.AiConfig.CallToActionText = request.CallToActionText;

                if (!string.IsNullOrWhiteSpace(request.AccentColor))
                    existingPage.AiConfig.AccentColor = request.AccentColor;

                await repository.UpdateAsync(existingPage);

                return Ok(new { message = "Proje başarıyla güncellendi.", page = existingPage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GÜNCELLEME HATASI]: {ex.Message} \n {ex.StackTrace}");
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost("ai-assist")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] 
        public async Task<IActionResult> AiAssist(
                    [FromBody] Launchify.Application.DTOs.AiAssistRequest request,
                    [FromServices] IAiGeneratorService aiService)
        {
            try
            {
                if (request == null)
                {
                    Console.WriteLine("[AI-Assist HATA] İstek gövdesi (body) boş geldi.");
                    return BadRequest(new { message = "İstek gövdesi boş olamaz." });
                }

                Console.WriteLine($"[AI-Assist İSTEK] Ürün: '{request.ProductName}', Mod: '{request.Mode}', Şablon: '{request.CurrentTemplate}'");

                var result = await aiService.AssistContentAsync(request);

                Console.WriteLine("[AI-Assist BAŞARILI] Yanıt üretildi ve gönderiliyor.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n--- [AI-Assist KRİTİK HATA] ---\nMesaj: {ex.Message}\nDetay: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}\n-----------------------------\n");

                return StatusCode(500, new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }
    }
}