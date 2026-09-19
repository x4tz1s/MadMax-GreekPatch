using System.Buffers.Binary;

namespace MadMaxGreekPatcher;

/// <summary>Avalanche SARC v2 container: u32 4,"SARC",u32 2,u32 hdrsize, then {u32 nameLen,name,u32 off,u32 size}* (terminated by
/// nameLen 0 / padding). Data offsets are absolute inside the container.</summary>
public static class Sarc
{
    public const int ChunkSize = 46137344; // 44 MiB: chunked SARCs consist of independent raw-deflate streams of this size

    public sealed class SEntry { public string Name; public uint Off, Size; public int Field; public string Base => Name[(Name.LastIndexOf('/') + 1)..]; }

    public static List<SEntry> ParseTable(byte[] d)
    {
        if (d[4] != 'S' || d[5] != 'A' || d[6] != 'R' || d[7] != 'C' || BitConverter.ToUInt32(d, 8) != 2) throw new InvalidDataException("not a SARC v2");
        long hdr = BitConverter.ToUInt32(d, 12);
        int p = 16; var ents = new List<SEntry>();
        while (p < hdr + 16)
        {
            uint l = BitConverter.ToUInt32(d, p);
            if (l == 0) break;
            string name = System.Text.Encoding.Latin1.GetString(d, p + 4, (int)l).TrimEnd('\0'); p += 4 + (int)l;
            ents.Add(new SEntry { Name = name, Off = BitConverter.ToUInt32(d, p), Size = BitConverter.ToUInt32(d, p + 4), Field = p }); p += 8;
        }
        if (p > hdr + 16) throw new InvalidDataException("SARC table overran the header");
        foreach (var e in ents) if (e.Size > 0 && e.Off < hdr + 16) throw new InvalidDataException("SARC data offset inside the table");
        return ents;
    }

    static void Put(byte[] d, int pos, uint a, uint b) { BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(pos), a); BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(pos + 4), b); }

    /// <summary>Unchunked SARCs: append each new file at the end (4-aligned) and repoint its table entry. Everything else stays put.</summary>
    public static byte[] PatchInner(byte[] d, Dictionary<string, byte[]> repl)
    {
        var ents = ParseTable(d);
        var o = new MemoryStream(d.Length + (1 << 16)); o.Write(d);
        var fixups = new List<(int, uint, uint)>(); var used = new HashSet<string>();
        foreach (var e in ents)
        {
            if (e.Size == 0 || !repl.TryGetValue(e.Base, out var nw)) continue;
            while (o.Length % 4 != 0) o.WriteByte(0);
            fixups.Add((e.Field, (uint)o.Length, (uint)nw.Length)); o.Write(nw); used.Add(e.Base);
        }
        foreach (var k in repl.Keys) if (!used.Contains(k)) throw new InvalidDataException($"{k} not found in SARC");
        byte[] r = o.ToArray();
        foreach (var (f, a, b) in fixups) Put(r, f, a, b);
        return r;
    }

    /// <summary>Chunked SARCs must keep an IDENTICAL layout: the (bigger) Greek file is written over a contiguous run of
    /// sibling-language files, the English entry is pointed there and every consumed sibling is pointed at the old English copy.</summary>
    public static byte[] PatchChunk0InPlace(byte[] chunk0, Dictionary<string, byte[]> repl)
    {
        var ents = ParseTable(chunk0);
        byte[] o = (byte[])chunk0.Clone();
        foreach (var (bname, nw) in repl)
        {
            var E = ents.Where(e => e.Base == bname && e.Size > 0).ToList();
            if (E.Count != 1) throw new InvalidDataException($"{bname}: {E.Count} matches");
            var eng = E[0];
            string stem = eng.Name[..^"eng.stringlookup".Length];
            var sibs = ents.Where(e => e != eng && e.Size > 0 && e.Name.StartsWith(stem, StringComparison.Ordinal) && e.Name.EndsWith(".stringlookup")).OrderBy(e => e.Off).ToList();
            if (sibs.Count == 0) throw new InvalidDataException($"no siblings for {bname}");
            var run = new List<SEntry> { sibs[0] }; long end = sibs[0].Off + sibs[0].Size;
            while (end - run[0].Off < nw.Length)
            {
                var nxt = sibs.FirstOrDefault(s => !run.Contains(s) && s.Off - end >= 0 && s.Off - end <= 3);
                if (nxt == null) throw new InvalidDataException($"{bname}: no contiguous sibling run big enough ({nw.Length} B)");
                run.Add(nxt); end = nxt.Off + nxt.Size;
            }
            uint start = run[0].Off;
            if (end > chunk0.Length) throw new InvalidDataException("region leaves chunk 0");
            uint oldOff = eng.Off, oldSize = eng.Size;
            Array.Clear(o, (int)start, (int)(end - start));
            nw.CopyTo(o, (int)start);
            Put(o, eng.Field, start, (uint)nw.Length);
            foreach (var s in run) Put(o, s.Field, oldOff, oldSize);
        }
        return o;
    }
}
