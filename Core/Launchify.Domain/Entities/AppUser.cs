using System;

namespace Launchify.Domain.Entities
{
    public class AppUser
    {
        public string Id { get; set; }

        public string Email { get; set; }
        public string FullName { get; set; }

        // Kullanıcının oluşturduğu sayfa hakkı var mı? (Gelecekteki premium paketler için)
        public int RemainingPageCredits { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}