using Launchify.Application.Common.Utils;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage
{
    public class CreateLandingPageCommandHandler : IRequestHandler<CreateLandingPageCommand, CreateLandingResponse>
    {
        private readonly ILandingPageRepository _repository;
        private readonly IAiGeneratorService _aiService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateLandingPageCommandHandler> _logger;

        public CreateLandingPageCommandHandler(
            ILandingPageRepository repository,
            IAiGeneratorService aiService,
            IUserRepository userRepository,
            ILogger<CreateLandingPageCommandHandler> logger)
        {
            _repository = repository;
            _aiService = aiService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CreateLandingResponse> Handle(CreateLandingPageCommand request, CancellationToken cancellationToken)
        {
            bool creditDeducted = false;

            try
            {
                if (string.IsNullOrWhiteSpace(request.UserId))
                {
                    return new CreateLandingResponse
                    {
                        IsSuccess = false,
                        Message = "Güvenlik İhlali: Proje oluşturmak için sisteme giriş yapmış olmanız gerekmektedir."
                    };
                }

                creditDeducted = await _userRepository.DeductCreditAsync(request.UserId);

                if (!creditDeducted)
                {
                    var existingPages = await _repository.GetByUserIdAsync(request.UserId);
                    int count = existingPages != null ? existingPages.Count() : 0;

                    if (count < 3)
                    {
                        _logger.LogInformation("Kullanıcı ({UserId}) 3'ten az projeye sahip ({Count}/3) fakat kredisi 0. Otomatik telafi ediliyor...", request.UserId, count);
                        await _userRepository.RefundCreditAsync(request.UserId);
                        creditDeducted = await _userRepository.DeductCreditAsync(request.UserId);
                    }
                }

                if (!creditDeducted)
                {
                    return new CreateLandingResponse { IsSuccess = false, Message = "Yeterli proje oluşturma krediniz bulunmuyor. Maksimum 3 aktif proje oluşturabilirsiniz." };
                }

                _logger.LogInformation("Creating new landing page for product: {ProductName}", request.ProductName);

                var generatedSlug = string.IsNullOrWhiteSpace(request.ProductName)
                    ? Guid.NewGuid().ToString()[..8]
                    : SlugUtility.GenerateSlug(request.ProductName);

                var aiConfig = await _aiService.GenerateContentAsync(
                    request.ProductName,
                    request.ThemeType,
                    request.ProductDescription,
                    request.LogCallback ?? (async (_) => await Task.CompletedTask)
                );

                var newPage = new LandingPage
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
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
                if (creditDeducted)
                {
                    try
                    {
                        await _userRepository.RefundCreditAsync(request.UserId);
                        _logger.LogWarning("İşlem patladığı için kullanıcıya ({UserId}) 1 kredisi iade edildi.", request.UserId);
                    }
                    catch (Exception refundEx)
                    {
                        _logger.LogError(refundEx, "Kredi geri iadesi sırasında hata.");
                    }
                }

                Console.WriteLine($"\n--- KRİTİK HATA BAŞLANGICI ---\n{ex.ToString()}\n--- KRİTİK HATA BİTİŞİ ---\n");

                return new CreateLandingResponse
                {
                    IsSuccess = false,
                    Message = "Platform oluşturulurken sunucu kaynaklı bir sorun meydana geldi: " + ex.Message
                };
            }
        }
    }
}