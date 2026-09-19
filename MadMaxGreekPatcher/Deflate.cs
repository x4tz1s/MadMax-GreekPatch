using System.IO.Compression;

namespace MadMaxGreekPatcher;

public static class Deflate
{
    public static byte[] Compress(byte[] data, CompressionLevel level)
    {
        var ms = new MemoryStream();
        using (var ds = new DeflateStream(ms, level, leaveOpen: true)) ds.Write(data);
        return ms.ToArray();
    }

    /// <summary>Raw-inflate; reads exactly <paramref name="size"/> bytes (trailing data after the stream is ignored).</summary>
    public static byte[] Inflate(byte[] raw, int size)
    {
        using var ds = new DeflateStream(new MemoryStream(raw), CompressionMode.Decompress);
        byte[] o = new byte[size]; ds.ReadExactly(o); return o;
    }

    /// <summary>Compress <paramref name="data"/> into a VALID raw-deflate stream of EXACTLY <paramref name="target"/> bytes:
    /// compress + sync flush, then an "adjust" group (m empty fixed-Huffman blocks + an empty stored block), empty stored
    /// blocks (5 bytes each) and a final empty block. Throws if the compressed data alone is too big.</summary>
    public static byte[] CompressExact(byte[] data, int target)
    {
        var ms = new MemoryStream();
        var ds = new DeflateStream(ms, CompressionLevel.SmallestSize, leaveOpen: true);
        ds.Write(data); ds.Flush();
        byte[] s = ms.ToArray();
        if (s.Length < 4 || s[^4] != 0 || s[^3] != 0 || s[^2] != 0xFF || s[^1] != 0xFF)
            throw new InvalidOperationException("deflate sync flush marker not found");
        ds.Dispose();

        for (int m = 0; m < 12; m++)
        {
            byte[] adj = AdjustGroup(m);
            int rest = target - s.Length - adj.Length - 5;
            if (rest < 0) throw new InvalidOperationException($"compressed data ({s.Length + adj.Length + 5}) exceeds target {target}");
            if (rest % 5 != 0) continue;
            var o = new MemoryStream(target);
            o.Write(s); o.Write(adj);
            for (int k = 0; k < rest / 5; k++) o.Write(new byte[] { 0, 0, 0, 0xFF, 0xFF });
            o.Write(new byte[] { 1, 0, 0, 0xFF, 0xFF });
            byte[] res = o.ToArray();
            if (res.Length != target) throw new InvalidOperationException("size mismatch");
            byte[] back = Inflate(res, data.Length);
            if (!back.AsSpan().SequenceEqual(data)) throw new InvalidOperationException("round-trip mismatch");
            return res;
        }
        throw new InvalidOperationException("could not reach exact size");
    }

    // m empty fixed-Huffman blocks (10 bits each: BFINAL=0, BTYPE=01, EOB=7 zero bits) followed by a non-final empty stored
    // block (3 header bits, pad to byte, LEN=0, NLEN=0xFFFF).
    static byte[] AdjustGroup(int m)
    {
        var bits = new List<int>();
        for (int i = 0; i < m; i++) { bits.AddRange(new[] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 }); }
        bits.AddRange(new[] { 0, 0, 0 });
        while (bits.Count % 8 != 0) bits.Add(0);
        var o = new List<byte>();
        for (int i = 0; i < bits.Count; i += 8)
        {
            int v = 0; for (int k = 0; k < 8; k++) v |= bits[i + k] << k;
            o.Add((byte)v);
        }
        o.AddRange(new byte[] { 0, 0, 0xFF, 0xFF });
        return o.ToArray();
    }
}
