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
            await publisher.PublishAsync(new SimulationSnapshot(tick));
        }

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), $"Publishing took {stopwatch.Elapsed}.");
    }

    [Fact]
    public async Task A_renderer_started_after_the_simulator_gets_the_latest_state_then_updates()
    {
        await using var publisher = new NamedPipeStatePublisher(_pipeName);
        await publisher.PublishAsync(new SimulationSnapshot(1));
        await publisher.PublishAsync(new SimulationSnapshot(2));

        using var cancel = new CancellationTokenSource(Timeout);
        await using var receiver = new NamedPipeStateReceiver(_pipeName);
        await using IAsyncEnumerator<SimulationSnapshot> snapshots = receiver.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);

        Assert.Equal(new SimulationSnapshot(2), await NextAsync(snapshots));

        await publisher.PublishAsync(new SimulationSnapshot(3));
        Assert.Equal(new SimulationSnapshot(3), await NextAsync(snapshots));
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
        await publisher.PublishAsync(new SimulationSnapshot(7));

        Assert.True(await first);
        Assert.Equal(new SimulationSnapshot(7), snapshots.Current);
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
            await first.PublishAsync(new SimulationSnapshot(1));
            Assert.Equal(new SimulationSnapshot(1), await NextAsync(snapshots));
        }

        await using var second = new NamedPipeStatePublisher(_pipeName);
        await second.PublishAsync(new SimulationSnapshot(10));

        Assert.Equal(new SimulationSnapshot(10), await NextAsync(snapshots));
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
        await publisher.PublishAsync(new SimulationSnapshot(5));

        Assert.True(await nextA);
        Assert.True(await nextB);
        Assert.Equal(new SimulationSnapshot(5), a.Current);
        Assert.Equal(new SimulationSnapshot(5), b.Current);
    }

    [Fact]
    public async Task The_simulator_forgets_a_renderer_that_closes()
    {
        await using var publisher = new NamedPipeStatePublisher(_pipeName);
        await publisher.PublishAsync(new SimulationSnapshot(1));

        using (var cancel = new CancellationTokenSource(Timeout))
        {
            await using var receiver = new NamedPipeStateReceiver(_pipeName);
            await using IAsyncEnumerator<SimulationSnapshot> snapshots = receiver.ReceiveAsync(cancel.Token).GetAsyncEnumerator(cancel.Token);
            await NextAsync(snapshots);
            Assert.Equal(1, publisher.ConnectedRenderers);
        }

        await WaitUntilAsync(() => publisher.ConnectedRenderers == 0);
        await publisher.PublishAsync(new SimulationSnapshot(2));
    }

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
