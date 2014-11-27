using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Informicus.TeamExplorerMergePlugin
{
    public class WorkitemService
    {
        public void SetLastWorkItems(ITeamExplorer teamExplorer, ITeamFoundationContext context)
        {
            var lastChangeset = GetLastUserChangeSet(context.TeamProjectCollection, context.TeamProjectName);
            if (lastChangeset == null)
                return;

            SetWorkItemsFromChangeset(teamExplorer, lastChangeset);
        }

        public void SetWorkItemsWithStateInProgress(ITeamExplorer teamExplorer, ITeamFoundationContext context)
        {
            var workItemWithStateInProgress = GetWorkItemWithStateInProgress(context.TeamProjectCollection, context.TeamProjectName);

            SetWorkItems(teamExplorer, workItemWithStateInProgress);
        }


        public Changeset GetLastUserChangeSet(TfsConnection tfs, string teamProjectName)
        {
            var versionControl = tfs.GetService<VersionControlServer>();
            
            var path = "$/" + teamProjectName;
            var queryHistory = versionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full, versionControl.AuthorizedUser,
                null, null, 2, true, true);
            var changesets = new List<Changeset>();
            foreach (Changeset changeset in queryHistory)
            {
                changesets.Add(changeset);
            }
            
            var q = versionControl.QueryHistory(path, VersionSpec.Latest, 0, RecursionType.Full,
                versionControl.AuthorizedUser,
                null, null, 1, true, true);

            var lastChangeset = q.Cast<Changeset>().FirstOrDefault();
            
            return lastChangeset;
        }

        public void SetWorkItemsFromChangeset(ITeamExplorer teamExplorer, Changeset changeset)
        {
            var workItemIds = GetAssociatedWorkItemsId(changeset);

            if (workItemIds.Length == 0)
                return;
            SetWorkItems(teamExplorer, workItemIds);
        }

        private int[] GetWorkItemWithStateInProgress(TfsConnection tfs, string teamProjectName)
        {
            var workItemStore = tfs.GetService<WorkItemStore>();

            WorkItemCollection workItemCollection = workItemStore.Query(
                string.Format("SELECT * FROM WorkItems WHERE [System.TeamProject] = '{0}' AND [Assigned To] = @Me AND [State] = 'In Progress'", teamProjectName));
            var workItemIds = (from WorkItem workItem in workItemCollection select workItem.Id).ToArray();

            return workItemIds;
        }

        public void SetWorkItems(ITeamExplorer teamExplorer, int[] workItems)
        {
            var pendingChangesPage =
                (TeamExplorerPageBase)teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.PendingChanges), null);
            var model = (IPendingCheckin)pendingChangesPage.Model;

            var modelType = model.GetType();
            var method = modelType.GetMethod("AddWorkItemsByIdAsync",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            method.Invoke(model, new object[] { workItems, 1 /* Add */});
            
        }

        private static int[] GetAssociatedWorkItemsId(Changeset changeset)
        {
            return HasWorkItem(changeset)
                ? changeset.WorkItems.Select(x=>x.Id).ToArray()
                : new int[0];
        }

        private static bool HasWorkItem(Changeset lastChangeset)
        {
            if (lastChangeset == null)
                return false;

            if (lastChangeset.WorkItems == null || lastChangeset.WorkItems.Length == 0)
                return false;

            return true;
        }

        public MergeInfo GetMergeInfo(ITeamExplorer teamExplorer, ITeamFoundationContext context, Changeset changeset)
        {
            var branchInfos = GetBranches(teamExplorer, context);
            BranchInfo currentBranch = null;
            var serverItemPaths = (from Change change in changeset.Changes select change.Item.ServerItem).ToArray();
            foreach (var branchInfo in branchInfos)
            {
                if (serverItemPaths.All(x => x.StartsWith(branchInfo.BranchServerPath)))
                {
                    currentBranch = branchInfo;
                    break;
                }
            }
            return new MergeInfo
            {
                ChangesetId = changeset.ChangesetId,
                VersionSpec = new ChangesetVersionSpec(changeset.ChangesetId),
                FromPath = currentBranch.BranchServerPath,
                ToPath = currentBranch.ParentServerPath
            };
        }

        private BranchInfo[] GetBranches(ITeamExplorer teamExplorer, ITeamFoundationContext context)
        {
            var versionControlServer = context.TeamProjectCollection.GetService<VersionControlServer>();

            BranchInfo[] branchInfos = versionControlServer.QueryRootBranchObjects(RecursionType.Full)
                .Where(b => !b.Properties.RootItem.IsDeleted)
                .Select(s => new
                {
                    Project = s.Properties.RootItem.Item
                        .Substring(0, s.Properties.RootItem.Item.IndexOf('/', 2)),
                    s.Properties,
                    s.DateCreated,
                    s.ChildBranches
                })
                .Select(s => new
                {
                    s.Project,
                    Branch = s.Properties.RootItem.Item.Replace(s.Project, ""),
                    Parent = s.Properties.ParentBranch != null
                        ? s.Properties.ParentBranch.Item.Replace(s.Project, "")
                        : "",
                    Version = (s.Properties.RootItem.Version as ChangesetVersionSpec)
                        .ChangesetId,
                    s.DateCreated,
                    s.Properties.Owner,
                    ChildBranches = s.ChildBranches
                        .Where(cb => !cb.IsDeleted)
                        .Select(cb => new
                        {
                            Branch = cb.Item.Replace(s.Project, ""),
                            Version = (cb.Version as ChangesetVersionSpec).ChangesetId
                        })
                })
                .Where(s => s.Project.Equals("$/" + context.TeamProjectName))
                .OrderBy(s => s.Project).ThenByDescending(s => s.Version).Select(x => new BranchInfo
                {
                    BranchServerPath = string.Concat(x.Project, x.Branch),
                    OriginalParentServerPath = string.IsNullOrEmpty(x.Parent)? string.Empty: string.Concat(x.Project, x.Parent)
                }).ToArray();

            foreach (var branchInfo in branchInfos)
            {
                branchInfo.ParentServerPath = branchInfo.OriginalParentServerPath;
                // Мёржить можно только в родителя.
                //if (branchInfo.BranchName.EndsWith("Stable"))
                //{
                //    var version = branchInfo.BranchName.Split('.').First();
                //    var parentBranch = branchInfos.Where(x => x.BranchName.EndsWith("Servicing"))
                //        .SingleOrDefault(x => x.BranchName.StartsWith(version));
                //    branchInfo.ParentServerPath = parentBranch.BranchServerPath;
                //}
                //else if (branchInfo.BranchName.EndsWith("Servicing"))
                //{
                //    branchInfo.ParentServerPath = branchInfo.OriginalParentServerPath;
                //}
                //else
                //{
                //    branchInfo.ParentServerPath = branchInfo.OriginalParentServerPath;
                //}
            }
            return branchInfos;
        }

        public Changeset GetChangeset(ITeamFoundationContext currentContext, int changesetId)
        {
            var versionControl = currentContext.TeamProjectCollection.GetService<VersionControlServer>();
            var changeset = versionControl.GetChangeset(changesetId);
            return changeset;
        }
    }
}