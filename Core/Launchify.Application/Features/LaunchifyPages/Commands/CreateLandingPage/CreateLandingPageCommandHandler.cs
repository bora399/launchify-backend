using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Launchify.Application.Common.Utils;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage
{
    public class CreateLandingPageCommandHandler : IRequestHandler<CreateLandingPageCommand, CreateLandingResponse>
    {
        private readonly ILandingPageRepository _repository;
        private readonly IAiGeneratorService _aiService;
        private readonly ILogger<CreateLandingPageCommandHandler> _logger;

        public CreateLandingPageCommandHandler(
            ILandingPageRepository repository,
            IAiGeneratorService aiService,
            ILogger<CreateLandingPageCommandHandler> logger)
        {
            _repository = repository;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<CreateLandingResponse> Handle(CreateLandingPageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new landing page for product: {ProductName}", request.ProductName);

                var generatedSlug = string.IsNullOrWhiteSpace(request.ProductName)
                    ? Guid.NewGuid().ToString()[..8]
                    : SlugUtility.GenerateSlug(request.ProductName);

                var aiConfig = await _aiService.GenerateContentAsync(
                    request.ProductName,
                    request.ThemeType,
                    request.ProductDescription);

                var newPage = new LandingPage
                {
                    Id = Guid.NewGuid(),
                    ProductName = request.ProductName,
                    Slug = generatedSlug,
                    ContactEmail = request.ContactEmail,
                    DemoLink = request.DemoLink,
                    ProductDescription = request.ProductDescription,
                    AiConfig = aiConfig,
                    CreatedAt = DateTime.UtcNow,
                    TemplateType = request.TemplateType ?? request.ThemeType,
                    IsActive = true
                };

                await _repository.AddAsync(newPage);

                _logger.LogInformation("Landing page successfully created with ID: {PageId}", newPage.Id);

                return new CreateLandingResponse
                {
                    IsSuccess = true,
                    Message = "Platform başarıyla oluşturuldu.",
                    GeneratedPageId = newPage.Id.ToString(),
                    Slug = generatedSlug
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n--- KRİTİK HATA BAŞLANGICI ---\n{ex.ToString()}\n--- KRİTİK HATA BİTİŞİ ---\n");

                return new CreateLandingResponse
                {
                    IsSuccess = false,
                    Message = "Platform oluşturulurken sunucu kaynaklı bir sorun meydana geldi."
                };
            }
        }
    }
}