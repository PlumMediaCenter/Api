using System;
using System.Collections.Generic;
using System.Linq;
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
            var stacktrace = exception.ToString().Split('\n');
            var sourceStacktrace = stacktrace.Where(x => x.Contains(":line ")).ToList();
            var baseException = exception.GetBaseException();

            var responseObj = new
            {
                message = baseException.Message,
                stacktrace = stacktrace,
                sourceStacktrace = sourceStacktrace
            };

            context.HttpContext.Response.StatusCode = 500;

            // Other exception types you want to handle ...

            context.Result = new ObjectResult(responseObj);
        }
    }
}