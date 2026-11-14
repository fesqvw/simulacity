using Simulacity.Core;

namespace Simulacity.Tests;

public class SimulationTests
{
    [Fact]
    public void A_new_simulation_starts_at_tick_zero()
    {
        Assert.Equal(0, new Simulation().Tick);
    }

    [Fact]
    public void Step_advances_the_tick_by_one()
    {
        var simulation = new Simulation();

        simulation.Step();
        simulation.Step();

        Assert.Equal(2, simulation.Tick);
    }
}
