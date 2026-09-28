using System;
using System.Collections.Generic;

namespace Launchify.Domain.Entities
{
    public class LandingPage : BaseEntity
    {
        public string ProductName { get; set; }
        public string ThemeType { get; set; }
        public string ContactEmail { get; set; }
        public string DemoLink { get; set; }
        public string ProductDescription { get; set; }
        public AiPageConfig AiConfig { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string Slug { get; set; }

    }

    public class AiPageConfig
    {
        public string AiGeneratedHeroTitle { get; set; }
        public string AiGeneratedMarketingCopy { get; set; }
        public string AccentColor { get; set; }
    }
}