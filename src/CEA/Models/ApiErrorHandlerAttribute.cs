using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace CEA.Models
{
    public class ApiErrorHandlerAttribute : ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                errorMessage = context.Exception.Message
            }));
            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }
    }
}