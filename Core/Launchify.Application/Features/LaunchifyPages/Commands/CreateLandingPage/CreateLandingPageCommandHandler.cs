using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Launchify.Application.Interfaces;
using MediatR;
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

        public CreateLandingPageCommandHandler(ILandingPageRepository repository, IAiGeneratorService aiService)
        {
            _repository = repository;
            _aiService = aiService;
        }

        public async Task<CreateLandingResponse> Handle(CreateLandingPageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. ADIM: Yapay Zeka'dan temaya ve ürüne uygun pazarlama metinlerini üret
                var aiConfig = await _aiService.GenerateContentAsync(
                    request.ProductName,
                    request.ThemeType,
                    request.ProductDescription);

                // 2. ADIM: Domain Nesnesini Oluştur (Yeni kurumsal alanlarla birlikte)
                var newPage = new LandingPage
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    ProductName = request.ProductName,
                    ThemeType = request.ThemeType,
                    ContactEmail = request.ContactEmail,
                    AdminPin = request.AdminPin,
                    DemoLink = request.DemoLink,
                    ProductDescription = request.ProductDescription,
                    AiConfig = aiConfig,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true

                    // Not: Eğer LandingPage sınıfında Photos listesi varsa burada onu da new List<PhotoItem>() olarak initialize edebilirsin
                };

                // 3. ADIM: Fotoğrafları Nesneye Ekle (Eğer Photos tanımı LandingPage.cs içinde varsa)
                // if (request.Photos != null && request.Photos.Any()) { ... }

                // 4. ADIM: Firebase'e Kaydet (CQRS Repository üzerinden)
                await _repository.AddAsync(newPage);

                // 5. ADIM: Başarılı Sonucu Dön (Next.js'in sayfaya uçabilmesi için oluşturulan ID'yi de veriyoruz)
                return new CreateLandingResponse
                {
                    IsSuccess = true,
                    Message = "Tebrikler! Ürün tanıtım sayfanız yapay zeka ile başarıyla oluşturuldu.",
                    GeneratedPageId = newPage.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                // Hata durumunda frontend'e bilgi ver
                return new CreateLandingResponse
                {
                    IsSuccess = false,
                    Message = $"Sayfa oluşturulurken bir hata oluştu: {ex.Message}",
                    GeneratedPageId = null
                };
            }
        }
    }
}