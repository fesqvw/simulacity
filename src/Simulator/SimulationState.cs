public class SimulationState
{

    /********* GEOMETRY **********/
    public List<SplineCurve> splines = new();
    public List<SplineIntersection> splineIntersections = new();


    /********* FEATURES **********/
    public List<Feature> allFeatures = new();
    public List<Junction> junctions = new();
    // Road
    public List<Road> roadFeatures = new(); // Combination of segments
    public List<RoadSegment> roadSegmentFeatures = new(); // the segment between two intersections represented by a spline
    public List<Street> streetFeatures = new(); // the features and attributes of a segment
    // Water
    public List<River> riverFeatures = new();
    public List<RiverSegment> riverSegmentFeatures = new();
    public List<Lake> lakeFeatures = new();

    /********* ENTITITES **********/


    /********* TOPOLOGY **********/
    public RoadGraph roadGraph = new();
    public WaterGraph waterGraph = new();
    public VehiculeGraph vehiculeGraph = new();
    public VehiculeGraph pedestrianGraph = new();



}