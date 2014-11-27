using System.Linq;

namespace Informicus.TeamExplorerMergePlugin
{
    public class BranchInfo
    {
        public string BranchServerPath { get; set; }
        public string ParentServerPath { get; set; }
        public string OriginalParentServerPath { get; set; }


        public string BranchName
        {
            get
            {
                return BranchServerPath.Split(new char[] { '/' }).LastOrDefault();
            }
        }

        public string ParentName
        {
            get
            {
                return ParentServerPath.Split(new char[] { '/' }).LastOrDefault();
            }
        }

        public string OriginalParentName
        {
            get { return OriginalParentServerPath.Split(new char[] { '/' }).LastOrDefault(); ; }
        }
    }
}