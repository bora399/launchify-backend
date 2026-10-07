using Launchify.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsQueueService _queueService;

        public AnalyticsController(AnalyticsQueueService queueService)
        {
            _queueService = queueService;
        }

        [HttpPost("{pageId}/visit")]
        public async Task<IActionResult> RecordVisit(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                return BadRequest();
            }

            await _queueService.EnqueueVisitAsync(pageId);

            return Ok(new { message = "Ziyaret kuyruğa alındı." });
        }
    }
}