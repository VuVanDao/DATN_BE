using FluentValidation;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardActivities;

public class GetCardActivitiesQueryValidator : AbstractValidator<GetCardActivitiesQuery>
{
    public GetCardActivitiesQueryValidator()
    {
        RuleFor(q => q.CardId).NotEmpty();
        RuleFor(q => q.PageIndex).InclusiveBetween(1, 1000000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
