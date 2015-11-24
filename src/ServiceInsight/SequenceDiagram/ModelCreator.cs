namespace ServiceInsight.SequenceDiagram
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Particular.ServiceInsight.Desktop.Framework;
    using Particular.ServiceInsight.Desktop.Models;
    using ServiceInsight.SequenceDiagram.Diagram;

    public class ModelCreator
    {
        readonly List<ReceivedMessage> messages;

        List<EndpointItem> endpoints = new List<EndpointItem>();
        List<Handler> handlers = new List<Handler>();
        List<MessageProcessingRoute> processingRoutes = new List<MessageProcessingRoute>();
        IMessageCommandContainer container;

        public ModelCreator(List<ReceivedMessage> messages, IMessageCommandContainer container)
        {
            this.messages = messages;
            this.container = container;

            Initialize();
        }

        public ReadOnlyCollection<EndpointItem> Endpoints
        {
            get { return endpoints.AsReadOnly(); }
        }

        public ReadOnlyCollection<Handler> Handlers
        {
            get { return handlers.AsReadOnly(); }
        }

        public ReadOnlyCollection<MessageProcessingRoute> Routes
        {
            get { return processingRoutes.AsReadOnly(); }
        }

        void Initialize()
        {
            var endpointRegistry = new EndpointRegistry();
            var handlerRegistry = new HandlerRegistry();

            var messageTrees = CreateMessageTrees(messages).ToArray();
            var messagesInOrder = messageTrees.SelectMany(x => x.Walk()).ToArray();

            // NOTE: All sending endpoints are created first to ensure version info is retained
            foreach (var message in messagesInOrder)
            {
                endpointRegistry.Register(CreateSendingEndpoint(message));
            }

            foreach (var message in messagesInOrder)
            {
                endpointRegistry.Register(CreateProcessingEndpoint(message));
            }

            foreach (var message in messagesInOrder)
            {
                var sendingEndpoint = endpointRegistry.Get(CreateSendingEndpoint(message));
                if (!endpoints.Contains(sendingEndpoint))
                {
                    endpoints.Add(sendingEndpoint);
                }

                var processingEndpoint = endpointRegistry.Get(CreateProcessingEndpoint(message));
                if (!endpoints.Contains(processingEndpoint))
                {
                    endpoints.Add(processingEndpoint);
                }

                Handler sendingHandler;
                Handler processingHandler;

                if (handlerRegistry.TryRegisterHandler(CreateSendingHandler(message, sendingEndpoint), out sendingHandler))
                {
                    handlers.Add(sendingHandler);
                    sendingEndpoint.Handlers.Add(sendingHandler);
                }

                if (handlerRegistry.TryRegisterHandler(CreateProcessingHandler(message, processingEndpoint), out processingHandler))
                {
                    handlers.Add(processingHandler);
                    processingEndpoint.Handlers.Add(processingHandler);
                }
                else
                {
                    UpdateProcessingHandler(processingHandler, message, processingEndpoint);
                }

                var arrow = CreateArrow(message);
                arrow.Receiving = new Endpoint { Name = processingEndpoint.Name, Host = processingEndpoint.Host };
                arrow.Sending = new Endpoint { Name = sendingEndpoint.Name, Host = sendingEndpoint.Host };
                arrow.ToHandler = processingHandler;
                arrow.FromHandler = sendingHandler;

                processingRoutes.Add(CreateRoute(arrow, processingHandler));
                processingHandler.In = arrow;

                sendingHandler.Out = sendingHandler.Out.Concat(new[] { arrow }).OrderBy(a => a).ToList();
            }

            handlers.Sort((x, y) => DateTime.Compare(x.HandledAt.GetValueOrDefault(), y.HandledAt.GetValueOrDefault()));
        }

        MessageProcessingRoute CreateRoute(Arrow arrow, Handler processingHandler)
        {
            return new MessageProcessingRoute(arrow, processingHandler);
        }

        IEnumerable<MessageTreeNode> CreateMessageTrees(IEnumerable<ReceivedMessage> recievedMessages)
        {
            var nodes = recievedMessages.Select(x => new MessageTreeNode(x)).ToList();
            var resolved = new HashSet<MessageTreeNode>();

            var index = nodes.ToLookup(x => x.Id);

            foreach (var node in nodes)
            {
                var parent = index[node.Parent].FirstOrDefault();
                if (parent != null)
                {
                    parent.AddChild(node);
                    resolved.Add(node);
                }
            }

            return nodes.Except(resolved);
        }

        EndpointItem CreateProcessingEndpoint(ReceivedMessage m)
        {
            return new EndpointItem(m.receiving_endpoint.name, m.receiving_endpoint.host, m.receiving_endpoint.host_id, m.sending_endpoint.Equals(m.receiving_endpoint) ? GetHeaderByKey(m.headers, MessageHeaderKeys.Version, null) : null);
        }

        EndpointItem CreateSendingEndpoint(ReceivedMessage m)
        {
            return new EndpointItem(m.sending_endpoint.name, m.sending_endpoint.host, m.sending_endpoint.host_id, GetHeaderByKey(m.headers, MessageHeaderKeys.Version, null));
        }

        Handler CreateSendingHandler(ReceivedMessage message, EndpointItem sendingEndpoint)
        {
            return new Handler(GetHeaderByKey(message.headers, MessageHeaderKeys.RelatedTo, "First"), container)
            {
                HandledAt = message.time_sent,
                State = HandlerState.Success,
                Endpoint = sendingEndpoint
            };
        }

        Handler CreateProcessingHandler(ReceivedMessage message, EndpointItem processingEndpoint)
        {
            var handler = new Handler(message.message_id, container)
            {
                HandledAt = message.processed_at,
                Endpoint = processingEndpoint
            };

            UpdateProcessingHandler(handler, message, processingEndpoint);

            return handler;
        }

        void UpdateProcessingHandler(Handler processingHandler, ReceivedMessage message, EndpointItem processingEndpoint)
        {
            processingHandler.ProcessingTime = message.processing_time;
            processingHandler.Name = TypeHumanizer.ToName(message.message_type);

            if (message.invoked_sagas != null && message.invoked_sagas.Count > 0)
            {
                processingHandler.PartOfSaga = string.Join(", ", Array.ConvertAll(message.invoked_sagas.ToArray(), x => TypeHumanizer.ToName(x.saga_type)));
            }

            if (message.status == MessageStatus.ArchivedFailure || message.status == MessageStatus.Failed || message.status == MessageStatus.RepeatedFailure)
            {
                processingHandler.State = HandlerState.Fail;
            }
            else
            {
                processingHandler.State = HandlerState.Success;
            }
        }

        Arrow CreateArrow(ReceivedMessage message)
        {
            var arrow = new Arrow(message.message_id, message.conversation_id, message.status, message.id, message.time_sent, message.headers, container)
            {
                Name = TypeHumanizer.ToName(message.message_type),
                SentTime = message.time_sent,
            };

            if (message.message_intent == MessageIntent.Publish)
            {
                arrow.Type = ArrowType.Event;
            }
            else
            {
                var isTimeoutString = GetHeaderByKey(message.headers, MessageHeaderKeys.IsSagaTimeout);
                var isTimeout = !string.IsNullOrEmpty(isTimeoutString) && bool.Parse(isTimeoutString);
                if (isTimeout)
                {
                    arrow.Type = ArrowType.Timeout;
                }
                else if (Equals(message.receiving_endpoint, message.sending_endpoint))
                {
                    arrow.Type = ArrowType.Local;
                }
                else
                {
                    arrow.Type = ArrowType.Command;
                }
            }

            return arrow;
        }

        static string GetHeaderByKey(IEnumerable<Header> headers, string key, string defaultValue = "")
        {
            //NOTE: Some keys start with NServiceBus, some don't
            var keyWithPrefix = "NServiceBus." + key;
            var pair = headers.FirstOrDefault(x => x.key.Equals(key, StringComparison.InvariantCultureIgnoreCase) ||
                                                   x.key.Equals(keyWithPrefix, StringComparison.InvariantCultureIgnoreCase));
            return pair == null ? defaultValue : pair.value;
        }

        class EndpointRegistry
        {
            IDictionary<Tuple<string, string, string>, List<EndpointItem>> store = new Dictionary<Tuple<string, string, string>, List<EndpointItem>>();

            public void Register(EndpointItem item)
            {
                List<EndpointItem> items;
                var key = MakeKey(item);
                if (!store.TryGetValue(key, out items))
                {
                    items = new List<EndpointItem>();
                    store[key] = items;
                }

                var existing = items.FirstOrDefault(x => x.Version == item.Version);
                if (existing == null)
                {
                    // Only add null if we haven't seen anything else
                    if (item.Version != null || !items.Any())
                    {
                        items.Add(item);
                    }
                }
            }

            public EndpointItem Get(EndpointItem prototype)
            {
                var key = MakeKey(prototype);

                var candidate = store[key].Where(x => x.Version != null).FirstOrDefault(x => x.Version == prototype.Version);

                if (candidate != null)
                {
                    return candidate;
                }

                return store[key].FirstOrDefault(x => x.Version == prototype.Version)
                       ?? store[key].FirstOrDefault();
            }

            Tuple<string, string, string> MakeKey(EndpointItem item)
            {
                return Tuple.Create(item.FullName, item.Host, item.HostId);
            }
        }

        class HandlerRegistry
        {
            IDictionary<Tuple<string, EndpointItem>, Handler> handlersLookup = new Dictionary<Tuple<string, EndpointItem>, Handler>();

            public bool TryRegisterHandler(Handler newHandler, out Handler handler)
            {
                Handler existingHandler;
                var key = Tuple.Create(newHandler.ID, newHandler.Endpoint);
                if (handlersLookup.TryGetValue(key, out existingHandler))
                {
                    handler = existingHandler;
                    return false;
                }

                handlersLookup.Add(key, newHandler);

                handler = newHandler;
                return true;
            }
        }

        class MessageTreeNode
        {
            List<MessageTreeNode> children = new List<MessageTreeNode>();

            public MessageTreeNode(ReceivedMessage msg)
            {
                this.Message = msg;
                Parent = GetHeaderByKey(msg.headers, MessageHeaderKeys.RelatedTo, null);
            }

            public string Id
            {
                get { return Message.message_id; }
            }

            public string Parent { get; set; }

            public ReceivedMessage Message { get; set; }

            public IEnumerable<MessageTreeNode> Children
            {
                get { return children; }
            }

            public void AddChild(MessageTreeNode childNode)
            {
                children.Add(childNode);
            }

            public IEnumerable<ReceivedMessage> Walk()
            {
                yield return Message;
                foreach (var child in Children.OrderBy(x => x.Message.processed_at))
                {
                    foreach (var walked in child.Walk())
                    {
                        yield return walked;
                    }
                }
            }
        }
    }
}