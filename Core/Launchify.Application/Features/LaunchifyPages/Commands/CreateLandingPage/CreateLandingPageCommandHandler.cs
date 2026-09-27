using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
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
                // 1. ADIM: URL için temiz Slug üretimi ("Videocu Uygulaması" -> "videocu-uygulamasi")
                string generatedSlug = string.Empty;
                if (!string.IsNullOrEmpty(request.ProductName))
                {
                    generatedSlug = request.ProductName.ToLower().Trim()
                        .Replace(" ", "-")
                        .Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s")
                        .Replace("ı", "i").Replace("ö", "o").Replace("ç", "c");

                    // Özel karakterleri sil ve çift tireleri tek tire yap
                    generatedSlug = System.Text.RegularExpressions.Regex.Replace(generatedSlug, @"[^a-z0-9\s-]", "");
                    generatedSlug = System.Text.RegularExpressions.Regex.Replace(generatedSlug, @"\s+", "-").Trim('-');
                }
                else
                {
                    generatedSlug = Guid.NewGuid().ToString().Substring(0, 8); // İsim yoksa rastgele ver
                }


                // 2. ADIM: Yapay Zeka'dan temaya ve ürüne uygun pazarlama metinlerini üret
                var aiConfig = await _aiService.GenerateContentAsync(
                    request.ProductName,
                    request.ThemeType,
                    request.ProductDescription);


                // 3. ADIM: Domain Nesnesini Oluştur (YENİ: Slug alanı eklendi)
                var newPage = new LandingPage
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    ProductName = request.ProductName,
                    Slug = generatedSlug, // Ürettiğimiz URL ismi veritabanı nesnesine ekleniyor
                    ThemeType = request.ThemeType,
                    ContactEmail = request.ContactEmail,
                    AdminPin = request.AdminPin,
                    DemoLink = request.DemoLink,
                    ProductDescription = request.ProductDescription,
                    AiConfig = aiConfig,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // 4. ADIM: Firebase'e Kaydet (CQRS Repository üzerinden)
                await _repository.AddAsync(newPage);


                // 5. ADIM: Başarılı Sonucu Dön (Next.js'in sayfaya uçabilmesi için ID ve Slug'ı veriyoruz)
                return new CreateLandingResponse
                {
                    IsSuccess = true,
                    Message = "Tebrikler! Ürün tanıtım sayfanız yapay zeka ile başarıyla oluşturuldu.",
                    GeneratedPageId = newPage.Id.ToString(),
                    Slug = generatedSlug // YENİ: Slug'ı response'a koyduk ki Controller onu Frontend'e yollasın
                };
            }
            catch (Exception ex)
            {
                // Hata durumunda frontend'e bilgi ver
                return new CreateLandingResponse
                {
                    IsSuccess = false,
                    Message = $"Sayfa oluşturulurken bir hata oluştu: {ex.Message}",
                    GeneratedPageId = null,
                    Slug = null
                };
            }
        }
    }
}