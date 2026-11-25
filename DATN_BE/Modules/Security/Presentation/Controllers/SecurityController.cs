using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Modules.Security.Application.Queries.GetMyLoginHistory;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Security.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityController : ControllerBase
    {
        private readonly IMediator _mediator;
        public SecurityController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Xem lịch sử đăng nhập cá nhân (Audit Log)
        /// </summary>
        [HttpGet("me/login-history")]
        public async Task<ActionResult<ApiResponse<List<LoginHistoryDto>>>> GetMyLoginHistory([FromQuery] int limit = 20)
        {
            var result = await _mediator.Send(new GetMyLoginHistoryQuery { Limit = limit });
            return Ok(ApiResponse<List<LoginHistoryDto>>.Ok(result, "Lấy lịch sử đăng nhập thành công."));
        }
    }
}
