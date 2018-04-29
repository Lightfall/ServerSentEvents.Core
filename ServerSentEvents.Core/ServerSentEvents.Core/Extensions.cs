using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerSentEvents.Core
{
    public static class Extensions
    {
        public static IServiceCollection AddServerSentEvents(this IServiceCollection services, Func<HttpContext, bool> isSubscription, Func<HttpContext, string> getTopicName)
        {
            services.AddSingleton<ServerSentEventsService>(new ServerSentEventsService(isSubscription, getTopicName));
            return services;
        }


        public static IApplicationBuilder UseServerSentEvents(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ServerSentEventsMiddleware>();
        }
    }
}
