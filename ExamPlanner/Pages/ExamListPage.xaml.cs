using ExamPlanner.Base;
using ExamPlanner.ViewModels;

namespace ExamPlanner.Pages;

public partial class ExamListPage : ContentPageBase
{
    public ExamListPage(ExamListViewModel viewModel)
    {
        InitializeViewModel(viewModel);
        InitializeComponent();
    }
}
