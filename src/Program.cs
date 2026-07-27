using Simulacity.Simulator;
using Simulacity.Renderer.ConsoleRenderer;


IRenderer renderer = new ConsoleRenderer();
ISimulator simulator = new Simulator();

simulator.Init();
renderer.Init();

while (true)
{
    simulator.Simulate();
    renderer.Render(simulator);

    Thread.Sleep(100);
}