using Microsoft.Extensions.Options;

namespace AllByMyshelf.Unit.TestDoubles;

/// <summary>
/// Options accessor whose value can change after construction, standing in for what
/// <c>IConfigurationRoot.Reload()</c> does to a live <see cref="IOptionsMonitor{TOptions}"/>
/// when settings are saved (ABM-075).
/// </summary>
/// <remarks>
/// Implements <see cref="IOptionsSnapshot{TOptions}"/> as well so the same double can be
/// handed to scoped consumers such as the API clients.
/// </remarks>
internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>, IOptionsSnapshot<T>
    where T : class
{
    private readonly List<Action<T, string?>> _listeners = [];

    public T CurrentValue { get; private set; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener)
    {
        _listeners.Add(listener);
        return new Subscription(() => _listeners.Remove(listener));
    }

    /// <summary>
    /// Replaces the current value and notifies listeners, as saving settings and
    /// reloading configuration does at runtime.
    /// </summary>
    public void Set(T updated)
    {
        CurrentValue = updated;

        foreach (var listener in _listeners.ToList())
        {
            listener(updated, Options.DefaultName);
        }
    }

    public T Value => CurrentValue;

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
