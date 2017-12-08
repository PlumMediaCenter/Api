using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using GraphQL.Http;
using GraphQL.Types;
using GraphQL;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using PlumMediaCenter.Graphql;
using PlumMediaCenter.Business;
using Newtonsoft.Json;
using System.Linq;

namespace PlumMediaCenter.Middlewares
{

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class GraphQlMiddlewareExtensions
    {
        public static IApplicationBuilder UseGraphQl(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GraphQlMiddleware>();
        }
    }

    public class GraphQlMiddleware
    {
        private readonly RequestDelegate _next;
        public GraphQlMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private async Task<GraphQLArguments> GetArgs(HttpContext httpContext)
        {
            var args = new GraphQLArguments();
            if (httpContext.Request.Method.ToUpper() != "GET")
            {
                using (var sr = new StreamReader(httpContext.Request.Body))
                {
                    var bodyString = await sr.ReadToEndAsync();
                    //if the request is graphql mime type, the body is only the query portion
                    if (httpContext.Request.ContentType.ToLower() == "application/graphql")
                    {
                        args.Query = bodyString;
                    }
                    else
                    {
                        var bodyObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(bodyString);
                        args.Query = (string)bodyObj["query"];
                        args.Variables = bodyObj.ContainsKey("variables") ? (JsonConvert.SerializeObject(bodyObj["variables"])).ToInputs() : null;
                    }
                }
            }

            //if there are querystring variables, use those and override the body 
            if (httpContext.Request.Query.ContainsKey("query"))
            {
                args.Query = httpContext.Request.Query["query"];
            }
            if (httpContext.Request.Query.ContainsKey("variables"))
            {
                args.Variables = httpContext.Request.Query["variables"].ToString().ToInputs();
            }
            return args;
        }

        private class GraphQLArguments
        {
            public string Query;
            public Inputs Variables;
        }
        public async Task Invoke(HttpContext httpContext)
        {
            var sent = false;
            try
            {
                if (httpContext.Request.Path.StartsWithSegments("/api/graphql"))
                {
                    var args = await GetArgs(httpContext);

                    if (!String.IsNullOrWhiteSpace(args.Query))
                    {
                        var manager = new Manager(AppSettings.BaseUrlStatic);
                        var schema = new Schema { Query = new BaseQuery() };
                        var result = await new DocumentExecuter()
                            .ExecuteAsync(options =>
                            {
                                options.Schema = schema;
                                options.Query = args.Query;
                                options.Inputs = args.Variables;
                                options.UserContext = manager;
                            }).ConfigureAwait(false);


                        CheckForErrors(result);

                        await WriteResult(httpContext, result);

                        sent = true;
                    }
                }
            }
            catch (Exception e)
            {
                await WriteErrorResult(httpContext, e);
            }
            if (!sent)
            {
                await _next(httpContext);
            }
        }

        private async Task WriteResult(HttpContext httpContext, ExecutionResult result)
        {
            var json = new DocumentWriter(indent: true).Write(result);

            httpContext.Response.StatusCode = 200;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(json);
        }

        private async Task WriteErrorResult(HttpContext httpContext, Exception e)
        {
            var responseError = Utility.GetCommonException(e);
            var json = JsonConvert.SerializeObject(responseError);

            httpContext.Response.StatusCode = 500;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(json);
        }

        private void CheckForErrors(ExecutionResult result)
        {
            if (result.Errors?.Count > 0)
            {
                var errors = new List<Exception>();
                foreach (var error in result.Errors)
                {
                    var ex = new Exception(error.Message);
                    if (error.InnerException != null)
                    {
                        ex = new Exception(error.Message, error.InnerException);
                    }
                    errors.Add(ex);
                }

                throw new AggregateException(errors);
            }
        }
    }
}
