using ExamPlanner.Pages;

namespace ExamPlanner
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ExamEditorPage), typeof(ExamEditorPage));
            Routing.RegisterRoute(nameof(QuestionEditorPage), typeof(QuestionEditorPage));
        }
    }
}
