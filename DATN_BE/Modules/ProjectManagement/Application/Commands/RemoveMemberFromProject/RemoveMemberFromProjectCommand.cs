using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.RemoveMemberFromProject
{
    public class RemoveMemberFromProjectCommand : IRequest<bool>
    {
        public Guid ProjectId { get; set; }
        public Guid MemberId { get; set; }
        public RemoveMemberFromProjectCommand(Guid projectId, Guid memberId)
        {
            this.ProjectId = projectId;
            this.MemberId = memberId;
        }
    }
}