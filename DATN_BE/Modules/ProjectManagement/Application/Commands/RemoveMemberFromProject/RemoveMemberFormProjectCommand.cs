using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.RemoveMemberFromProject
{
    public class RemoveMemberFormProjectCommand : IRequest<bool>
    {
        public Guid ProjectId { get; set; }
        public Guid MerberId { get; set; }
    }
}