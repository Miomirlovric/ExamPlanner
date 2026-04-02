using ExamPlanner.Base;
using ExamPlanner.ViewModels;

namespace ExamPlanner.Pages;

public partial class ExamEditorPage : ContentPageBase
{
    public ExamEditorPage(ExamEditorViewModel viewModel)
    {
        InitializeViewModel(viewModel);
        InitializeComponent();
    }
}
