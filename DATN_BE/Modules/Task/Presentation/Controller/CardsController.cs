using DATN_BE.Modules.Task.Application.Commands.CreateCard;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Task.Presentation.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CardsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CardDto>>> CreateCard([FromBody] CreateCardCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardDto>.Ok(result,"Tạo card mới thành công"));
        }
    }
}
