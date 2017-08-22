using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace PlumMediaCenter.Middleware
{
    public class RequestMiddleware
    {
        private static LocalDataStoreSlot RequestNamedDataSlot = Thread.AllocateNamedDataSlot("request");

        private readonly RequestDelegate _next;

        public RequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Thread.SetData(RequestNamedDataSlot, context);
            await _next.Invoke(context);
        }

        /// <summary>
        /// Get the current request
        /// </summary>
        /// <returns></returns>
        public static HttpContext CurrentHttpContext
        {
            get
            {
                return (HttpContext)Thread.GetData(RequestNamedDataSlot);
            }
        }
    }

    public static class MyMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestMiddleware>();
        }
    }
}