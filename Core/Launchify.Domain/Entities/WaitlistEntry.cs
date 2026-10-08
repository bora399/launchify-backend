using System;

namespace Launchify.Domain.Entities
{
    public class WaitlistEntry : BaseEntity 
    {
        public string PageId { get; set; } 
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}