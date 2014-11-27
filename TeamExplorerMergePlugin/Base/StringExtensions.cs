using System.Linq;

namespace Informicus.TeamExplorerMergePlugin.Base
{
    public static class StringExtensions
    {
        public static string ToBranchName(this string @branchPath)
        {
            return @branchPath.Split(new char[]{'/'}).LastOrDefault();
        }
    }
}