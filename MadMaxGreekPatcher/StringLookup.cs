using System.Text;

namespace MadMaxGreekPatcher;

/// <summary>Parser/rebuilder for Mad Max .stringlookup files (ADF type 0x9CBBE69A). Text is kept as raw bytes so that
/// untouched strings round-trip byte-for-byte.</summary>
public sealed class StringLookup
{
    const uint Magic = 0x41444620;
    const uint TypeHash = 0x9CBBE69A;

    public sealed class Entry
    {
        public uint LineHash;
        public uint FloatBits;
        public ulong CombinedHash;
        public uint Flag;
        public byte[] Text;
    }

    public List<Entry> Entries = new();
    public byte[] Table3 = Array.Empty<byte>();
    public ulong T3Count;

    public static StringLookup Parse(byte[] d)
    {
        if (BitConverter.ToUInt32(d, 0) != Magic || BitConverter.ToUInt32(d, 4) != 4)
            throw new InvalidDataException("not a version-4 ADF file");
        uint instanceCount = BitConverter.ToUInt32(d, 8);
        uint instanceOffset = BitConverter.ToUInt32(d, 12);
        if (instanceCount != 1) throw new InvalidDataException("expected exactly 1 instance");
        uint typeHash = BitConverter.ToUInt32(d, (int)instanceOffset + 4);
        if (typeHash != TypeHash) throw new InvalidDataException("not a stringlookup");
        long instOff = BitConverter.ToUInt32(d, (int)instanceOffset + 8);
        long instSize = BitConverter.ToUInt32(d, (int)instanceOffset + 12);
        long b = instOff;
        long entriesPtr = (long)BitConverter.ToUInt64(d, (int)b);
        long entriesCnt = (long)BitConverter.ToUInt64(d, (int)b + 8);
        long poolPtr = (long)BitConverter.ToUInt64(d, (int)b + 16);
        long t3Ptr = (long)BitConverter.ToUInt64(d, (int)b + 32);
        var sl = new StringLookup { T3Count = BitConverter.ToUInt64(d, (int)b + 40) };
        for (long i = 0; i < entriesCnt; i++)
        {
            int o = (int)(b + entriesPtr + i * 24);
            int sAbs = (int)(b + poolPtr) + (int)BitConverter.ToUInt32(d, o + 4);
            int end = Array.IndexOf(d, (byte)0, sAbs);
            sl.Entries.Add(new Entry
            {
                LineHash = BitConverter.ToUInt32(d, o),
                FloatBits = BitConverter.ToUInt32(d, o + 8),
                CombinedHash = BitConverter.ToUInt64(d, o + 12),
                Flag = BitConverter.ToUInt32(d, o + 20),
                Text = d[sAbs..end],
            });
        }
        sl.Table3 = d[(int)(b + t3Ptr)..(int)(b + instSize)];
        return sl;
    }

    static int Pad(long v, int a) => (int)((a - v % a) % a);

    public byte[] Build()
    {
        const int baseOff = 0x50;
        const int headerFields = 0x30;
        var pool = new MemoryStream();
        var poolOffsets = new List<uint>();
        foreach (var e in Entries) { poolOffsets.Add((uint)pool.Length); pool.Write(e.Text); pool.WriteByte(0); }
        long poolPtrRel = headerFields + Entries.Count * 24L;
        int pad1 = Pad(poolPtrRel, 16); poolPtrRel += pad1;
        long poolLen = pool.Length;
        long t3PtrRel = poolPtrRel + poolLen;
        int pad2 = Pad(t3PtrRel, 16); t3PtrRel += pad2;

        var body = new MemoryStream();
        var w = new BinaryWriter(body);
        w.Write((ulong)headerFields); w.Write((ulong)Entries.Count); w.Write((ulong)poolPtrRel);
        w.Write((ulong)poolLen); w.Write((ulong)t3PtrRel); w.Write(T3Count);
        for (int i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];
            w.Write(e.LineHash); w.Write(poolOffsets[i]); w.Write(e.FloatBits); w.Write(e.CombinedHash); w.Write(e.Flag);
        }
        w.Write(new byte[pad1]); w.Write(pool.ToArray()); w.Write(new byte[pad2]); w.Write(Table3);
        long instSize = body.Length;
        long instanceOffset = baseOff + instSize;
        int pad3 = Pad(instanceOffset, 16); instanceOffset += pad3;
        long nameTableOffset = instanceOffset + 24;
        byte[] name = Encoding.ASCII.GetBytes("stringlookup");
        byte[] nameTable = new byte[name.Length + 2]; nameTable[0] = (byte)name.Length; name.CopyTo(nameTable, 1);
        long total = nameTableOffset + nameTable.Length;

        var o = new MemoryStream();
        var ow = new BinaryWriter(o);
        ow.Write(Magic); ow.Write(4u);
        ow.Write(1u); ow.Write((uint)instanceOffset); ow.Write(0u); ow.Write(0u); ow.Write(0u); ow.Write(0u);
        ow.Write(1u); ow.Write((uint)nameTableOffset); ow.Write((uint)total);
        for (int i = 0; i < 5; i++) ow.Write(0u);
        ow.Write((byte)0);
        ow.Write(new byte[baseOff - o.Length]);
        ow.Write(body.ToArray()); ow.Write(new byte[pad3]);
        ow.Write(0x169716CFu); ow.Write(TypeHash); ow.Write((uint)baseOff); ow.Write((uint)instSize); ow.Write(0L);
        ow.Write(nameTable);
        return o.ToArray();
    }
}
