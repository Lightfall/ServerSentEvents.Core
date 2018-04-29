using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ServerSentEvents.Core
{
    public sealed class ServerSentEventsService
    {

        #region Delegates
        public delegate void SessionCreatedEventHandler(ServerSentEventsService sender, SessionCreatedEventArgs e);
        public delegate void SessionEndedEventHandler(ServerSentEventsService sender, SessionEndedEventArgs e);
        public delegate void MessagePushedEventHandler(ServerSentEventsService sender, MessagePushedEventArgs e);

        public delegate void SessionCreatingEventHandler(ServerSentEventsService sender, SessionCreatingEventArgs e);
        public delegate void SessionEndingEventHandler(ServerSentEventsService sender, SessionEndingEventArgs e);
        public delegate void MessagePushingEventHandler(ServerSentEventsService sender, MessagePushingEventArgs e);
        #endregion //Delegates

        #region Events
        public event SessionCreatedEventHandler OnSessionCreated;
        public event SessionEndedEventHandler OnSessionEnded;
        public event MessagePushedEventHandler OnMessagePushed;

        public event SessionCreatingEventHandler OnSessionCreating;
        public event SessionEndingEventHandler OnSessionEnding;
        public event MessagePushingEventHandler OnMessagePushing;
        #endregion //Events



        private readonly ConcurrentDictionary<string, HttpContext> _sessions;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HttpContext>> _topics;
        private readonly Func<HttpContext, bool> _isSubscription;
        private readonly Func<HttpContext, string> _getTopicName;

        public ServerSentEventsService(Func<HttpContext, bool> isSubscription, Func<HttpContext, string> getTopicName)
        {
            this._isSubscription = isSubscription;
            this._topics = new ConcurrentDictionary<string, ConcurrentDictionary<string, HttpContext>>();
            this._getTopicName = getTopicName;
            this._sessions = new ConcurrentDictionary<string, HttpContext>();
        }

        internal bool HandleRequest(HttpContext context)
        {
            if (!this._isSubscription(context))
            {
                return false;
            }

            string topicName = _getTopicName(context);
            string sessionId = context.Connection.Id;
            if (_topics.TryGetValue(topicName, out ConcurrentDictionary<string, HttpContext> followers))
            {
                followers.TryAdd(context.Connection.Id,context);
            }
            else
            {
                followers = new ConcurrentDictionary<string, HttpContext>();
                followers.TryAdd(context.Connection.Id, context);
                _topics.TryAdd(topicName, followers);
            }

            this.CreateSession(sessionId, context);
            context.RequestAborted.WaitHandle.WaitOne();
            this.RemoveSession(sessionId);
            return true;

        }

        private void CreateSession(string id, HttpContext context)
        {
            var ea = new SessionCreatingEventArgs(id, context);
            OnSessionCreating?.Invoke(this, ea);
            if (ea.Cancel)
            {
                return;
            }


            context.Response.Headers.Add("Content-Type", "text/event-stream");
            context.Response.WriteAsync("data: connected\n\n");
            context.Response.Body.Flush();
            _sessions.AddOrUpdate(id, context, (key, oldContext) =>
            {
                oldContext.Abort();
                return context;
            });
            OnSessionCreated?.Invoke(this, new SessionCreatedEventArgs(id, context));
        }

        private void RemoveSession(string id)
        {
            try
            {
                var ea = new SessionEndingEventArgs(id);
                OnSessionEnding?.Invoke(this, ea);
            }
            finally
            {
                _sessions.Remove(id, out HttpContext value);
                foreach (var topic in _topics)
                {
                    topic.Value.Remove(id, out HttpContext x);
                }
                OnSessionEnded?.Invoke(this, new SessionEndedEventArgs(id));
            }

        }

        public void PushMesage(string message, string topicName = "")
        {

            var ea = new MessagePushingEventArgs(message, topicName);
            OnMessagePushing?.Invoke(this, ea);
            if (ea.Cancel)
            {
                return;
            }



            List<string> itemsToRemove = new List<string>();

            if (string.IsNullOrWhiteSpace(topicName))
            {
                foreach (var session in _sessions)
                {
                    try
                    {
                        if (session.Key != session.Value.Connection.Id)
                        {
                            Debugger.Break();
                        }
                        session.Value.Response.WriteAsync($"data: {message}\n\n");
                        session.Value.Response.Body.Flush();
                    }
                    catch (ObjectDisposedException)
                    {
                        itemsToRemove.Add(session.Key);
                    }
                }
            }
            else
            {
                if (_topics.TryGetValue(topicName, out ConcurrentDictionary<string, HttpContext> followers))
                {
                    List<string> itemsToRemovex = new List<string>();
                    foreach (var follower in followers)
                    {

                        try
                        {
                            follower.Value.Response.WriteAsync($"data: {message}\n\n");
                            follower.Value.Response.Body.Flush();
                        }
                        catch (ObjectDisposedException)
                        {
                            itemsToRemove.Add(follower.Value.Connection.Id);
                            itemsToRemovex.Add(follower.Key);
                        }

                    }
                    itemsToRemove.ForEach(ids =>
                    {
                        followers.Remove(ids, out HttpContext context);
                    });
                }
            }

            itemsToRemove.ForEach(ids =>
            {
                
                _sessions.Remove(ids, out HttpContext context);
                if (context != null)
                {
                    context.Abort(); 
                }
            });
            OnMessagePushed?.Invoke(this, new MessagePushedEventArgs(message, topicName));
        }
    }


    public class SessionCreatedEventArgs
    {
        public SessionCreatedEventArgs(string sessionId, HttpContext httpContext)
        {
            this.SessionId = sessionId;
            this.HttpContext = httpContext;
        }
        public string SessionId { get; private set; }
        public HttpContext HttpContext { get; private set; }
    }

    public class SessionEndedEventArgs
    {
        public SessionEndedEventArgs(string sessionId)
        {
            this.SessionId = sessionId;
        }
        public string SessionId { get; private set; }
    }

    public class MessagePushedEventArgs
    {
        public MessagePushedEventArgs(string message, string topicName)
        {
            this.Message = message;
            this.TopicName = topicName;
        }
        public string Message { get; private set; }
        public string TopicName { get; private set; }
    }

    public class SessionCreatingEventArgs
    {
        public SessionCreatingEventArgs(string sessionId, HttpContext httpContext)
        {
            this.SessionId = sessionId;
            this.HttpContext = httpContext;
        }
        public string SessionId { get; private set; }
        public HttpContext HttpContext { get; private set; }
        public bool Cancel { get; set; }
    }

    public class SessionEndingEventArgs
    {
        public SessionEndingEventArgs(string sessionId)
        {
            this.SessionId = sessionId;
        }
        public string SessionId { get; private set; }

    }

    public class MessagePushingEventArgs
    {
        public MessagePushingEventArgs(string message, string topicName)
        {
            this.Message = message;
            this.TopicName = topicName;
        }
        public string Message { get; private set; }
        public string TopicName { get; private set; }
        public bool Cancel { get; set; }
    }
}
