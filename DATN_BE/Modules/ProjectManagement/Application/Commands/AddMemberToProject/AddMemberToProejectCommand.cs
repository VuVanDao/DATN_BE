using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.AddMemberToProejct
{
        public class AddProjectMemberEntry
    {
        public Guid UserId { get; set; }
        public Guid? RoleId { get; set; } // Nếu null, mặc định gán ProjectMember
    }
    public class AddMemberToProejctCommand: IRequest<List<ProjectMemberDto>>
    {
        public Guid ProjectId { get; set; }
         public List<AddProjectMemberEntry> Members { get; set; } = new();
    }
}