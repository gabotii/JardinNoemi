namespace JardinNoemi.Services;

public enum ToastKind
{
    Success,
    Info,
    Warning,
    Error
}

public sealed record Toast(Guid Id, ToastKind Kind, string Title, string? Message, string? ActionText, string? ActionHref);

/// <summary>Notificaciones efímeras para el circuito actual (Scoped). El componente ToastHost las muestra.</summary>
public sealed class ToastService
{
    private readonly List<Toast> _toasts = [];

    public IReadOnlyList<Toast> Toasts => _toasts;

    public event Action? Changed;

    public void Show(ToastKind kind, string title, string? message = null, string? actionText = null, string? actionHref = null)
    {
        var toast = new Toast(Guid.NewGuid(), kind, title, message, actionText, actionHref);
        _toasts.Add(toast);
        if (_toasts.Count > 3) _toasts.RemoveAt(0);
        Changed?.Invoke();

        var lifetime = kind == ToastKind.Error ? TimeSpan.FromSeconds(7) : TimeSpan.FromSeconds(4.5);
        _ = DismissLaterAsync(toast.Id, lifetime);
    }

    public void Success(string title, string? message = null, string? actionText = null, string? actionHref = null) =>
        Show(ToastKind.Success, title, message, actionText, actionHref);

    public void Error(string title, string? message = null) => Show(ToastKind.Error, title, message);
    public void Info(string title, string? message = null) => Show(ToastKind.Info, title, message);
    public void Warning(string title, string? message = null) => Show(ToastKind.Warning, title, message);

    public void Dismiss(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0) Changed?.Invoke();
    }

    private async Task DismissLaterAsync(Guid id, TimeSpan delay)
    {
        await Task.Delay(delay);
        Dismiss(id);
    }
}
