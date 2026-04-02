namespace ExamPlanner.Base;

public class ContentViewBase : ContentView
{
    private ViewModelBase? _viewModel;
    private bool _startCalled;

    protected void InitializeViewModel(ViewModelBase viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
    }

    private async void OnViewLoaded(object? sender, EventArgs e)
    {
        if (_viewModel is null) return;
        await _viewModel.InternalInitialize();
        await _viewModel.Start();
        _startCalled = true;
    }

    private async void OnViewUnloaded(object? sender, EventArgs e)
    {
        if (_viewModel is null) return;
        if (_startCalled)
        {
            await _viewModel.OnStop();
            _startCalled = false;
        }
        await _viewModel.OnRemovedFrom();
    }
}
