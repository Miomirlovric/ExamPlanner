using ExamPlanner.Base;
using ExamPlanner.ViewModels;

namespace ExamPlanner.Pages;

public partial class QuestionEditorPage : ContentPageBase
{
    public QuestionEditorPage(QuestionEditorViewModel viewModel)
    {
        InitializeViewModel(viewModel);
        InitializeComponent();
    }
}
