using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

namespace Informicus.TeamExplorerMergePlugin.Base
{
    /// <summary>
    /// Team Explorer plugin common base class.
    /// </summary>
    public class TeamExplorerBase : IDisposable, INotifyPropertyChanged
    {
        #region Members

        private bool _contextSubscribed;

        #endregion

        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Get/set the service provider.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
            set
            {
                // Unsubscribe from Team Foundation context changes 
                if (_serviceProvider != null)
                {
                    UnsubscribeContextChanges();
                }

                _serviceProvider = value;

                // Subscribe to Team Foundation context changes 
                if (_serviceProvider != null)
                {
                    SubscribeContextChanges();
                }
            }
        }

        /// <summary>
        /// Get the requested service from the service provider.
        /// </summary>
        public T GetService<T>()
        {
            Debug.Assert(ServiceProvider != null, "GetService<T> called before service provider is set");
            if (ServiceProvider != null)
            {
                return (T) ServiceProvider.GetService(typeof (T));
            }

            return default(T);
        }

        /// <summary>
        /// Show a notification in the Team Explorer window.
        /// </summary>
        protected Guid ShowNotification(string message, NotificationType type, ICommand command = null)
        {
            var teamExplorer = GetService<ITeamExplorer>();
            if (teamExplorer != null)
            {
                Guid guid = Guid.NewGuid();
                teamExplorer.ShowNotification(message, type, NotificationFlags.None, command, guid);
                return guid;
            }

            return Guid.Empty;
        }

        protected Guid ShowNotificationOnPage(string message, NotificationType type, ITeamExplorerPage page,ICommand command = null)
        {
            var teamExplorer = GetService<ITeamExplorer>();
            if (teamExplorer != null)
            {
                Guid guid = Guid.NewGuid();
                TeamExplorerUtils.Instance.ShowNotification(ServiceProvider, message, type, NotificationFlags.None, command, guid, page);
                return guid;
            }

            return Guid.Empty;
        }
        
        /// <summary>
        /// Hide a notification in the Team Explorer window.
        /// </summary>
        protected bool HideNotification(Guid id)
        {
            var teamExplorer = GetService<ITeamExplorer>();
            if (teamExplorer != null)
            {
                return teamExplorer.HideNotification(id);
            }

            return false;
        }

        #region IDisposable

        /// <summary>
        /// Dispose.
        /// </summary>
        public virtual void Dispose()
        {
            UnsubscribeContextChanges();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Team Foundation Context

        /// <summary>
        /// Get the current Team Foundation context.
        /// </summary>
        protected ITeamFoundationContext CurrentContext
        {
            get
            {
                var tfContextManager = GetService<ITeamFoundationContextManager>();
                if (tfContextManager != null)
                {
                    return tfContextManager.CurrentContext;
                }

                return null;
            }
        }

        /// <summary>
        /// Subscribe to context changes.
        /// </summary>
        protected void SubscribeContextChanges()
        {
            Debug.Assert(ServiceProvider != null, "ServiceProvider must be set before subscribing to context changes");
            if (ServiceProvider == null || _contextSubscribed)
            {
                return;
            }

            var tfContextManager = GetService<ITeamFoundationContextManager>();
            if (tfContextManager != null)
            {
                tfContextManager.ContextChanged += ContextChanged;
                _contextSubscribed = true;
            }
        }

        /// <summary>
        /// Unsubscribe from context changes.
        /// </summary>
        protected void UnsubscribeContextChanges()
        {
            if (ServiceProvider == null || !_contextSubscribed)
            {
                return;
            }

            var tfContextManager = GetService<ITeamFoundationContextManager>();
            if (tfContextManager != null)
            {
                tfContextManager.ContextChanged -= ContextChanged;
            }
        }

        /// <summary>
        /// ContextChanged event handler.
        /// </summary>
        protected virtual void ContextChanged(object sender, ContextChangedEventArgs e)
        {
        }

        #endregion
    }
}