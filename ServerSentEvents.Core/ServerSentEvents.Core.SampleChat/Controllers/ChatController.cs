using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerSentEvents.Core.SampleChat.Controllers
{
    [Route("api/[controller]")]
    public class ChatController : Controller
    {
        ServerSentEventsService serverSentEventsService;
        public ChatController(ServerSentEventsService serverSentEventsService)
        {
            this.serverSentEventsService = serverSentEventsService;
        }
        [HttpPost("{roomname}")]
        public void SendMessage(string roomname, [FromBody] Message value)
        {
            for (int i = 0; i < value.count; i++)
            {
                serverSentEventsService.PushMesage(value.message, roomname);
                Thread.Sleep(10);
            }
        }
    }

    public class Message
    {
        public string message { get; set; }
        public int count { get; set; }
    }
}
