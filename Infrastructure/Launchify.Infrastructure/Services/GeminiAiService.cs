using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Masalimiz.Infrastructure.Services
{
    public class GeminiAiService : IAiGeneratorService
    {
        private readonly string _apiKey;

        public GeminiAiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<AiPageConfig> GenerateContentAsync(string productName, string themeType, string productDescription)
        {
            try
            {
                var googleAi = new GoogleAI(_apiKey.Trim());
                var model = googleAi.GenerativeModel(Model.GeminiFlashLatest);

                string prompt = $@"
Sen uzman bir ürün pazarlama stratejisti ve metin yazarısın (Copywriter). Yeni bir yazılım/ürün için dönüşüm odaklı (conversion-optimized) bir açılış sayfası (Landing Page) içeriği üreteceksin.
Ürün / Girişim Adı: {productName} 
Marka Tonu: {(themeType == "modern" ? "Modern, yenilikçi ve teknolojik (Startup tarzı)" : "Kurumsal, güvenilir ve ciddi (B2B tarzı)")}
Ürünün Özellikleri / Amacı: {productDescription}

SADECE aşağıdaki formatta geçerli bir JSON objesi dön. Asla Markdown veya ekstra metin ekleme:
{{
    ""AiGeneratedHeroTitle"": ""Ürünün ana değer önerisini (value proposition) anlatan kısa ve vurucu slogan"",
    ""AiGeneratedMarketingCopy"": ""Ürünün özelliklerini müşteriye fayda sağlayacak şekilde anlatan, yaklaşık 40-50 kelimelik profesyonel pazarlama metni."",
    ""AccentColor"": ""{(themeType == "modern" ? "#2563EB" : "#0F172A")}""
}}";

                var response = await model.GenerateContent(prompt);
                string textResult = response.Text;

                if (!string.IsNullOrEmpty(textResult))
                {
                    textResult = textResult.Replace("```json", "").Replace("```", "").Trim();
                }

                return JsonSerializer.Deserialize<AiPageConfig>(textResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                throw new Exception($"Gemini SDK Hatası: {ex.Message}");
            }
        }
    }
}