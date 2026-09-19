using System.Text.Json;

namespace MadMaxGreekPatcher;

public sealed class PatchItem { public string Name { get; set; } public string Hash { get; set; } public string OrigMd5 { get; set; } public string NewMd5 { get; set; } }
public sealed class FontItem { public string Name { get; set; } public string Hash { get; set; } public string OrigMd5 { get; set; } public string NewMd5 { get; set; } public int Size { get; set; } }
public sealed class SarcFile { public string Name { get; set; } public string OrigMd5 { get; set; } public string NewMd5 { get; set; } public bool PatchLayer { get; set; } }
public sealed class SarcItem { public string Archive { get; set; } public string Hash { get; set; } public bool Chunked { get; set; } public List<SarcFile> Files { get; set; } }
public sealed class Manifest
{
    public int Version { get; set; }
    public List<PatchItem> PatchLayer { get; set; }
    public FontItem Font { get; set; }
    public List<SarcItem> Sarcs { get; set; }

    static readonly JsonSerializerOptions Opt = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    public static Manifest Load(string dir) => JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(dir, "manifest.json")), Opt);
    public static Dictionary<string, Dictionary<string, string>> LoadTranslations(string dir) =>
        JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Path.Combine(dir, "translations.json")));
}
