using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Masalimiz.Infrastructure.Services
{
    public class AiServiceUnavailableException : Exception
    {
        public AiServiceUnavailableException(string message, Exception inner = null)
            : base(message, inner) { }
    }

    public class GeminiAiService : IAiGeneratorService
    {
        private readonly string _apiKey;

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(40)
        };

        // Sırayla denenecek modeller (asıl model + yedek). Güncel model adlarını Google'dan kontrol et.
        private static readonly string[] Models = { "gemini-2.5-flash", "gemini-2.5-flash-lite" };
        private const int RetriesPerModel = 3;

        public GeminiAiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<AiPageConfig> GenerateContentAsync(string productName, string themeType, string productDescription)
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
                },
                generationConfig = new { responseMimeType = "application/json" }
            };

            string payload = JsonSerializer.Serialize(requestBody);
            Exception lastError = null;

            foreach (var model in Models)
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey.Trim()}";

                for (int attempt = 0; attempt < RetriesPerModel; attempt++)
                {
                    try
                    {
                        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        using var response = await _httpClient.PostAsync(url, content);
                        var responseString = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            lastError = new Exception($"{model}: {(int)response.StatusCode} - {responseString}");

                            // Exponential backoff + jitter: ~1s, 2s, 4s
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) +
                                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
                            await Task.Delay(delay);
                            continue;
                        }

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

                        return JsonSerializer.Deserialize<AiPageConfig>(
                            textResult,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (TaskCanceledException ex) // timeout
                    {
                        lastError = ex;
                    }
                    catch (HttpRequestException ex)  // ağ hatası
                    {
                        lastError = ex;
                    }
                }
            }

            throw new AiServiceUnavailableException("AI servisi şu an yoğun.", lastError);
        }
    }
}