using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetColumnActivities;

public class GetColumnActivitiesQueryValidator : AbstractValidator<GetColumnActivitiesQuery>
{
    public GetColumnActivitiesQueryValidator()
    {
        RuleFor(q => q.ColumnId).NotEmpty();
        RuleFor(q => q.PageIndex).InclusiveBetween(1, 1000000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
