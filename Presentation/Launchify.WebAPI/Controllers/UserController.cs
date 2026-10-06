using Launchify.Application.Features.Users.Commands.SyncUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Launchify.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncUser([FromBody] SyncUserCommand command)
        {
            var response = await _mediator.Send(command);

            if (response.IsSuccess)
            {
                return Ok(response);
            }

            return BadRequest(new { message = response.Message });
        }
    }
}