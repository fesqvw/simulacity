using System.IO.Pipes;
using Simulacity.Contracts;

namespace Simulacity.Transport.NamedPipes;

/// <summary>
/// Sends snapshots to renderers over a named pipe. The simulator owns the pipe; renderers connect to it.
/// </summary>
/// <remarks>
/// <para>Any number of renderers can connect and disconnect at any time. Publishing never waits for
/// one: with no renderer connected the snapshot is only kept, and each renderer that connects is
/// sent the latest snapshot straight away.</para>
/// <para>A renderer that doesn't take a snapshot within <see cref="SendTimeout"/> is disconnected, so a
/// stuck renderer can't hold up the simulator.</para>
/// </remarks>
public sealed class NamedPipeStatePublisher : IStatePublisher
{
    /// <summary>How long a renderer has to take a snapshot before it's disconnected.</summary>
    public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(1);

    private readonly string _pipeName;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<NamedPipeServerStream> _renderers = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;
    private SimulationSnapshot? _latest;
    private bool _disposed;

    /// <summary>Starts listening for renderers on <paramref name="pipeName"/>.</summary>
    public NamedPipeStatePublisher(string pipeName = NamedPipeDefaults.PipeName)
    {
        _pipeName = pipeName;
        _acceptLoop = Task.Run(() => AcceptRenderersAsync(_stop.Token));
    }

    /// <summary>The number of renderers currently connected.</summary>
    public int ConnectedRenderers
    {
        get
        {
            _gate.Wait();
            try { return _renderers.Count; }
            finally { _gate.Release(); }
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(SimulationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _latest = snapshot;
            foreach (NamedPipeServerStream renderer in _renderers.ToArray())
            {
                await SendLockedAsync(renderer, snapshot).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task AcceptRenderersAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            // InOut only so a pending read can notice when the renderer goes away. Renderers never send anything.
            var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.WaitForConnectionAsync(stop).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                return;
            }
            catch (IOException)
            {
                // A renderer connected and left straight away. Wait for the next one.
                await pipe.DisposeAsync().ConfigureAwait(false);
                continue;
            }

            await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                _renderers.Add(pipe);
                if (_latest is not null)
                {
                    await SendLockedAsync(pipe, _latest).ConfigureAwait(false);
                }
            }
            finally
            {
                _gate.Release();
            }

            _ = WatchForDisconnectAsync(pipe);
        }
    }

    /// <summary>Waits for the renderer to close its end, then forgets it.</summary>
    private async Task WatchForDisconnectAsync(NamedPipeServerStream pipe)
    {
        byte[] buffer = new byte[1];
        try
        {
            while (await pipe.ReadAsync(buffer).ConfigureAwait(false) > 0)
            {
                // Renderers aren't supposed to send anything. Ignore it.
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or OperationCanceledException)
        {
        }

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            RemoveLocked(pipe);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SendLockedAsync(NamedPipeServerStream renderer, SimulationSnapshot snapshot)
    {
        using var timeout = new CancellationTokenSource(SendTimeout);
        try
        {
            await SnapshotFraming.WriteAsync(renderer, snapshot, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException or OperationCanceledException)
        {
            RemoveLocked(renderer);
        }
    }

    private void RemoveLocked(NamedPipeServerStream renderer)
    {
        if (_renderers.Remove(renderer))
        {
            renderer.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _stop.CancelAsync().ConfigureAwait(false);
        await _acceptLoop.ConfigureAwait(false);

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            foreach (NamedPipeServerStream renderer in _renderers.ToArray())
            {
                RemoveLocked(renderer);
            }
        }
        finally
        {
            _gate.Release();
        }

        _stop.Dispose();
    }
}
