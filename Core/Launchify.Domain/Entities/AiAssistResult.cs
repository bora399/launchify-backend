using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launchify.Domain.Entities
{
    public class AiAssistResult
    {
        public string HeroTitle { get; set; } = string.Empty;
        public string MarketingCopy { get; set; } = string.Empty;
        public string CallToActionText { get; set; } = string.Empty;
        public string SuggestedTemplate { get; set; } = string.Empty;
        public string SuggestedAccentColor { get; set; } = string.Empty;
    }
}
