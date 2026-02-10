using System.Windows.Input;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 비동기 커맨드 - 실행 중 중복 방지
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            _isExecuting = value;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool CanExecute(object? parameter) => !IsExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        IsExecuting = true;
        try
        {
            await _execute();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand 실행 오류: {ex}");
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
