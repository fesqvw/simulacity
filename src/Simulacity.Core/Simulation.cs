namespace Simulacity.Core;

/// <summary>
/// The simulation itself. Placeholder until the model is designed: for now it only counts ticks.
/// </summary>
/// <remarks>
/// This project must not depend on Godot, on a renderer, or on a transport (decisions 0003 and 0005).
/// </remarks>
public sealed class Simulation
{
    /// <summary>The number of steps simulated so far.</summary>
    public long Tick { get; private set; }

    /// <summary>Advances the simulation by one step.</summary>
    public void Step() => Tick++;
}
