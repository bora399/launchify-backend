using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launchify.Application.DTOs
{
    public class AiAssistRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string CurrentTitle { get; set; } = string.Empty;
        public string CurrentCopy { get; set; } = string.Empty;
        public string CurrentTemplate { get; set; } = "Aurora";
        public string Mode { get; set; } = "punchy"; // "punchy", "corporate", "minimal", "redesign"
    }
}
