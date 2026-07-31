using System.Numerics;

public class Maps
{

    public static SimulationState Map1()
    {
        SimulationState state = new();

        // RIVER
        River r = new();
        RiverSegment rs = new()
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
            ),
        };
        Junction startRiver = new()
        {
            Position = rs.Centerline.Points[0].Position,
            connectedSegmentIds = new(){ rs.Id }
        };
        rs.StartJunctionId = startRiver.Id;
        Junction endRiver = new()
        {
            Position = rs.Centerline.Points[rs.Centerline.Points.Count-1].Position,   
            connectedSegmentIds = new(){ rs.Id }        
        };
        rs.EndJunctionId = endRiver.Id;

        r.SegmentIds.Add(rs.Id);

        state.junctions.Add(startRiver);
        state.junctions.Add(endRiver);
        state.riverSegmentFeatures.Add(rs);
        state.riverFeatures.Add(r);

        // ROAD

        return state;
    }

}
