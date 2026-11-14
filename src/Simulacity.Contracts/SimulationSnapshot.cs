namespace Simulacity.Contracts;

/// <summary>
/// The simulation state the simulator sends to renderers.
/// </summary>
/// <remarks>
/// Provisional: what gets sent will be decided once work on the simulation model has started
/// (see the open question in docs/decisions/0005-render-channel.md).
/// </remarks>
/// <param name="Tick">The number of steps simulated so far.</param>
public sealed record SimulationSnapshot(long Tick);
