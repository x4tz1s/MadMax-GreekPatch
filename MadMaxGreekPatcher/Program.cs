using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MadMaxGreekPatcher;

public static class Program
{
    static bool pause = true;
    static string Md5(byte[] b) => Convert.ToHexString(MD5.HashData(b)).ToLowerInvariant();

    static void Log(string s) => Console.WriteLine(s);
    static void Fail(string s) => throw new PatchError(s);
    sealed class PatchError : Exception { public PatchError(string m) : base(m) { } }

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string game = null, dataDir = null, outDir = null; bool restore = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--game": game = args[++i]; break;
                case "--data": dataDir = args[++i]; break;
                case "--out": outDir = args[++i]; break;
                case "--restore": restore = true; break;
                case "--no-pause": pause = false; break;
                default: Log($"Άγνωστη επιλογή / unknown option: {args[i]}"); return 2;
            }
        }
        int rc = 0;
        try
        {
            Log("=== Mad Max — Ελληνικό Patch v1.0.1 ===");
            string root = FindGameRoot(game);
            string dataRoot = Directory.Exists(Path.Combine(root, "archives_win64")) ? root : Path.Combine(root, "share", "data");
            string archives = Path.Combine(dataRoot, "archives_win64"), patchDir = Path.Combine(dataRoot, "patch_win64");
            string backup = Path.Combine(root, "GreekPatch_Backup");
            Log($"Φάκελος παιχνιδιού / game folder: {root}");
            if (restore) Restore(patchDir, backup);
            else Apply(archives, patchDir, backup, dataDir ?? FindData(), outDir);
        }
        catch (PatchError e) { Console.Error.WriteLine("\nΣΦΑΛΜΑ / ERROR: " + e.Message); rc = 1; }
        catch (Exception e) { Console.Error.WriteLine("\nΑΠΡΟΣΔΟΚΗΤΟ ΣΦΑΛΜΑ / UNEXPECTED ERROR: " + e); rc = 3; }
        if (pause) { Log("\nΠάτα Enter για έξοδο / press Enter to exit..."); try { Console.ReadLine(); } catch { } }
        return rc;
    }

    static string FindData()
    {
        foreach (var d in new[] { Path.Combine(AppContext.BaseDirectory, "Data"), Path.Combine(Directory.GetCurrentDirectory(), "Data"), Path.Combine(AppContext.BaseDirectory, "..", "Data") })
            if (File.Exists(Path.Combine(d, "manifest.json"))) return d;
        Fail("Δεν βρέθηκε ο φάκελος Data (manifest.json, translations.json, font.diff) δίπλα στο πρόγραμμα.\nΚατέβασέ τον από τη σελίδα του mod στο Nexus (ή από https://github.com/x4tz1s/MadMax-GreekPatch/tree/main/Data) και αποσυμπίεσέ τον στον ίδιο φάκελο με το πρόγραμμα, ώστε να υπάρχει το Data\\manifest.json.\nData folder not found: download it from the Nexus mod page (or the GitHub repo) and extract it next to this program.");
        return null;
    }

    static bool IsGameRoot(string d) => Directory.Exists(Path.Combine(d, "archives_win64")) && Directory.Exists(Path.Combine(d, "patch_win64"))
        || Directory.Exists(Path.Combine(d, "share", "data", "archives_win64")) && Directory.Exists(Path.Combine(d, "share", "data", "patch_win64"));

    static string FindGameRoot(string given)
    {
        if (given != null)
        {
            if (!IsGameRoot(given)) Fail($"Ο φάκελος '{given}' δεν μοιάζει με το Mad Max (λείπουν archives_win64 / patch_win64).");
            return Path.GetFullPath(given);
        }
        var cands = new List<string>();
        cands.Add(Directory.GetCurrentDirectory()); cands.Add(AppContext.BaseDirectory); cands.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..")));
        string home = Environment.GetEnvironmentVariable("HOME") ?? "";
        cands.Add(Path.Combine(home, ".local/share/Steam/steamapps/common/Mad Max"));
        foreach (var drv in new[] { "C", "D", "E", "F", "G", "H" })
            foreach (var sub in new[] { @"Program Files (x86)\Steam", @"Program Files\Steam", "Steam", "SteamLibrary", @"Games\Steam", @"Games\SteamLibrary" })
                cands.Add($@"{drv}:\{sub}\steamapps\common\Mad Max");
        foreach (var c in cands) { try { if (IsGameRoot(c)) return Path.GetFullPath(c); } catch { } }
        Log("Δεν βρέθηκε αυτόματα το Mad Max. Γράψε τον φάκελο εγκατάστασης (π.χ. C:\\Program Files (x86)\\Steam\\steamapps\\common\\Mad Max):");
        string p = Console.ReadLine()?.Trim().Trim('"');
        if (string.IsNullOrEmpty(p) || !IsGameRoot(p)) Fail("Μη έγκυρος φάκελος παιχνιδιού.");
        return Path.GetFullPath(p);
    }

    static void Restore(string patchDir, string backup)
    {
        foreach (var f in new[] { "game0.arc", "game0.tab" })
            if (!File.Exists(Path.Combine(backup, f))) Fail($"Δεν υπάρχει αντίγραφο ασφαλείας ({Path.Combine(backup, f)}). Χρησιμοποίησε 'Verify integrity of game files' στο Steam.");
        foreach (var f in new[] { "game0.arc", "game0.tab" }) File.Copy(Path.Combine(backup, f), Path.Combine(patchDir, f), true);
        Log("Επαναφέρθηκαν τα αυθεντικά αρχεία (game0.arc / game0.tab). / Original files restored.");
    }

    static void AppendEntry(TabFile tab, FileStream arc, uint h, byte[] payload, uint usize, bool isNew)
    {
        long pad = (tab.Alignment - arc.Length % tab.Alignment) % tab.Alignment;
        arc.Seek(0, SeekOrigin.End); if (pad > 0) arc.Write(new byte[pad]);
        long off = arc.Length; arc.Write(payload);
        if (arc.Length > uint.MaxValue) Fail("Το game0.arc ξεπέρασε τα 4 GiB (όριο 32-bit offsets).");
        tab.Entries[h] = new TabEntry { Offset = (uint)off, CSize = (uint)payload.Length, USize = usize };
        if (isNew) tab.Order.Add(h);
    }

    static byte[] Translate(byte[] original, Dictionary<string, string> tr, string name, string expectedMd5)
    {
        var sl = StringLookup.Parse(original);
        foreach (var e in sl.Entries)
            if (tr != null && tr.TryGetValue(e.LineHash.ToString("x8"), out var g)) e.Text = Encoding.UTF8.GetBytes(g);
        byte[] built = sl.Build();
        if (Md5(built) != expectedMd5) Fail($"Το αποτέλεσμα για το '{name}' δεν ταιριάζει με το αναμενόμενο — πιθανώς διαφορετική έκδοση παιχνιδιού.");
        return built;
    }

    static void Apply(string archives, string patchDir, string backup, string dataDir, string outDir)
    {
        var manifest = Manifest.Load(dataDir); var tr = Manifest.LoadTranslations(dataDir);
        string curArc = Path.Combine(patchDir, "game0.arc"), curTab = Path.Combine(patchDir, "game0.tab");
        string srcArc = File.Exists(Path.Combine(backup, "game0.arc")) && File.Exists(Path.Combine(backup, "game0.tab")) ? Path.Combine(backup, "game0.arc") : curArc;
        string srcTab = Path.Combine(Path.GetDirectoryName(srcArc)!, "game0.tab");
        bool fromBackup = srcArc != curArc;
        Log(fromBackup ? "Χρήση αυθεντικών αρχείων από το GreekPatch_Backup." : "Χρήση των τρεχόντων αρχείων του παιχνιδιού ως πηγή.");

        var tab = TabFile.Parse(File.ReadAllBytes(srcTab));
        using var src = new FileStream(srcArc, FileMode.Open, FileAccess.Read, FileShare.Read);

        // 1. verify that the source is the pristine game0 the translations were made against
        var origPatch = new Dictionary<string, byte[]>();
        foreach (var it in manifest.PatchLayer)
        {
            uint h = Convert.ToUInt32(it.Hash, 16);
            if (!tab.Entries.ContainsKey(h) || tab.ChunkLists.ContainsKey(h)) Fail($"Λείπει το '{it.Name}' — μη συμβατή έκδοση παιχνιδιού.");
            byte[] o = tab.Read(src, h);
            if (Md5(o) != it.OrigMd5) Fail($"Το '{it.Name}' δεν είναι το αυθεντικό (ήδη τροποποιημένο ή άλλη έκδοση παιχνιδιού).\nΔοκίμασε 'Verify integrity of game files' στο Steam και ξανατρέξε το patch.");
            origPatch[it.Name] = o;
        }
        uint fh = Convert.ToUInt32(manifest.Font.Hash, 16);
        byte[] origFont = tab.Read(src, fh);
        if (Md5(origFont) != manifest.Font.OrigMd5) Fail("Η γραμματοσειρά (mad_max.font) δεν είναι η αυθεντική — ήδη τροποποιημένη ή άλλη έκδοση παιχνιδιού.");
        Log("✔ Τα αρχικά αρχεία επαληθεύτηκαν.");

        if (!fromBackup)
        {
            Directory.CreateDirectory(backup);
            File.Copy(curArc, Path.Combine(backup, "game0.arc"), true); File.Copy(curTab, Path.Combine(backup, "game0.tab"), true);
            Log($"✔ Αντίγραφο ασφαλείας: {backup}");
        }

        outDir ??= patchDir; Directory.CreateDirectory(outDir);
        string tmpArc = Path.Combine(outDir, "game0.arc.tmp");
        File.Copy(srcArc, tmpArc, true);
        using var arc = new FileStream(tmpArc, FileMode.Open, FileAccess.ReadWrite);

        // 2. patch-layer text files (master_gui + dialogue)
        var built = new Dictionary<string, byte[]>();
        foreach (var it in manifest.PatchLayer)
        {
            uint h = Convert.ToUInt32(it.Hash, 16);
            byte[] b = Translate(origPatch[it.Name], tr.GetValueOrDefault(it.Name), it.Name, it.NewMd5);
            built[it.Name] = b;
            byte[] c = Deflate.Compress(b, CompressionLevel.SmallestSize);
            AppendEntry(tab, arc, h, c.Length < b.Length ? c : b, (uint)b.Length, false);
        }
        Log($"✔ {manifest.PatchLayer.Count} αρχεία κειμένου (UI + διάλογοι)");

        // 3. font
        byte[] font = (byte[])origFont.Clone();
        using (var r = new BinaryReader(File.OpenRead(Path.Combine(dataDir, "font.diff"))))
        {
            if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "MMFD") Fail("Κατεστραμμένο font.diff");
            int size = r.ReadInt32(), runs = r.ReadInt32();
            if (size != font.Length) Fail("font.diff δεν ταιριάζει με τη γραμματοσειρά.");
            for (int i = 0; i < runs; i++) { int off = r.ReadInt32(), len = r.ReadInt32(); r.ReadBytes(len).CopyTo(font, off); }
        }
        if (Md5(font) != manifest.Font.NewMd5) Fail("Η ελληνική γραμματοσειρά δεν προέκυψε σωστά.");
        { byte[] c = Deflate.Compress(font, CompressionLevel.SmallestSize); AppendEntry(tab, arc, fh, c.Length < font.Length ? c : font, (uint)font.Length, false); }
        Log("✔ Ελληνική γραμματοσειρά");

        // 4. SARC containers (missions / cutscenes)
        var btabs = new Dictionary<string, (TabFile, FileStream)>();
        int n = manifest.Sarcs.Count, done = 0; var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var s in manifest.Sarcs)
        {
            done++;
            uint h = Convert.ToUInt32(s.Hash, 16);
            if (!btabs.TryGetValue(s.Archive, out var bt))
            {
                string tp = Path.Combine(archives, s.Archive + ".tab"), ap = Path.Combine(archives, s.Archive + ".arc");
                if (!File.Exists(tp) || !File.Exists(ap)) Fail($"Λείπει το {s.Archive} από τα archives_win64 — ελλιπής εγκατάσταση.");
                bt = (TabFile.Parse(File.ReadAllBytes(tp)), new FileStream(ap, FileMode.Open, FileAccess.Read, FileShare.Read)); btabs[s.Archive] = bt;
            }
            var (btab, barc) = bt;
            if (!btab.Entries.TryGetValue(h, out var be)) Fail($"Λείπει το {s.Archive}/{s.Hash} — άλλη έκδοση παιχνιδιού.");
            bool chunked = btab.ChunkLists.ContainsKey(h);
            if (chunked != s.Chunked) Fail($"{s.Archive}/{s.Hash}: διαφορετική δομή chunks — άλλη έκδοση παιχνιδιού.");
            byte[] blob = new byte[be.CSize]; barc.Seek(be.Offset, SeekOrigin.Begin); barc.ReadExactly(blob);
            byte[] data; List<(uint U, uint C)> ch = null;
            if (!chunked) data = be.CSize == be.USize ? blob : Deflate.Inflate(blob, (int)be.USize);
            else
            {
                ch = btab.ChunkLists[h];
                if (ch.Count != 2 || ch[0] != (0u, 0u)) Fail($"{s.Archive}/{s.Hash}: μη αναμενόμενα chunks.");
                data = Deflate.Inflate(blob[(int)ch[0].C..(int)ch[1].C], (int)ch[1].U);   // header + text live in chunk 0
            }
            var ents = Sarc.ParseTable(data);
            var repl = new Dictionary<string, byte[]>();
            foreach (var f in s.Files)
            {
                if (f.PatchLayer) { repl[f.Name] = built[f.Name]; continue; }
                var m = ents.Where(e => e.Base == f.Name && e.Size > 0).ToList();
                if (m.Count != 1) Fail($"{s.Archive}/{s.Hash}: το '{f.Name}' δεν βρέθηκε.");
                byte[] o = data[(int)m[0].Off..(int)(m[0].Off + m[0].Size)];
                if (Md5(o) != f.OrigMd5) Fail($"{s.Archive}/{s.Hash}: το '{f.Name}' δεν είναι το αναμενόμενο — άλλη έκδοση παιχνιδιού.");
                repl[f.Name] = Translate(o, tr.GetValueOrDefault(f.Name), f.Name, f.NewMd5);
            }
            if (!chunked)
            {
                byte[] nd = Sarc.PatchInner(data, repl);
                if (nd.Length > Sarc.ChunkSize) Fail("Μη αναμενόμενο μέγεθος SARC.");
                byte[] c = Deflate.Compress(nd, CompressionLevel.Optimal);
                AppendEntry(tab, arc, h, c.Length < nd.Length ? c : nd, (uint)nd.Length, true);
            }
            else
            {
                byte[] nc0 = Sarc.PatchChunk0InPlace(data, repl);
                int c0len = (int)(ch[1].C - ch[0].C);
                byte[] c0 = Deflate.CompressExact(nc0, c0len);
                byte[] entry = new byte[blob.Length]; c0.CopyTo(entry, 0); Array.Copy(blob, c0len, entry, c0len, blob.Length - c0len);
                AppendEntry(tab, arc, h, entry, be.USize, true);
                tab.ChunkLists[h] = new List<(uint, uint)>(ch); tab.ChunkListOrder.Add(h);
            }
            if (done % 20 == 0 || done == n) Log($"  SARC {done}/{n}  ({sw.Elapsed.TotalSeconds:0}s)");
        }
        foreach (var (_, (_, fs)) in btabs) fs.Dispose();
        arc.Flush(); arc.Dispose();

        string tmpTab = Path.Combine(outDir, "game0.tab.tmp");
        File.WriteAllBytes(tmpTab, tab.Build());
        File.Move(tmpArc, Path.Combine(outDir, "game0.arc"), true); File.Move(tmpTab, Path.Combine(outDir, "game0.tab"), true);
        Log($"✔ Έτοιμο! {n} SARC + {manifest.PatchLayer.Count} αρχεία κειμένου. Ξεκίνα το παιχνίδι με γλώσσα ΑΓΓΛΙΚΑ (English).");
        Log("Για επαναφορά: τρέξε το Uninstall.bat (Linux: --restore) ή Steam → Verify integrity of game files.");
    }
}
