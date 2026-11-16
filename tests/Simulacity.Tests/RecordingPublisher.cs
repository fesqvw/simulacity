using Simulacity.Contracts;

namespace Simulacity.Tests;

/// <summary>A publisher that keeps every snapshot it's given, so tests can check what was published.</summary>
internal sealed class RecordingPublisher : IStatePublisher
{
    public List<SimulationSnapshot> Published { get; } = [];

    public Task PublishAsync(SimulationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Published.Add(snapshot);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
