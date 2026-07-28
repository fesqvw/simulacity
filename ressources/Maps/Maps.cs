using System.Numerics;

public class Maps
{

    public static SimulationState Map1()
    {
        SimulationState state = new();

        River r = new()
        {
            attributes = new RiverAttributes
            {
                AverageWidth = 10,
                FlowRate = 2
            },
            Centerline = SplineFactory.FromPoints(
                    new List<Vector3>()
                    {
                        new Vector3(10,0,0),
                        new Vector3(20,10,0),
                        new Vector3(20,20,0),
                        new Vector3(10,30,0),
                        new Vector3(20,40,0),
                        new Vector3(30,50,0),
                    },
                    false
                )
        };

        state.riverFeatures.Add(r);

        return state;
    }

}
