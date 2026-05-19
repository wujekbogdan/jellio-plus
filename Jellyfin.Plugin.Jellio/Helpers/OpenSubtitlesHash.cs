using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Jellyfin.Plugin.Jellio.Helpers;

/// <summary>
/// Computes the OpenSubtitles hash for a video file.
/// </summary>
public static class OpenSubtitlesHash
{
    private const int ChunkSize = 65536; // 64 KB

    public static string? ComputeFromPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return Compute(stream);
        }
        catch
        {
            return null;
        }
    }

    internal static string? Compute(Stream stream)
    {
        if (stream.Length < ChunkSize * 2)
        {
            return null;
        }

        var firstChunk = ReadChunk(stream, 0);
        var lastChunk = ReadChunk(stream, stream.Length - ChunkSize);

        if (firstChunk == null || lastChunk == null)
        {
            return null;
        }

        unchecked
        {
            var hash = firstChunk
                .Concat(lastChunk)
                .Aggregate(stream.Length, (acc, val) => acc + val);

            return ((ulong)hash).ToString("x16", CultureInfo.InvariantCulture);
        }
    }

    private static long[]? ReadChunk(Stream stream, long offset)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[ChunkSize];
        if (stream.Read(buffer, 0, ChunkSize) != ChunkSize)
        {
            return null;
        }

        return Enumerable.Range(0, ChunkSize / 8)
            .Select(i => BitConverter.ToInt64(buffer, i * 8))
            .ToArray();
    }
}
