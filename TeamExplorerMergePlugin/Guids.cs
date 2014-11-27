// Guids.cs
// MUST match guids.h
using System;

namespace Informicus.TeamExplorerMergePlugin
{
    static class GuidList
    {
        public const string guidTeamExplorerMergePluginPkgString = "2beb3df9-b23d-4137-822f-2871e03c7544";
        public const string guidTeamExplorerMergePluginCmdSetString = "f8378722-5444-4913-b587-6ac3b1f945fb";
        
        public const string guidTeamExplorerMergeNavigationItem = "8765121e-652b-4227-b381-a3caf0c555ba";
        public const string guidTeamExplorerMergePage = "c7865244-e6c0-4917-a4f8-439a7e747f6a";
        public const string TeamExplorerPendingChangesExSectionGuid = "403720A4-D2F8-4FEF-B659-E8659A4F0BFC";
        
        public static readonly Guid guidTeamExplorerMergePluginCmdSet = new Guid(guidTeamExplorerMergePluginCmdSetString);

    };
}