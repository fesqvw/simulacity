using Simulacity.Core;
using Simulacity.Core.Land;

namespace Simulacity.Tests;

public class SimulationTests
{
    private static Simulation NewSimulation() => new(new World(new LandGrid(4, 4, 10)));

    [Fact]
    public void A_new_simulation_starts_at_tick_zero()
    {
        Assert.Equal(0, NewSimulation().Tick);
    }

    [Fact]
    public void Step_advances_the_tick_by_one()
    {
        var simulation = NewSimulation();

        simulation.Step();
        simulation.Step();

        Assert.Equal(2, simulation.Tick);
    }

    [Fact]
    public void The_simulation_owns_the_world_it_was_given()
    {
        var world = new World(new LandGrid(4, 4, 10));

        Assert.Same(world, new Simulation(world).World);
    }
}
