using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace DATN_BE.Modules.Workspaces.Application.Commands.RemoveMemberFromProject
{
    public class RemoveMemberFromWorkspaceCommand : IRequest<bool>
    {
        public Guid WorkspaceId { get; set; }
        public Guid MemberId { get; set; }
        public RemoveMemberFromWorkspaceCommand(Guid workspaceId, Guid memberId)
        {
            WorkspaceId = workspaceId;
            MemberId = memberId;
        }
    }
}