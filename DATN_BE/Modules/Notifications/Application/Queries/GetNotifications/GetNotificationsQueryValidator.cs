using FluentValidation;

namespace DATN_BE.Modules.Notifications.Application.Queries.GetNotifications
{
    public class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
    {
        public GetNotificationsQueryValidator()
        {
            RuleFor(x => x.PageIndex)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Số trang phải lớn hơn hoặc bằng 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50)
                .WithMessage("Kích thước trang phải nằm trong khoảng từ 1 đến 50.");
        }
    }
}
