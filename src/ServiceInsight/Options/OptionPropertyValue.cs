﻿namespace ServiceInsight.Options
{
    using System;
    using System.ComponentModel;
    using System.Reflection;

    public class OptionPropertyValue : IDataErrorInfo
    {
        PropertyInfo propertyInfo;
        object owner;
        DisplayNameAttribute displayName;
        DescriptionAttribute description;

        public OptionPropertyValue(PropertyInfo propertyInfo, object owner, PropertyInfo defaultPropertyInfo)
        {
            this.propertyInfo = propertyInfo;
            this.owner = owner;
            displayName = this.propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
            description = this.propertyInfo.GetCustomAttribute<DescriptionAttribute>();

            if (defaultPropertyInfo != null)
            {
                DefaultValue = (string)defaultPropertyInfo.GetValue(owner);
            }
        }

        public string Name => displayName != null ? displayName.DisplayName : propertyInfo.Name;

        public string Description => description == null ? string.Empty : description.Description;

        public string DefaultValue { get; set; }

        public Type PropertyType => propertyInfo.PropertyType;

        public object Value
        {
            get { return propertyInfo.GetValue(owner, null); }
            set { TrySetValue(value); }
        }

        void TrySetValue(object value)
        {
            var convertedValue = Convert.ChangeType(value, PropertyType);
            propertyInfo.SetValue(owner, convertedValue, null);
        }

        public string this[string columnName] => string.Empty;

        public string Error { get; }
    }
}