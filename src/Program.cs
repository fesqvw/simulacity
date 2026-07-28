using Simulacity.Simulator;
using Simulacity.Renderer.ConsoleRenderer;


SimulationState simulationState = Maps.Map1();
ISimulator simulator = new Simulator(simulationState);
IRenderer renderer = new ConsoleRenderer(simulator);

simulator.Init();
renderer.Init();

while (true)
{
    simulator.Simulate();
    renderer.Render();

    Thread.Sleep(100);
}