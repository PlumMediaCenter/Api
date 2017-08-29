using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlumMediaCenter.Attributues
{
    public class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            if (exception == null)
            {
                // should never happen
                return;
            }
            exception = exception.GetBaseException();
            var responseObj = new
            {
                message = exception.Message,
                stacktrace = exception.ToString().Split('\n')
            };

            context.HttpContext.Response.StatusCode = 500;

            // Other exception types you want to handle ...

            context.Result = new ObjectResult(responseObj);
        }
    }
}