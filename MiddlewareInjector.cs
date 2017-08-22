using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace PlumMediaCenter
{
    public static class MiddlewareInjectorExtensions
    {
        public static IApplicationBuilder UseMiddlewareInjector(this IApplicationBuilder builder, MiddlewareInjectorOptions options)
        {
            var result = builder.UseMiddleware<MiddlewareInjectorMiddleware>(builder.New(), options);

            //register the sources during startup
            options.InjectMiddleware(app =>
            {
                Startup.RegisterSources(app);
            });

            return result;
        }
    }

    public class MiddlewareInjectorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _builder;
        private readonly MiddlewareInjectorOptions _options;
        private RequestDelegate _subPipeline;

        public MiddlewareInjectorMiddleware(RequestDelegate next, IApplicationBuilder builder, MiddlewareInjectorOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Invoke(HttpContext httpContext)
        {
            var injector = _options.GetInjector();
            if (injector != null)
            {
                var builder = _builder.New();
                injector(builder);
                builder.Run(_next);
                _subPipeline = builder.Build();
            }

            if (_subPipeline != null)
            {
                return _subPipeline(httpContext);
            }

            return _next(httpContext);
        }
    }

    public class MiddlewareInjectorOptions
    {
        private Action<IApplicationBuilder> _injector;

        public void InjectMiddleware(Action<IApplicationBuilder> builder)
        {
            Interlocked.Exchange(ref _injector, builder);
        }

        internal Action<IApplicationBuilder> GetInjector()
        {
            return Interlocked.Exchange(ref _injector, null);
        }
    }
}
