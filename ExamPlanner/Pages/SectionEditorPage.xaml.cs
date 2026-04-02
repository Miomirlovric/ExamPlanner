using ExamPlanner.Base;
using ExamPlanner.ViewModels;

namespace ExamPlanner.Pages;

public partial class SectionEditorPage : ContentPageBase
{
    public SectionEditorPage(SectionEditorViewModel viewModel)
    {
        InitializeViewModel(viewModel);
        InitializeComponent();
    }
}
