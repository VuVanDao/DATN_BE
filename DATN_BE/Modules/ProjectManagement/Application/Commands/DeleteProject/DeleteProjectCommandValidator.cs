using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteProject
{
    public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
    {
        public DeleteProjectCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Mã dự án (ProjectId) không hợp lệ.");
        }
    }
}
