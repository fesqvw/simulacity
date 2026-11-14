using System.IO.Pipes;
using System.Runtime.CompilerServices;
using Simulacity.Contracts;

namespace Simulacity.Transport.NamedPipes;

/// <summary>
/// Receives snapshots from the simulator over a named pipe. Used by renderers running in their own process.
/// </summary>
/// <remarks>
/// The renderer is started by hand, so the simulator may not be running yet (decision 0005).
/// This waits for it, and reconnects if it stops.
/// </remarks>
public sealed class NamedPipeStateReceiver(string pipeName = NamedPipeDefaults.PipeName) : IStateReceiver
{
    /// <summary>Raised with true when connected to the simulator, and false when the connection is lost.</summary>
    public event Action<bool>? ConnectionChanged;

    /// <inheritdoc />
    public async IAsyncEnumerable<SimulationSnapshot> ReceiveAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // InOut matches the simulator's end of the pipe. Nothing is ever written to it.
            await using var pipe = new NamedPipeClientStream(
                ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            // Waits for as long as it takes for the simulator to start.
            await pipe.ConnectAsync(cancellationToken).ConfigureAwait(false);
            ConnectionChanged?.Invoke(true);

            while (await TryReadAsync(pipe, cancellationToken).ConfigureAwait(false) is { } snapshot)
            {
                yield return snapshot;
            }

            ConnectionChanged?.Invoke(false);
        }
    }

    /// <summary>Reads the next snapshot, or returns null if the connection is lost or broken.</summary>
    private static async Task<SimulationSnapshot?> TryReadAsync(Stream pipe, CancellationToken cancellationToken)
    {
        try
        {
            return await SnapshotFraming.ReadAsync(pipe, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or InvalidDataException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
