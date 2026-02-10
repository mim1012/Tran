using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// ViewModel 기본 클래스
/// INotifyPropertyChanged 및 IDisposable 구현
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
{
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 리소스 해제 (이벤트 구독 해제 등)
    /// 서브클래스에서 override하여 이벤트 구독 해제 로직 추가
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 서브클래스에서 managed 리소스 해제
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
