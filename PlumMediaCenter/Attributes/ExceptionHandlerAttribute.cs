using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PlumMediaCenter.Business;

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
            var responseObj = Utility.GetCommonException(exception);

            context.HttpContext.Response.StatusCode = 500;

            // Other exception types you want to handle ...

            context.Result = new ObjectResult(responseObj);
        }

    }
}