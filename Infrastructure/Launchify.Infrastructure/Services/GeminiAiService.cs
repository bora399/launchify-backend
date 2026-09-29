using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Masalimiz.Infrastructure.Services
{
    public class GeminiAiService : IAiGeneratorService
    {
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GeminiAiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<AiPageConfig> GenerateContentAsync(string productName, string themeType, string productDescription)
        {
            try
            {
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

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={_apiKey.Trim()}";

                var response = await _httpClient.PostAsync(url, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                   throw new Exception($"Google API Hatası: {response.StatusCode} - {responseString}");
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                var textResult = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

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