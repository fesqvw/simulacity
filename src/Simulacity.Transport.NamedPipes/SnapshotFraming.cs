using System.Buffers.Binary;
using System.Text.Json;
using Simulacity.Contracts;

namespace Simulacity.Transport.NamedPipes;

/// <summary>
/// Writes and reads snapshots on a stream: a 4-byte little-endian length, then the snapshot as UTF-8 JSON.
/// </summary>
/// <remarks>
/// Provisional format: what gets sent, and how, will be decided once work on the simulation model
/// has started (decision 0005).
/// </remarks>
internal static class SnapshotFraming
{
    private const int HeaderBytes = sizeof(int);
    private const int MaxPayloadBytes = 64 * 1024 * 1024;

    public static async Task WriteAsync(Stream stream, SimulationSnapshot snapshot, CancellationToken cancellationToken)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(snapshot);
        byte[] frame = new byte[HeaderBytes + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
        payload.CopyTo(frame, HeaderBytes);

        // One write per frame, so a frame is either sent whole or the connection is dropped.
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the next snapshot, or returns null when the other end has closed the stream.</summary>
    /// <exception cref="InvalidDataException">The data on the stream isn't a valid frame.</exception>
    public static async Task<SimulationSnapshot?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] header = new byte[HeaderBytes];
        if (!await TryReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        int length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 0 or > MaxPayloadBytes)
        {
            throw new InvalidDataException($"Invalid snapshot frame length: {length}.");
        }

        byte[] payload = new byte[length];
        if (!await TryReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SimulationSnapshot>(payload)
                ?? throw new InvalidDataException("Snapshot frame contained null.");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException("Snapshot frame is not valid JSON.", e);
        }
    }

    private static async Task<bool> TryReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }
}
