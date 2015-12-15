﻿namespace ServiceInsight.Framework.Rx
{
    using System;
    using System.Linq.Expressions;
    using Caliburn.Micro;

    public class RxScreen : RxViewAware, IScreen, IChild
    {
        static readonly ILog Log = LogManager.GetLog(typeof(RxScreen));

        public RxScreen()
        {
            DisplayName = GetType().FullName;
        }

        public virtual object Parent { get; set; }

        public virtual string DisplayName { get; set; }

        public bool IsActive { get; private set; }

        public bool IsInitialized { get; private set; }

        public event EventHandler<ActivationEventArgs> Activated = delegate { };

        public event EventHandler<DeactivationEventArgs> AttemptingDeactivation = delegate { };

        public event EventHandler<DeactivationEventArgs> Deactivated = delegate { };

        void IActivate.Activate()
        {
            if (IsActive)
            {
                return;
            }

            var initialized = false;

            if (!IsInitialized)
            {
                IsInitialized = initialized = true;
                OnInitialize();
            }

            IsActive = true;
            Log.Info("Activating {0}.", this);
            OnActivate();

            Activated(this, new ActivationEventArgs
            {
                WasInitialized = initialized
            });
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnActivate()
        {
        }

        void IDeactivate.Deactivate(bool close)
        {
            if (IsActive || (IsInitialized && close))
            {
                AttemptingDeactivation(this, new DeactivationEventArgs
                {
                    WasClosed = close
                });

                IsActive = false;
                Log.Info("Deactivating {0}.", this);
                OnDeactivate(close);

                Deactivated(this, new DeactivationEventArgs
                {
                    WasClosed = close
                });

                if (close)
                {
                    Views.Clear();
                    Log.Info("Closed {0}.", this);
                }
            }
        }

        protected virtual void OnDeactivate(bool close)
        {
        }

        public virtual void CanClose(Action<bool> callback)
        {
            callback(true);
        }

        System.Action GetViewCloseAction(bool? dialogResult)
        {
            var conductor = Parent as IConductor;
            if (conductor != null)
            {
                return () => conductor.CloseItem(this);
            }

            foreach (var contextualView in Views.Values)
            {
                var viewType = contextualView.GetType();
#if WinRT
                var closeMethod = viewType.GetRuntimeMethod("Close", new Type[0]);
#else
                var closeMethod = viewType.GetMethod("Close");
#endif
                if (closeMethod != null)
                    return () =>
                    {
#if !SILVERLIGHT && !WinRT
                        var isClosed = false;
                        if (dialogResult != null)
                        {
                            var resultProperty = contextualView.GetType().GetProperty("DialogResult");
                            if (resultProperty != null)
                            {
                                resultProperty.SetValue(contextualView, dialogResult, null);
                                isClosed = true;
                            }
                        }

                        if (!isClosed)
                        {
                            closeMethod.Invoke(contextualView, null);
                        }
#else
                        closeMethod.Invoke(contextualView, null);
#endif
                    };

#if WinRT
                var isOpenProperty = viewType.GetRuntimeProperty("IsOpen");
#else
                var isOpenProperty = viewType.GetProperty("IsOpen");
#endif
                if (isOpenProperty != null)
                {
                    return () => isOpenProperty.SetValue(contextualView, false, null);
                }
            }

            return () => Log.Info("TryClose requires a parent IConductor or a view with a Close method or IsOpen property.");
        }

        public virtual void TryClose()
        {
            GetViewCloseAction(null).OnUIThread();
        }

        public virtual void TryClose(bool? dialogResult)
        {
            GetViewCloseAction(dialogResult).OnUIThread();
        }

        [PropertyChanged.DoNotNotify]
        [Obsolete("Use SuppressChangeNotifications() instead.", true)]
        bool INotifyPropertyChangedEx.IsNotifying
        {
            get
            {
                throw new NotSupportedException("Use SuppressChangeNotifications() instead.");
            }
            set
            {
                throw new NotSupportedException("Use SuppressChangeNotifications() instead.");
            }
        }

        public void NotifyOfPropertyChange(string propertyName)
        {
            raisePropertyChanged(propertyName);
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            NotifyOfPropertyChange(property.GetMemberInfo().Name);
        }

        void INotifyPropertyChangedEx.Refresh()
        {
            raisePropertyChanged(string.Empty);
        }
    }
}