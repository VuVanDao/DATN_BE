using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Commands.RemoveMemberFromProject
{
    public class RemoveMemberFromProjectCommandValidator : AbstractValidator<RemoveMemberFromWorkspaceCommand>
    {
        public RemoveMemberFromProjectCommandValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty().WithMessage("Mã Workspace không được để trống");

            RuleFor(x => x.MemberId)
                .NotEmpty().WithMessage("Mã thành viên không được để trống");
        }
    }
}