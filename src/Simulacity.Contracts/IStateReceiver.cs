namespace Simulacity.Contracts;

/// <summary>
/// The renderer's side of the render channel, for renderers that run in a separate process (decision 0005).
/// </summary>
/// <remarks>
/// A renderer running in the same process as the simulator, such as a future Godot front end,
/// can implement <see cref="IStatePublisher"/> directly instead.
/// </remarks>
public interface IStateReceiver : IAsyncDisposable
{
    /// <summary>
    /// Yields each snapshot as it arrives. Waits for the simulator if it isn't running yet, and
    /// reconnects if it stops, until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    IAsyncEnumerable<SimulationSnapshot> ReceiveAsync(CancellationToken cancellationToken = default);
}
