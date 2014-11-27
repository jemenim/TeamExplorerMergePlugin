using Microsoft.TeamFoundation.VersionControl.Client;

namespace Informicus.TeamExplorerMergePlugin
{
    public class MergeInfo
    {
        public VersionSpec VersionSpec { get; set; }

        public string FromPath { get; set; }

        public string ToPath { get; set; }
        
        public int ChangesetId { get; set; }

        public bool CanMerge
        {
            get { return !string.IsNullOrEmpty(ToPath); }
        }
    }
}