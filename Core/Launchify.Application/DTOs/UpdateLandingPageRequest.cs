using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launchify.Application.DTOs
{
    public class UpdateLandingPageRequest
    {
        public string ProductName { get; set; }
        public string TemplateType { get; set; }
        public string HeroTitle { get; set; }
        public string MarketingCopy { get; set; }
        public string CallToActionText { get; set; }
        public string AccentColor { get; set; }
    }
}
