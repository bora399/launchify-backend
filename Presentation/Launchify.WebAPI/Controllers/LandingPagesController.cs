using Launchify.Application.DTOs;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Features.LaunchifyPages.Commands.DeleteLandingPage;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using LaunchifyBackend.Hubs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingPagesController(
        IMediator mediator,
        ILandingPageRepository repository,
        IHubContext<GenerationHub> hubContext,
        IMemoryCache cache) : ControllerBase
    {
        private void InvalidateProjectCache(string? userId, string? slug)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                cache.Remove($"user_projects_{userId}");
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                cache.Remove($"page_slug_{slug.Trim().ToLowerInvariant()}");
            }

            Console.WriteLine($"[CACHE INVALIDATION] UserId: '{userId}', Slug: '{slug}' önbellekten temizlendi.");
        }

        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            return Ok(new { status = "awake", timestamp = DateTime.UtcNow });
        }

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
                InvalidateProjectCache(command.UserId, response.Slug);

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

            string cacheKey = $"user_projects_{userId}";

            if (cache.TryGetValue(cacheKey, out IEnumerable<LandingPage>? cachedProjects) && cachedProjects != null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return Ok(cachedProjects);
            }

            var projects = await repository.GetByUserIdAsync(userId);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(45))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(90));

            cache.Set(cacheKey, projects, cacheOptions);

            Response.Headers["X-Cache"] = "MISS";
            return Ok(projects);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLandingPage(string id, [FromQuery] string userId)
        {
            var existingPage = await repository.GetByIdAsync(id);

            var command = new DeleteLandingPageCommand { PageId = id, UserId = userId };
            var response = await mediator.Send(command);

            if (response.IsSuccess)
            {
                InvalidateProjectCache(userId, existingPage?.Slug);
                return Ok(response);
            }

            return BadRequest(new { message = response.Message });
        }

        [HttpGet("{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLandingPageBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new { message = "Slug gereklidir." });
            }

            string normalizedSlug = slug.Trim().ToLowerInvariant();
            string cacheKey = $"page_slug_{normalizedSlug}";

            if (cache.TryGetValue(cacheKey, out object? cachedPageDto) && cachedPageDto != null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return Ok(cachedPageDto);
            }

            var pageData = await repository.GetBySlugAsync(slug);
            if (pageData == null)
            {
                return NotFound(new { message = "Bu isme ait bir platform bulunamadı." });
            }

            var responseDto = new
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
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High);

            cache.Set(cacheKey, responseDto, cacheOptions);

            Response.Headers["X-Cache"] = "MISS";
            return Ok(responseDto);
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
                    existingPage.AiConfig = new AiPageConfig();
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

                InvalidateProjectCache(existingPage.UserId, existingPage.Slug);

                return Ok(new { message = "Proje başarıyla güncellendi.", page = existingPage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GÜNCELLEME HATASI]: {ex.Message} \n {ex.StackTrace}");
                return StatusCode(500, new { message = $"Sunucu hatası: {ex.Message}" });
            }
        }

        [HttpPost("ai-assist")]
        [AllowAnonymous]
        public async Task<IActionResult> AiAssist(
            [FromBody] AiAssistRequest request,
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