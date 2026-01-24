
using LocalServersDashBoard.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace LocalServersDashBoard.Views.Pages
{
    public partial class NodeAppPage : INavigableView<NodeAppViewModel>
    {
        public NodeAppViewModel ViewModel { get; }

        public NodeAppPage(NodeAppViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

