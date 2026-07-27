using Simulacity.Constants;

namespace Simulacity.Simulator;

public class Simulator : ISimulator {

    public Simulator(){
    }

    public void Init()
    {
    }

    public void Simulate(){
    }

    public GridType GetCell(int x, int y, int size)
    {
        return GridType.Water;
    }

}