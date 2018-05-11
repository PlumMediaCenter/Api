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

            var responseObj = new PrettyError(exception);

            context.HttpContext.Response.StatusCode = 500;

            // Other exception types you want to handle ...

            context.Result = new ObjectResult(responseObj);
        }
    }

    public class PrettyError
    {
        public PrettyError(Exception e)
        {
            var stacktrace = e.ToString().Split('\n');
            this.stackTrace = stacktrace.Where(x => x.Contains(":line ")).ToList();

            this.message = e.GetBaseException().Message;
        }
        public string message;
        public IEnumerable<string> stackTrace;
    }
}