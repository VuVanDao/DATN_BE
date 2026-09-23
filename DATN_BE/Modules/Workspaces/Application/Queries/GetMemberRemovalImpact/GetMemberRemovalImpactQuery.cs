using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetMemberRemovalImpact
{
    public class GetMemberRemovalImpactQuery : IRequest<MemberRemovalImpactDto>
    {
        public Guid WorkspaceId { get; set; }
        public Guid MemberId { get; set; }
    }
}