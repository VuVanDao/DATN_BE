using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Queries.GetUsers
{
    public class GetUsersQuery : IRequest<PagedResult<UserManagementDto>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
