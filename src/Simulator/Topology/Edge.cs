public class Edge
{
    public Guid Id = Guid.NewGuid();
    public Guid OriginNode;
    public Guid DestinationNode;
    public float Cost;
}