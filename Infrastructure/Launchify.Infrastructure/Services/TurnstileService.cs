using System.Text.Json;

namespace Launchify.Application.Services
{
    public class TurnstileService
    {
        private readonly HttpClient _httpClient;

        private const string SecretKey = "SENIN_CLOUDFLARE_SECRET_KEY_BURAYA";

        public TurnstileService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", SecretKey),
                new KeyValuePair<string, string>("response", token)
            });

            var response = await _httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);

            if (!response.IsSuccessStatusCode) return false;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(jsonString);

            return jsonDoc.RootElement.GetProperty("success").GetBoolean();
        }
    }
}