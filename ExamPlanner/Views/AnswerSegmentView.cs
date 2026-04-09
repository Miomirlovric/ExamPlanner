using Application.Storage;

namespace ExamPlanner.Views;

public class AnswerSegmentView : ContentView
{
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is not AnswerSegment segment)
        {
            Content = null;
            return;
        }

        if (segment.Type == SegmentType.Placeholder)
        {
            Content = new Border
            {
                Padding = new Thickness(8, 4),
                Margin = new Thickness(4, 2),
                BackgroundColor = Color.FromArgb("#E0E0E0"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                Content = new Label
                {
                    FontAttributes = FontAttributes.Bold,
                    Text = segment.DisplayValue,
                    VerticalOptions = LayoutOptions.Center
                }
            };
        }
        else
        {
            Content = new Label
            {
                Text = segment.Text,
                VerticalOptions = LayoutOptions.Center
            };
        }
    }
}
