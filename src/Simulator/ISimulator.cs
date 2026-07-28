public interface ISimulator
{
    public void Init();
    public void Simulate();

    public List<River> GetRivers();
}