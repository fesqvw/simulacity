using System.Diagnostics;
using Simulacity.Contracts;
using Simulacity.Transport.NamedPipes;

namespace Simulacity.Tests;

/// <summary>
/// End-to-end tests of the named-pipe render channel, covering the start-order rules in decision 0005.
/// </summary>
public class NamedPipeChannelTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // Each test gets its own pipe, so tests can run in parallel.
    private readonly string _pipeName = $"simulacity-test-{Guid.NewGuid():N}";

    [Fact]
    public async Task Publishing_with_no_renderer_connected_does_not_wait()
    {
        await using var publisher = new NamedPipeStatePublisher(_pipeName);

        var stopwatch = Stopwatch.StartNew();
        for (int tick = 0; tick < 100; tick++)
        {
            await publisher.PublishAsync(Snapshot(tick));
        }

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Publishing took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task A_renderer_started_after_the_simulator_gets_the_latest_state_then_updates()
    {
        await using var publisher = new NamedPipeStatePublisher(_pipeName);
        await publisher.PublishAsync(Snapshot(1));
        await publisher.PublishAsync(Snapshot(2));

        using var cancel = new CancellationTokenSource(Timeout);
        await using var receiver = new NamedPipeStateReceiver(_pipeName);
        await using IAsyncEnumerator<SimulationSnapshot> snapshots = receiver.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);

        Assert.Equivalent(Snapshot(2), await NextAsync(snapshots), strict: true);

        await publisher.PublishAsync(Snapshot(3));
        Assert.Equivalent(Snapshot(3), await NextAsync(snapshots), strict: true);
    }

    [Fact]
    public async Task A_renderer_started_before_the_simulator_waits_for_it()
    {
        using var cancel = new CancellationTokenSource(Timeout);
        await using var receiver = new NamedPipeStateReceiver(_pipeName);
        await using IAsyncEnumerator<SimulationSnapshot> snapshots = receiver.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);
        ValueTask<bool> first = snapshots.MoveNextAsync();

        await Task.Delay(TimeSpan.FromMilliseconds(300));
        Assert.False(first.IsCompleted);

        await using var publisher = new NamedPipeStatePublisher(_pipeName);
        await publisher.PublishAsync(Snapshot(7));

        Assert.True(await first);
        Assert.Equivalent(Snapshot(7), snapshots.Current, strict: true);
    }

    [Fact]
    public async Task A_renderer_reconnects_when_the_simulator_restarts()
    {
        using var cancel = new CancellationTokenSource(Timeout);
        await using var receiver = new NamedPipeStateReceiver(_pipeName);
        var connectionChanges = new List<bool>();
        receiver.ConnectionChanged += connected => { lock (connectionChanges) { connectionChanges.Add(connected); } };
        await using IAsyncEnumerator<SimulationSnapshot> snapshots = receiver.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);

        await using (var first = new NamedPipeStatePublisher(_pipeName))
        {
            await first.PublishAsync(Snapshot(1));
            Assert.Equivalent(Snapshot(1), await NextAsync(snapshots), strict: true);
        }

        await using var second = new NamedPipeStatePublisher(_pipeName);
        await second.PublishAsync(Snapshot(10));

        Assert.Equivalent(Snapshot(10), await NextAsync(snapshots), strict: true);
        lock (connectionChanges)
        {
            Assert.Equal([true, false, true], connectionChanges);
        }
    }

    [Fact]
    public async Task Several_renderers_can_watch_at_once()
    {
        await using var publisher = new NamedPipeStatePublisher(_pipeName);
        using var cancel = new CancellationTokenSource(Timeout);
        await using var receiverA = new NamedPipeStateReceiver(_pipeName);
        await using var receiverB = new NamedPipeStateReceiver(_pipeName);
        await using IAsyncEnumerator<SimulationSnapshot> a = receiverA.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);
        await using IAsyncEnumerator<SimulationSnapshot> b = receiverB.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);
        ValueTask<bool> nextA = a.MoveNextAsync();
        ValueTask<bool> nextB = b.MoveNextAsync();

        await WaitUntilAsync(() => publisher.ConnectedRenderers == 2);
        await publisher.PublishAsync(Snapshot(5));

        Assert.True(await nextA);
        Assert.True(await nextB);
        Assert.Equivalent(Snapshot(5), a.Current, strict: true);
        Assert.Equivalent(Snapshot(5), b.Current, strict: true);
    }

    [Fact]
    public async Task The_simulator_forgets_a_renderer_that_closes()
    {
        await using var publisher = new NamedPipeStatePublisher(_pipeName);
        await publisher.PublishAsync(Snapshot(1));

        using (var cancel = new CancellationTokenSource(Timeout))
        {
            await using var receiver = new NamedPipeStateReceiver(_pipeName);
            await using IAsyncEnumerator<SimulationSnapshot> snapshots = receiver.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);
            await NextAsync(snapshots);
            Assert.Equal(1, publisher.ConnectedRenderers);
        }

        await WaitUntilAsync(() => publisher.ConnectedRenderers == 0);
        await publisher.PublishAsync(Snapshot(2));
    }

    /// <summary>A small but complete snapshot: a 2 × 1 grid with a water cell, and one bridge.</summary>
    private static SimulationSnapshot Snapshot(long tick) =>
        new(
            tick,
            new LandSnapshot(2, 1, 10, [LandKind.Ground, LandKind.Water], [0, 1.5, 3, 0, -2, 4]),
            new RoadsSnapshot(
                [new RoadNodeSnapshot(1, new Position(0, 5, 0)), new RoadNodeSnapshot(2, new Position(20, 5, 3))],
                [new RoadSegmentSnapshot(7, 1, 2, [new Position(0, 5, 0), new Position(10, 5, 6.25), new Position(20, 5, 3)])]));

    private static async Task<SimulationSnapshot> NextAsync(IAsyncEnumerator<SimulationSnapshot> snapshots)
    {
        Assert.True(await snapshots.MoveNextAsync().AsTask().WaitAsync(Timeout));
        return snapshots.Current;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(stopwatch.Elapsed < Timeout, "Timed out waiting for the condition.");
            await Task.Delay(20);
        }
    }
}
