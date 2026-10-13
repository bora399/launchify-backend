using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using System;
using Launchify.Application.DTOs;

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
        [AllowAnonymous]
        public async Task<IActionResult> JoinWaitlist([FromBody] WaitlistRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PageId))
            {
                return BadRequest("Email and PageId are required.");
            }

            if (!string.IsNullOrWhiteSpace(request.Honeypot))
            {
                return Ok(new { message = "Successfully joined the waitlist." });
            }

            string cleanEmail = request.Email.Trim().ToLowerInvariant();

            bool alreadyExists = await _waitlistRepository.ExistsAsync(request.PageId, cleanEmail);
            if (alreadyExists)
            {
                return StatusCode(409, new { message = "Bu e-posta adresi zaten bekleme listesinde kayıtlı." });
            }

            var entry = new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                PageId = request.PageId,
                Email = cleanEmail,
                CreatedAt = DateTime.UtcNow
            };

            await _waitlistRepository.AddAsync(entry);
            return Ok(new { message = "Successfully joined the waitlist." });
        }

        [HttpGet("{pageId}")]
        public async Task<IActionResult> GetWaitlistByPageId(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                return BadRequest("PageId gereklidir.");
            }

            var entries = await _waitlistRepository.GetByPageIdAsync(pageId);
            return Ok(entries);
        }
    }
}