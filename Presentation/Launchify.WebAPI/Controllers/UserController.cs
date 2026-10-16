using Launchify.Application.Features.Users.Commands.SyncUser;
using Launchify.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserRepository _userRepository; 

        public UserController(IMediator mediator, IUserRepository userRepository)
        {
            _mediator = mediator;
            _userRepository = userRepository;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncUser([FromBody] SyncUserCommand command)
        {
            command.Uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            command.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var response = await _mediator.Send(command);
            if (response.IsSuccess) return Ok(response);
            return BadRequest(new { message = response.Message });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            if (id != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(user);
        }
    }
}