namespace Simulacity.Core;

/// <summary>
/// The simulation itself: it owns the <see cref="World"/> and advances it step by step.
/// For now a step only counts ticks. The agent-based technique comes with agents (decision 0008).
/// </summary>
/// <remarks>
/// This project must not depend on Godot, on a renderer, or on a transport (decisions 0003 and 0005).
/// </remarks>
public sealed class Simulation(World world)
{
    /// <summary>The city being simulated.</summary>
    public World World { get; } = world ?? throw new ArgumentNullException(nameof(world));

    /// <summary>The number of steps simulated so far.</summary>
    public long Tick { get; private set; }

    /// <summary>Advances the simulation by one step.</summary>
    public void Step() => Tick++;
}
