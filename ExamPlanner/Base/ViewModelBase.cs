using CommunityToolkit.Mvvm.ComponentModel;

namespace ExamPlanner.Base;

public abstract partial class ViewModelBase : ObservableObject
{
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    internal async Task InternalInitialize()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        await Initialize();
    }

    public virtual Task Initialize() => Task.CompletedTask;

    public virtual Task Start() => Task.CompletedTask;

    public virtual Task OnStop() => Task.CompletedTask;

    public virtual Task OnRemovedFrom() => Task.CompletedTask;
}
