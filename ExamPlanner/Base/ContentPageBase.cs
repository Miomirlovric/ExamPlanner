namespace ExamPlanner.Base;

public class ContentPageBase : ContentPage
{
    private ViewModelBase? _viewModel;

    protected void InitializeViewModel(ViewModelBase viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Unloaded += OnPageUnloaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel is null) return;
        await _viewModel.InternalInitialize();
        await _viewModel.Start();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel is not null)
            await _viewModel.OnStop();
    }

    private async void OnPageUnloaded(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.OnRemovedFrom();
    }
}
