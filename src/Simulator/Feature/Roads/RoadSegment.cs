public sealed class RoadSegment : LinearFeature
{
        public Guid RoadSegmentId; // self
        public Guid RoadStrokeId; // parent
        
        public Guid StartJunctionId;
        public Guid EndJunctionId;
}