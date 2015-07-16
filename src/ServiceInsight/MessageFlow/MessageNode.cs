﻿namespace Particular.ServiceInsight.Desktop.MessageFlow
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Framework;
    using Mindscape.WpfDiagramming;
    using Models;
    using ReactiveUI;

    [DebuggerDisplay("Type={Message.FriendlyMessageType}, Id={Message.Id}")]
    public class MessageNode : DiagramNode
    {
        int heightNoEndpoints = 56;
        const int endpointsHeight = 25;

        public MessageNode(MessageFlowViewModel owner, StoredMessage message)
        {
            IsResizable = false;
            Owner = owner;
            Data = message;
            ExceptionMessage = message.GetHeaderByKey(MessageHeaderKeys.ExceptionType);
            SagaType = ProcessSagaType(message);

            heightNoEndpoints += HasSaga ? 10 : 0;
            Bounds = new Rect(0, 0, 100, heightNoEndpoints);

            CopyConversationIDCommand = owner.CopyConversationIDCommand;
            CopyMessageURICommand = owner.CopyMessageURICommand;
            SearchByMessageIDCommand = owner.SearchByMessageIDCommand;
            RetryMessageCommand = owner.RetryMessageCommand;

            message.ObservableForProperty(m => m.Status).Subscribe(_ =>
            {
                OnPropertyChanged("HasFailed");
                OnPropertyChanged("HasRetried");
            });
        }

        string ProcessSagaType(StoredMessage message)
        {
            if (message.Sagas == null) return string.Empty;

            var originatingSaga = message.Sagas.FirstOrDefault();
            if (originatingSaga == null) return string.Empty;

            return TypeHumanizer.ToName(originatingSaga.SagaType);
        }

        public StoredMessage Message
        {
            get { return Data as StoredMessage; }
        }

        public MessageFlowViewModel Owner { get; private set; }

        public ICommand CopyConversationIDCommand { get; private set; }
        public ICommand CopyMessageURICommand { get; private set; }
        public ICommand SearchByMessageIDCommand { get; private set; }
        public ICommand RetryMessageCommand { get; private set; }

        public void ShowBody()
        {
            Owner.ShowMessageBody();
        }

        public void ShowException()
        {
            Owner.ShowException(new ExceptionDetails(Message));
        }

        public bool ShowEndpoints { get; set; }

        public void OnShowEndpointsChanged()
        {
            Bounds = new Rect(Bounds.Location, new Size(Bounds.Width, heightNoEndpoints + (ShowEndpoints ? endpointsHeight : 0)));
        }

        public bool ShowExceptionInfo
        {
            get { return !string.IsNullOrEmpty(ExceptionMessage); }
        }

        public string NSBVersion
        {
            get { return Message.GetHeaderByKey(MessageHeaderKeys.Version); }
        }

        public bool IsPublished
        {
            get { return Message.MessageIntent == MessageIntent.Publish; }
        }

        public bool IsEventMessage
        {
            get { return IsPublished && !IsTimeout; }
        }

        public bool IsCommandMessage
        {
            get { return !IsPublished && !IsTimeout; }
        }

        public bool IsTimeoutMessage
        {
            get { return IsTimeout; }
        }

        public bool IsSagaInitiated
        {
            get
            {
                return string.IsNullOrEmpty(Message.GetHeaderByKey(MessageHeaderKeys.SagaId)) && !string.IsNullOrEmpty(Message.GetHeaderByKey(MessageHeaderKeys.OriginatedSagaId));
            }
        }

        public bool IsSagaCompleted
        {
            get
            {
                var status = Message.InvokedSagas == null ? null : Message.InvokedSagas.FirstOrDefault();
                return status != null && status.ChangeStatus == "Completed";
            }
        }

        public bool IsTimeout
        {
            get
            {
                var isTimeoutString = Message.GetHeaderByKey(MessageHeaderKeys.IsSagaTimeout);
                return !string.IsNullOrEmpty(isTimeoutString) && bool.Parse(isTimeoutString);
            }
        }

        public DateTime? TimeSent
        {
            get
            {
                var timeString = Message.GetHeaderByKey(MessageHeaderKeys.TimeSent);
                if (string.IsNullOrEmpty(timeString))
                    return null;
                return DateTime.ParseExact(timeString, HeaderInfo.MessageDateFormat, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public bool HasFailed
        {
            get
            {
                return Message.Status == MessageStatus.Failed ||
                       Message.Status == MessageStatus.RepeatedFailure || Message.Status == MessageStatus.ArchivedFailure;
            }
        }

        public bool HasRetried
        {
            get
            {
                return Message.Status == MessageStatus.RetryIssued;
            }
        }

        public string SagaType { get; private set; }

        public bool HasSaga
        {
            get
            {
                return !string.IsNullOrEmpty(SagaType);
            }
        }

        public string ExceptionMessage { get; set; }

        public bool IsFocused { get; set; }
    }
}