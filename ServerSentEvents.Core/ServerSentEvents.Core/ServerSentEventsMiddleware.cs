using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServerSentEvents.Core
{
    public class ServerSentEventsMiddleware
    {

        private readonly RequestDelegate _next;
        public ServerSentEventsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context, ServerSentEventsService service)
        {
            bool requestHandled = service.HandleRequest(context);
            if (requestHandled)
            {
                return context.Response.WriteAsync("");
            }
            return _next.Invoke(context);
        }
    }
}
