
using LocalServersDashBoard.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace LocalServersDashBoard.Views.Pages
{
    public partial class PythonAppPage : INavigableView<PythonAppViewModel>
    {
        public PythonAppViewModel ViewModel { get; }

        public PythonAppPage(PythonAppViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

