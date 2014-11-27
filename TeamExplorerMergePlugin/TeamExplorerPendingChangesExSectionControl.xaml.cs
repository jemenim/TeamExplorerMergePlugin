using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Informicus.TeamExplorerMergePlugin
{
    /// <summary>
    /// Interaction logic for MergePageControl.xaml
    /// </summary>
    public partial class TeamExplorerPendingChangesExSectionControl : UserControl
    {
        public TeamExplorerPendingChangesExSectionControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Parent section.
        /// </summary>
        public TeamExplorerPendingChangesExSection ParentSection
        {
            get { return (TeamExplorerPendingChangesExSection)GetValue(ParentSectionProperty); }
            set { SetValue(ParentSectionProperty, value); }
        }
        public static readonly DependencyProperty ParentSectionProperty =
            DependencyProperty.Register("ParentSection", typeof(TeamExplorerPendingChangesExSection), typeof(TeamExplorerPendingChangesExSection));
    }
}
