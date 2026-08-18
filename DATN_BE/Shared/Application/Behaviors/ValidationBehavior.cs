using FluentValidation;
using MediatR;

namespace DATN_BE.Shared.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        /*
         * Middleware nội bộ MediatR
         * Tác dụng: Khi có 1 Command/Query gửi đến MediatR, class này sẽ tự động tìm kiếm Validator của Command đó, chạy kiểm tra dữ liệu trước. 
         * Nếu có lỗi, nó sẽ tự động ném ra ValidationException và chặn không cho Handler chạy.
         */
        private readonly IEnumerable<IValidator<TRequest>> _validator;
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validator = validators;
        }
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validator.Any())
            {
                return await next();
            }
            var context = new ValidationContext<TRequest>(request);
            var validationResult = await Task.WhenAll(_validator.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResult.SelectMany(r => r.Errors).Where(f => f != null).ToList();
            if (failures.Count() != 0)
            {
                throw new ValidationException(failures);
            }
            return await next();
        }
    }
}
