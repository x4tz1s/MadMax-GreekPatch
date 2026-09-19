namespace MadMaxGreekPatcher;

public sealed class TabEntry { public uint Offset, CSize, USize; }

/// <summary>Avalanche .tab index: alignment, chunk lists, then 16-byte entries (name hash, offset, csize, usize).</summary>
public sealed class TabFile
{
    public uint Alignment = 0x800;
    public List<uint> ChunkListOrder = new();
    public Dictionary<uint, List<(uint U, uint C)>> ChunkLists = new();
    public Dictionary<uint, TabEntry> Entries = new();
    public List<uint> Order = new();

    public static TabFile Parse(byte[] d)
    {
        var t = new TabFile { Alignment = BitConverter.ToUInt32(d, 0) };
        if (t.Alignment != 0x800) throw new InvalidDataException("unexpected .tab header");
        int off = 4;
        uint cl = BitConverter.ToUInt32(d, off); off += 4;
        for (uint i = 0; i < cl; i++)
        {
            uint h = BitConverter.ToUInt32(d, off), n = BitConverter.ToUInt32(d, off + 4); off += 8;
            var l = new List<(uint, uint)>();
            for (uint k = 0; k < n; k++) { l.Add((BitConverter.ToUInt32(d, off), BitConverter.ToUInt32(d, off + 4))); off += 8; }
            t.ChunkLists[h] = l; t.ChunkListOrder.Add(h);
        }
        while (off + 16 <= d.Length)
        {
            uint h = BitConverter.ToUInt32(d, off);
            t.Entries[h] = new TabEntry { Offset = BitConverter.ToUInt32(d, off + 4), CSize = BitConverter.ToUInt32(d, off + 8), USize = BitConverter.ToUInt32(d, off + 12) };
            t.Order.Add(h); off += 16;
        }
        return t;
    }

    public byte[] Build()
    {
        var ms = new MemoryStream(); var w = new BinaryWriter(ms);
        w.Write(Alignment); w.Write((uint)ChunkListOrder.Count);
        foreach (var h in ChunkListOrder)
        {
            var l = ChunkLists[h]; w.Write(h); w.Write((uint)l.Count);
            foreach (var (u, c) in l) { w.Write(u); w.Write(c); }
        }
        foreach (var h in Order) { var e = Entries[h]; w.Write(h); w.Write(e.Offset); w.Write(e.CSize); w.Write(e.USize); }
        return ms.ToArray();
    }

    /// <summary>Reads a NON-chunked entry (raw when csize==usize, otherwise raw deflate).</summary>
    public byte[] Read(Stream arc, uint h)
    {
        var e = Entries[h];
        if (ChunkLists.ContainsKey(h)) throw new NotSupportedException("chunked entry");
        arc.Seek(e.Offset, SeekOrigin.Begin);
        byte[] raw = new byte[e.CSize]; arc.ReadExactly(raw);
        return e.CSize == e.USize ? raw : Deflate.Inflate(raw, (int)e.USize);
    }
}
