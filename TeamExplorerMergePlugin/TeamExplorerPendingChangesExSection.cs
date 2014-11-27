﻿using System;
﻿using System.ComponentModel;
﻿using System.Linq;
﻿using System.Reflection;
﻿using System.Text.RegularExpressions;
﻿using System.Windows;
using System.Windows.Controls;
﻿using System.Windows.Controls.Primitives;
﻿using System.Windows.Input;
﻿using Informicus.TeamExplorerMergePlugin.Base;
﻿using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer.Framework;
using Microsoft.TeamFoundation.MVVM;
﻿using Microsoft.TeamFoundation.VersionControl.Client;
﻿using Microsoft.TeamFoundation.VersionControl.Common;

namespace Informicus.TeamExplorerMergePlugin
{
    [TeamExplorerSection(GuidList.TeamExplorerPendingChangesExSectionGuid, TeamExplorerPageIds.PendingChanges, 1)]
    public class TeamExplorerPendingChangesExSection : TeamExplorerBaseSection
    {
        private readonly Guid _workitemId;
        private readonly Guid _pendingChangesPageId;
        private WorkitemService _workitemService;
        private Guid _mergeWarningGuid;
        private Delegate _checkinDelegate;
        private ITeamExplorerPage _pendingChangesViewModel;

        public TeamExplorerPendingChangesExSection():base()
        {
            this.Title = "Pending Changes Extensions";
            this.IsExpanded = true;
            this.IsVisible = false;
            this.SectionContent = new TeamExplorerPendingChangesExSectionControl();
            this.View.ParentSection = this;
            
            _workitemId = Guid.Parse("a32392d1-6592-4e35-80c6-4b412e73f613");
            _pendingChangesPageId = Guid.Parse(TeamExplorerPageIds.PendingChanges);

            _workitemService = new WorkitemService();
        }

        private void OnCheckinCompleted(object o, object o1)
        {
            var teamExplorer = GetService<ITeamExplorer>();
            var lastUserChangeSet = _workitemService.GetLastUserChangeSet(CurrentContext.TeamProjectCollection, CurrentContext.TeamProjectName);
            var mergeInfo = _workitemService.GetMergeInfo(teamExplorer, CurrentContext, lastUserChangeSet);
            if (mergeInfo.CanMerge)
            {
                var pendingChangesPage = GetPendingChangesPage(SectionContent as DependencyObject);
                _mergeWarningGuid = ShowNotificationOnPage(string.Format("Need merge changeset {0} from {1} to {2}. [Merge] (Merge {0} changeset)", mergeInfo.ChangesetId, mergeInfo.FromPath.ToBranchName(), mergeInfo.ToPath.ToBranchName()), NotificationType.Warning, pendingChangesPage, new RelayCommand(MergeChangeset));
            }
        }

        /// <summary> 
        /// Get the view. 
        /// </summary> 
        private TeamExplorerPendingChangesExSectionControl View
        {
            get { return this.SectionContent as TeamExplorerPendingChangesExSectionControl; }
        }


        public override void Loaded(object sender, SectionLoadedEventArgs e)
        {
            base.Loaded(sender,e);

            var workItemView = FindWorkItemView(SectionContent as DependencyObject);
            var workItemWrapPanel = FindWorkItemWrapPanel(workItemView);
            AddCommand(workItemWrapPanel);

            SubscribeCheckinComleted();
        }

        public override void Dispose()
        {
            UnsubscribeCheckinComleted();
            base.Dispose();
        }

        private void SubscribeCheckinComleted()
        {
            var pendingChangesPage = GetPendingChangesPage(SectionContent as DependencyObject);
            _pendingChangesViewModel = pendingChangesPage.ViewModel;

            var checkinEventInfo = _pendingChangesViewModel.GetType()
                .BaseType.GetEvent("CheckinCompleted", BindingFlags.NonPublic | BindingFlags.Instance);

            Action<object, object> handler = OnCheckinCompleted;
            _checkinDelegate = Delegate.CreateDelegate(checkinEventInfo.EventHandlerType, handler.Target, handler.Method);

            var addMethod = checkinEventInfo.GetAddMethod(true);
            addMethod.Invoke(_pendingChangesViewModel, new[] { _checkinDelegate });
        }
        
        private void UnsubscribeCheckinComleted()
        {
            var checkinEventInfo = _pendingChangesViewModel.GetType()
                .BaseType.GetEvent("CheckinCompleted", BindingFlags.NonPublic | BindingFlags.Instance);

            var removeMethod = checkinEventInfo.GetRemoveMethod(true);
            removeMethod.Invoke(_pendingChangesViewModel, new[] { _checkinDelegate });
        }

        private TeamExplorerPageBase GetPendingChangesPage(DependencyObject section)
        {
            DependencyObject parent = section;
            var index = 0;
            while (index < 10)
            {
                parent = LogicalTreeHelper.GetParent(parent);
                var frameworkElement = parent as FrameworkElement;
                if (frameworkElement != null)
                {
                    var context = frameworkElement.DataContext as TeamExplorerPageHost;
                    if (context != null)
                    {
                        if (context.Id == _pendingChangesPageId)
                        {
                            return context.Page as TeamExplorerPageBase;
                        }
                    }
                }
                index++;
            }

            return null;
        }

        private DependencyObject FindWorkItemView(DependencyObject section)
        {
            DependencyObject parent = section;
            var index = 0;
            while (index < 10)
            {
                parent = LogicalTreeHelper.GetParent(parent);
                var frameworkElement = parent as FrameworkElement;
                if (frameworkElement != null)
                {
                    var context = frameworkElement.DataContext as TeamExplorerPageHost;
                    if (context != null)
                    {
                        if (context.Id == _pendingChangesPageId)
                        {
                            var workitemPageHost =
                                context.Sections.FirstOrDefault(s => s.Id == _workitemId);

                            if (workitemPageHost != null)
                            {
                                var workitemSection = workitemPageHost.Section as TeamExplorerSectionBase;
                                if (workitemSection != null)
                                    return workitemSection.View as DependencyObject;
                                return null;
                            }
                        }
                    }
                }
                index++;
            }

            return null;
        }

        private static WrapPanel FindWorkItemWrapPanel(DependencyObject workItemView)
        {
            if (workItemView == null)
                return null;

            var mainGrid = LogicalTreeHelper.FindLogicalNode(workItemView, "mainGrid");
            if (mainGrid != null)
            {
                var children = LogicalTreeHelper.GetChildren(mainGrid);
                foreach (var child in children)
                {
                    var wrapPanel = child as WrapPanel;
                    if (wrapPanel != null)
                    {
                        if (wrapPanel.Uid == "actionsPanel")
                        {
                            return wrapPanel;
                        }
                    }
                }
            }
            return null;
        }

        private void MergeChangeset(object o)
        {
            var teamExplorer = GetService<ITeamExplorer>();
            HideNotification(_mergeWarningGuid);
            var changesetId = int.Parse(Regex.Replace(o as string, "[^0-9]+", string.Empty));
            var changeset = _workitemService.GetChangeset(CurrentContext,changesetId);

            var mergeInfo = _workitemService.GetMergeInfo(teamExplorer, CurrentContext, changeset);
            var pendingChangesPage = GetPendingChangesPage(SectionContent as DependencyObject);
            var model = (IPendingCheckin)pendingChangesPage.Model;
            model.PendingChanges.Comment = string.Format("Merge changeset {0} from {1} to {2}:\n{3}", mergeInfo.ChangesetId, mergeInfo.FromPath.ToBranchName(), mergeInfo.ToPath.ToBranchName(),changeset.Comment);
            _workitemService.SetWorkItemsFromChangeset(teamExplorer, changeset);

            model.PendingChanges.Workspace.Merge(mergeInfo.FromPath, mergeInfo.ToPath, mergeInfo.VersionSpec, mergeInfo.VersionSpec, LockLevel.None, RecursionType.Full, MergeOptionsEx.None);
            pendingChangesPage.Loaded(this, new PageLoadedEventArgs());
        }

        private void SetLastWorkItem()
        {
            var teamExplorer = GetService<ITeamExplorer>();

            _workitemService.SetLastWorkItems(teamExplorer, CurrentContext);
        }

        private void SetWorkItemsWithStateInProgress()
        {
            var teamExplorer = GetService<ITeamExplorer>();
            _workitemService.SetWorkItemsWithStateInProgress(teamExplorer, CurrentContext);
        }


        private void AddCommand(Panel workItemPanel)
        {
            if (workItemPanel == null)
                return;

            var separator = new Separator();
            separator.Style = (Style) workItemPanel.FindResource("VerticalSeparator");
            workItemPanel.Children.Add(separator);


            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem
            {
                Header = "In Progress",
                Command = new RelayCommand(SetWorkItemsWithStateInProgress)
            });
            contextMenu.Items.Add(new MenuItem
            {
                Header = "From last changeset",
                Command = new RelayCommand(SetLastWorkItem)
            });

            var dropDownLink = new DropDownLink();
            dropDownLink.Text = "Add Work Items...";
            dropDownLink.DropDownMenu = contextMenu;

            workItemPanel.Children.Add(dropDownLink);
        }
    }
}