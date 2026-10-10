using Launchify.Application.DTOs;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
    Sen uzman bir ürün pazarlama stratejisti, dönüşüm oranı optimizasyonu (CRO) uzmanı ve metin yazarısın. Yeni bir yazılım/ürün için e-posta toplamaya (Lead Capture) yönelik, yüksek dönüşüm odaklı bir açılış sayfası (Landing Page) içeriği üreteceksin.
    
    Ürün / Girişim Adı: {productName} 
    Marka Tonu: {(themeType == "modern" ? "Modern, yenilikçi ve teknolojik (Startup tarzı)" : "Kurumsal, güvenilir ve ciddi (B2B tarzı)")}
    Ürünün Özellikleri / Amacı: {productDescription}

    SADECE aşağıdaki formatta geçerli bir JSON objesi dön. Asla Markdown (```json gibi) veya ekstra metin ekleme:
    {{
        ""AiGeneratedHeroTitle"": ""Ürünün ana değer önerisini (value proposition) anlatan kısa ve vurucu başlık."",
        ""AiGeneratedMarketingCopy"": ""Ürünün özelliklerini müşteriye fayda sağlayacak şekilde anlatan, yaklaşık 30-40 kelimelik ikna edici alt metin."",
        ""CallToActionText"": ""Kullanıcıyı e-posta bırakmaya itecek 2-3 kelimelik vurucu buton metni (Örn: Erken Erişime Katıl, Ücretsiz Başla)."",
        ""Features"": [
            {{ ""Title"": ""1. Özelliğin Vurucu Başlığı"", ""Description"": ""Bu özelliğin kullanıcıya sağladığı spesifik faydayı anlatan 1-2 cümlelik açıklama."" }},
            {{ ""Title"": ""2. Özelliğin Vurucu Başlığı"", ""Description"": ""Bu özelliğin kullanıcıya sağladığı spesifik faydayı anlatan 1-2 cümlelik açıklama."" }},
            {{ ""Title"": ""3. Özelliğin Vurucu Başlığı"", ""Description"": ""Bu özelliğin kullanıcıya sağladığı spesifik faydayı anlatan 1-2 cümlelik açıklama."" }}
        ],
        ""Faqs"": [
        {{ ""Question"": ""Verilerimiz güvende mi?"", ""Answer"": ""Tüm altyapımız uçtan uca şifreleme ve kurumsal güvenlik standartlarıyla korunmaktadır."" }},
        {{ ""Question"": ""Kurulum ne kadar sürer?"", ""Answer"": ""Bulut tabanlı altyapımız sayesinde dakikalar içinde kullanmaya başlayabilirsiniz."" }},
        {{ ""Question"": ""Erken erişim avantajları nelerdir?"", ""Answer"": ""İlk kaydolan kurumsal kullanıcılarımıza özel indirimler ve öncelikli destek sağlanacaktır."" }}
    ],
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
                string url = $"[https://generativelanguage.googleapis.com/v1beta/models/](https://generativelanguage.googleapis.com/v1beta/models/){model}:generateContent?key={_apiKey.Trim()}";

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

        public async Task<AiAssistResult> AssistContentAsync(AiAssistRequest request)
        {
            string modeDirective = request.Mode switch
            {
                "punchy" => "Metinleri iddialı, enerjik, merak uyandıran ve harekete geçirici startup/indie-hacker dilinde yeniden yaz.",
                "corporate" => "Metinleri güven veren, B2B SaaS uyumlu, kurumsal ve profesyonel bir dille yeniden yaz.",
                "minimal" => "Metinleri olabildiğince az kelimeyle, net, zarif ve minimalist bir dille yeniden yaz.",
                "redesign" => "Ürünün değer önerisine en uygun şablonu (Aurora, Brutal, Corporate, Minimal arasından BİRİ) ve en uyumlu HEX vurgu rengini seç.",
                _ => "Metinleri dönüşüm oranını maksimize edecek şekilde optimize et."
            };

            string prompt = $@"
Sen uzman bir SaaS büyüme danışmanı ve sanat yönetmenisin.
Ürün Adı: {request.ProductName}
Mevcut Başlık: {request.CurrentTitle}
Mevcut Açıklama: {request.CurrentCopy}
Mevcut Şablon: {request.CurrentTemplate}

GÖREVİN: {modeDirective}

SADECE aşağıdaki formatta saf bir JSON objesi dön. Asla Markdown veya ekstra metin ekleme:
{{
  ""HeroTitle"": ""Yeni veya optimize edilmiş başlık"",
  ""MarketingCopy"": ""Yeni veya optimize edilmiş açıklama (25-35 kelime)"",
  ""CallToActionText"": ""2-3 kelimelik etkili buton metni"",
  ""SuggestedTemplate"": ""{(request.Mode == "redesign" ? "Aurora, Brutal, Corporate veya Minimal" : request.CurrentTemplate)}"",
  ""SuggestedAccentColor"": ""{(request.Mode == "redesign" ? "#6366F1" : "#3B82F6")}""
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
            string key = (_apiKey ?? "").Trim();
            Exception lastError = null;

            foreach (var model in Models)
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={key}";

                try
                {
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await _httpClient.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        lastError = new Exception($"{model}: HTTP {(int)response.StatusCode} - {responseString}");
                        continue;
                    }

                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var candidates = jsonDoc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() == 0) continue;

                    var textResult = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();

                    if (!string.IsNullOrEmpty(textResult))
                    {
                        textResult = textResult.Replace("```json", "").Replace("```", "").Trim();
                    }

                    var result = JsonSerializer.Deserialize<AiAssistResult>(
                        textResult,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result != null) return result;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    continue;
                }
            }

            throw new AiServiceUnavailableException($"AI asistanı yanıt veremedi. Hata: {lastError?.Message}");
        }
    }
}