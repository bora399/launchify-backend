using Launchify.Application.DTOs;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        // BaseAddress tanımlandığı için "BaseAddress must be set" hatası artık oluşamaz
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
            Timeout = TimeSpan.FromSeconds(60)
        };

        private static readonly string[] Models = { "gemini-3.8-flash", "gemini-3.5-flash-lite", "gemini-1.5-flash" };
        private const int RetriesPerModel = 3;

        public GeminiAiService(IConfiguration configuration)
        {
            var rawKey = configuration["Gemini:ApiKey"]
                      ?? configuration["Gemini__ApiKey"]
                      ?? configuration["GEMINI_API_KEY"]
                      ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                      ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                      ?? string.Empty;

            // Tırnak işaretlerini (", '), boşlukları ve satır sonlarını tamamen temizle
            _apiKey = Regex.Replace(rawKey ?? string.Empty, @"[\s""']+", "");
        }

        public async Task<AiPageConfig> GenerateContentAsync(string productName, string themeType, string productDescription, Func<string, Task> logCallback)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                await logCallback("❌ Hata: Gemini API anahtarı bulunamadı.");
                throw new InvalidOperationException("Gemini API Key yapılandırmada bulunamadı.");
            }

            await logCallback($"[Adım 1/4] '{productName}' için sistem analizi başlatıldı...");
            await Task.Delay(500);

            string prompt = $@"
    Sen uzman bir ürün pazarlama stratejisti, CRO uzmanı ve metin yazarısın. Yeni bir yazılım/ürün için e-posta toplamaya (Lead Capture) yönelik, yüksek dönüşüm odaklı bir açılış sayfası içeriği üreteceksin.
    
    Ürün / Girişim Adı: {productName} 
    Marka Tonu: {(themeType == "modern" ? "Modern, teknolojik (Startup tarzı)" : "Kurumsal, güvenilir (B2B tarzı)")}
    Ürünün Özellikleri / Amacı: {productDescription}

    SADECE aşağıdaki formatta geçerli bir JSON objesi dön. Asla Markdown veya ekstra metin ekleme:
    {{
        ""AiGeneratedHeroTitle"": ""Ürünün ana değer önerisini anlatan kısa ve vurucu başlık."",
        ""AiGeneratedMarketingCopy"": ""Ürünün özelliklerini müşteriye fayda sağlayacak şekilde anlatan 30-40 kelimelik ikna edici alt metin."",
        ""CallToActionText"": ""2-3 kelimelik vurucu buton metni"",
        ""Features"": [
            {{ ""Title"": ""1. Özellik Başlığı"", ""Description"": ""Kullanıcıya sağladığı spesifik faydayı anlatan 1-2 cümlelik açıklama."" }},
            {{ ""Title"": ""2. Özellik Başlığı"", ""Description"": ""Kullanıcıya sağladığı spesifik faydayı anlatan 1-2 cümlelik açıklama."" }},
            {{ ""Title"": ""3. Özellik Başlığı"", ""Description"": ""Kullanıcıya sağladığı spesifik faydayı anlatan 1-2 cümlelik açıklama."" }}
        ],
        ""AccentColor"": ""{(themeType == "modern" ? "#6366F1" : "#0F172A")}""
    }}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json" }
            };

            string payload = JsonSerializer.Serialize(requestBody);
            string safeKey = Uri.EscapeDataString(_apiKey);
            Exception lastError = null;

            await logCallback("[Adım 2/4] B2B/SaaS platform mimarisi tasarlanıyor...");

            foreach (var model in Models)
            {
                // BaseAddress tanımlı olduğu için relative path kullanımı güvenlidir
                string relativePath = $"v1beta/models/{model}:generateContent?key={safeKey}";

                for (int attempt = 0; attempt < RetriesPerModel; attempt++)
                {
                    try
                    {
                        await logCallback($"[Adım 3/4] AI motoru ile iletişim kuruluyor ({model} - Deneme: {attempt + 1})...");

                        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        using var response = await _httpClient.PostAsync(relativePath, content);
                        var responseString = await response.Content.ReadAsStringAsync();

                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            lastError = new Exception($"{model}: {(int)response.StatusCode} - {responseString}");
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                            await Task.Delay(delay);
                            continue;
                        }

                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            lastError = new Exception($"{model}: 404 - {responseString}");
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

                        await logCallback("[Adım 4/4] Yanıt başarıyla alındı...");

                        string cleanJson = ExtractJson(textResult);

                        var result = JsonSerializer.Deserialize<AiPageConfig>(
                            cleanJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        await logCallback("✅ İşlem Tamamlandı!");
                        return result;
                    }
                    catch (TaskCanceledException ex)
                    {
                        lastError = ex;
                    }
                    catch (HttpRequestException ex)
                    {
                        lastError = ex;
                    }
                }
            }

            throw new AiServiceUnavailableException("AI servisi şu an yanıt veremiyor.", lastError);
        }

        public async Task<AiAssistResult> AssistContentAsync(AiAssistRequest request)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Gemini API Key yapılandırmada bulunamadı.");
            }

            string modeDirective = request.Mode switch
            {
                "punchy" => "Metinleri iddialı, enerjik, merak uyandıran ve harekete geçirici startup dilinde yeniden yaz.",
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

SADECE aşağıdaki formatta geçerli bir JSON objesi dön. Asla Markdown veya ekstra metin ekleme:
{{
  ""HeroTitle"": ""Yeni başlık"",
  ""MarketingCopy"": ""Yeni açıklama (25-35 kelime)"",
  ""CallToActionText"": ""2-3 kelimelik buton metni"",
  ""SuggestedTemplate"": ""{(request.Mode == "redesign" ? "Aurora, Brutal, Corporate veya Minimal" : request.CurrentTemplate)}"",
  ""SuggestedAccentColor"": ""{(request.Mode == "redesign" ? "#6366F1" : "#3B82F6")}""
}}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json" }
            };

            string payload = JsonSerializer.Serialize(requestBody);
            string safeKey = Uri.EscapeDataString(_apiKey);
            Exception lastError = null;

            foreach (var model in Models)
            {
                string relativePath = $"v1beta/models/{model}:generateContent?key={safeKey}";

                try
                {
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await _httpClient.PostAsync(relativePath, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        lastError = new Exception($"{model}: HTTP {(int)response.StatusCode} - {responseString}");
                        continue;
                    }

                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    {
                        continue;
                    }

                    var textResult = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();

                    if (string.IsNullOrWhiteSpace(textResult)) continue;

                    string cleanJson = ExtractJson(textResult);

                    var result = JsonSerializer.Deserialize<AiAssistResult>(
                        cleanJson,
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

        private static string ExtractJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";
            string text = input.Replace("```json", "").Replace("```", "").Trim();
            int firstBrace = text.IndexOf('{');
            int lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return text.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            return text;
        }
    }
}