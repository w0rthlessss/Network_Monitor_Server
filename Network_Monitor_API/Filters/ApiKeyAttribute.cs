using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Network_Monitor_API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IActionFilter
    {
        private const string HeaderName = "X-Api-Key";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expectedKey = config["InternalApi:ApiKey"];

            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || providedKey != expectedKey)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
