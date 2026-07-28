public class SimulationState
{
    // Geometry
    public List<SplineCurve> splines = new();
    public List<CurveInterval> internalCurves = new();

    // Topology
    public RoadGraph roadGraph = new();
    public WaterGraph waterGraph = new();
    public VehiculeGraph vehiculeGraph = new();
    public VehiculeGraph pedestrianGraph = new();

    // Features
    public List<Feature> allFeatures = new();
    public List<RoadStroke> roadStrokeFeatures = new();
    public List<RoadSegment> roadSegmentFeatures = new();
    public List<River> riverFeatures = new();
    public List<Lake> lakeFeatures = new();

    //Entities

}