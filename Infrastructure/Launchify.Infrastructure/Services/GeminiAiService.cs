using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Not: Namespace'i kendi projendeki haline (Launchify veya Masalimiz) göre ayarlarsın.
namespace Launchify.Infrastructure.Services
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
            Timeout = TimeSpan.FromSeconds(60)
        };

        private static readonly string[] Models = { "gemini-3.8-flash", "gemini-3.5-flash-lite" };
        private const int RetriesPerModel = 3;

        public GeminiAiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<AiPageConfig> GenerateContentAsync(string productName, string themeType, string productDescription, Func<string, Task> logCallback)
        {
            await logCallback($"[Adım 1/4] '{productName}' için sistem analizi başlatıldı...");
            await Task.Delay(1000);

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

            await logCallback("[Adım 2/4] B2B/SaaS platform mimarisi tasarlanıyor...");

            foreach (var model in Models)
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey.Trim()}";

                for (int attempt = 0; attempt < RetriesPerModel; attempt++)
                {
                    try
                    {
                        await logCallback($"[Adım 3/4] AI motoru ile iletişim kuruluyor (Model: {model} - Deneme: {attempt + 1})...");

                        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        using var response = await _httpClient.PostAsync(url, content);
                        var responseString = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            lastError = new Exception($"{model}: {(int)response.StatusCode} - {responseString}");

                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)) +
                                        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));

                            await logCallback($"[Uyarı] Sunucu yoğunluğu tespit edildi. {delay.TotalSeconds:F1} saniye sonra tekrar deneniyor...");
                            await Task.Delay(delay);
                            continue;
                        }

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            lastError = new Exception($"{model}: 404 - {responseString}");
                            await logCallback($"[Bilgi] {model} modeline ulaşılamadı. Alternatif modele geçiliyor...");
                            break;
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

                        await logCallback("[Adım 4/4] Yanıt başarıyla alındı. React bileşenleri oluşturuluyor...");

                        if (!string.IsNullOrEmpty(textResult))
                        {
                            textResult = textResult.Replace("```json", "").Replace("```", "").Trim();
                        }

                        var result = JsonSerializer.Deserialize<AiPageConfig>(
                            textResult,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        await logCallback("✅ İşlem Tamamlandı: Platformunuz canlıya alınmaya hazır!");

                        return result;
                    }
                    catch (TaskCanceledException ex)
                    {
                        lastError = ex;
                        await logCallback("[Hata] Bağlantı zaman aşımına uğradı. Yeniden deneniyor...");
                    }
                    catch (HttpRequestException ex)
                    {
                        lastError = ex;
                        await logCallback("[Hata] Ağ erişim sorunu yaşandı. Yeniden deneniyor...");
                    }
                }
            }

            await logCallback("❌ Hata: Tüm AI modelleri denendi ancak sunucu yanıt vermiyor. Lütfen daha sonra tekrar deneyin.");
            throw new AiServiceUnavailableException("AI servisi şu an yoğun.", lastError);
        }
    }
}