using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UniProject.API.Common
{
    /// <summary>
    /// Chạy FluentValidation cho mọi tham số action có validator được đăng ký (IValidator&lt;T&gt;).
    /// Dữ liệu không hợp lệ → ValidationException → GlobalExceptionHandler trả 400 kèm danh sách lỗi.
    /// </summary>
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values.Where(a => a != null))
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    throw new UniProject.Share.Infrastructure.Exceptions.ValidationException(result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
                }
            }

            await next();
        }
    }
}
