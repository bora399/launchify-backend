using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using System;

namespace Launchify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaitlistController : ControllerBase
    {
        private readonly IWaitlistRepository _waitlistRepository;

        public WaitlistController(IWaitlistRepository waitlistRepository)
        {
            _waitlistRepository = waitlistRepository;
        }

        [HttpPost]
        public async Task<IActionResult> JoinWaitlist([FromBody] WaitlistRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PageId))
            {
                return BadRequest("Email and PageId are required.");
            }

            var entry = new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                PageId = request.PageId,
                Email = request.Email
            };

            await _waitlistRepository.AddAsync(entry);
            return Ok(new { message = "Successfully joined the waitlist." });
        }
    }

    public class WaitlistRequest
    {
        public string PageId { get; set; }
        public string Email { get; set; }
    }
}