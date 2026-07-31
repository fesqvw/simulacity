public interface ISimulator
{
    public void Init();
    public void Simulate();

    public SimulationState GetState();
}