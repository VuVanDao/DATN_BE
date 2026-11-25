using DATN_BE.Modules.Identity.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Identity.Application.Queries.GetProfile
{
    public class GetProfileQuery:IRequest<UserProfileDto>
    {
        // Query không cần tham số UserId vì UserId sẽ được lấy từ ICurrentUserService
    }
}
