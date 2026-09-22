using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectMember
{
    public class GetProjectMemberQuery : IRequest<List<ProjectMemberDto>>
    {
        public Guid projectId { get; set; }
        public GetProjectMemberQuery(Guid id)
        {
            projectId = id;
        }
    }
}