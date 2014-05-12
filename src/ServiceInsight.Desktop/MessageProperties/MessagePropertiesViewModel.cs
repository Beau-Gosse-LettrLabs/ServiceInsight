﻿namespace Particular.ServiceInsight.Desktop.MessageProperties
{
    using System.ComponentModel;
    using Caliburn.PresentationFramework.Screens;
    using ExceptionHandler;

    public class MessagePropertiesViewModel : Screen, IMessagePropertiesViewModel
    {
        readonly IClipboard clipboard;

        public MessagePropertiesViewModel(
            IErrorHeaderViewModel error,
            IGeneralHeaderViewModel general,
            ISagaHeaderViewModel saga,
            IPerformanceHeaderViewModel performance,
            IGatewayHeaderViewModel gateway,
            IClipboard clipboard)
        {
            this.clipboard = clipboard;
            Saga = saga;
            Performance = performance;
            Gateway = gateway;
            Errors = error;
            General = general;
        }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public IGeneralHeaderViewModel General { get; private set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public IPerformanceHeaderViewModel Performance { get; private set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public IErrorHeaderViewModel Errors { get; private set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public IGatewayHeaderViewModel Gateway { get; private set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public ISagaHeaderViewModel Saga { get; private set; }

        public void CopyPropertyValue(object value)
        {
            clipboard.CopyTo(value.ToString());
        }

    }
}