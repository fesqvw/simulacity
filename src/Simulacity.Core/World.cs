using Simulacity.Core.Land;
using Simulacity.Core.Roads;

namespace Simulacity.Core;

/// <summary>
/// The city being simulated: its land and roads, and later its buildings. It doesn't know which
/// simulation technique is running (decision 0008).
/// </summary>
public sealed class World(LandGrid land)
{
    /// <summary>The land under the city.</summary>
    public LandGrid Land { get; } = land ?? throw new ArgumentNullException(nameof(land));

    /// <summary>The road graph.</summary>
    public RoadNetwork Roads { get; } = new();
}
