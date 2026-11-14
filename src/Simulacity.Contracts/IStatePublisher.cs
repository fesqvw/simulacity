namespace Simulacity.Contracts;

/// <summary>
/// The simulator's side of the render channel: how it sends state to renderers (decision 0005).
/// </summary>
/// <remarks>
/// The simulator only knows this interface, never the transport behind it. Data flows one way,
/// so there is nothing here for receiving commands. Implementations must not block the simulator
/// or fail when no renderer is connected.
/// </remarks>
public interface IStatePublisher : IAsyncDisposable
{
    /// <summary>Sends the current state to any connected renderer.</summary>
    Task PublishAsync(SimulationSnapshot snapshot, CancellationToken cancellationToken = default);
}
