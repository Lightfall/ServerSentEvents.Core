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
        /// <summary>
        /// Register SSE Service as Singleton
        /// </summary>
        /// <param name="services"></param>
        /// <param name="isSubscription">Predicate for Request is a SSE registration</param>
        /// <param name="getTopicName">Get TopicName funcion</param>
        /// <returns></returns>
        public static IServiceCollection AddServerSentEvents(this IServiceCollection services, Func<HttpContext, bool> isSubscription, Func<HttpContext, string> getTopicName)
        {
            services.AddSingleton<ServerSentEventsService>(new ServerSentEventsService(isSubscription, getTopicName));
            return services;
        }

        /// <summary>
        /// Register SSE Middleware
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseServerSentEvents(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ServerSentEventsMiddleware>();
        }
    }
}
