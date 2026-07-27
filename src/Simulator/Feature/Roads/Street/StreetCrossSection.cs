public sealed class StreetCrossSection
{
    public float ShoulderWidthLeft;
    public float ShoulderWidthRight;
    public Sidewalk? LeftSidewalk;
    public Sidewalk? RightSidewalk;
    public Median? Median;
    public List<VehicleLane> Lanes = new();
    public List<Crossing> Crossings = new();
}