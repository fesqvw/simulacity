namespace Simulacity.Simulator;

public class Simulator : ISimulator {

    SimulationState _state;

    public Simulator(SimulationState initialState)
    {
        _state = initialState;
    }


    public void Init()
    {
    }

    public void Simulate(){
    }

    
    public List<River> GetRivers()
    {
        return _state.riverFeatures;
    }

}