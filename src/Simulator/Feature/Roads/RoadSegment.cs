public sealed class RoadSegment : Feature
{
        public Guid RoadStrokeId;

        // Topological meaning
        public Guid StartJunctionId;
        public Guid EndJunctionId;

        // Geometric meaning
        public CurveInterval GeometryOnStroke = new();

        public Guid StreetSectionId;
}