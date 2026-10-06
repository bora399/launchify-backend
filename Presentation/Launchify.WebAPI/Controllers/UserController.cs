using Launchify.Application.Features.Users.Commands.SyncUser;
using Launchify.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var response = await _mediator.Send(command);
            if (response.IsSuccess) return Ok(response);
            return BadRequest(new { message = response.Message });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(user);
        }
    }
}