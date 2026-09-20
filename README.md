# Mad Max — Ελληνικό Patch / Greek Translation Patch

Ανεπίσημη ελληνική μετάφραση για το **Mad Max (2015, Steam)**: μενού/UI, tutorials, όλοι οι διάλογοι
(gameplay και cutscenes), υπότιτλοι αποστολών, φωνές μάχης και ελληνική γραμματοσειρά.

*Unofficial Greek translation for **Mad Max (2015, Steam)**: UI, tutorials, all dialogue (gameplay + cutscenes),
mission subtitles, combat barks and a Greek font.*

## Πώς δουλεύει / How it works

Το πακέτο **δεν περιέχει αρχεία του παιχνιδιού**. Περιέχει μόνο τις μεταφράσεις (πίνακες `line_hash → ελληνικό κείμενο`,
χωρίς το αγγλικό κείμενο) και ένα μικρό εργαλείο που τις εφαρμόζει πάνω στα **δικά σου** αρχεία του παιχνιδιού.
Το εργαλείο ελέγχει ότι τα αρχεία είναι τα αυθεντικά πριν αλλάξει οτιδήποτε, και κρατά αντίγραφο ασφαλείας.

*This package contains no game data — only translation tables and a small tool that applies them to **your own** game
files. It verifies every original file first and keeps a backup.*

Η μετάφραση μπαίνει στη θέση της **αγγλικής** γλώσσας. Στο παιχνίδι διάλεξε **English**.
*The Greek text replaces the **English** language slot — select English in the game.*

## Εγκατάσταση / Install (Windows)

Το mod έρχεται σε **δύο μέρη**: τα **δεδομένα μετάφρασης** (φάκελος `Data`, στο Nexus) και το **πρόγραμμα** που τα εφαρμόζει (εδώ, στο GitHub).

1. Κατέβασε το πρόγραμμα από τα [Releases](../../releases) (`MadMaxGreekPatcher_v1.0.1_Windows.zip`) και αποσυμπίεσέ το σε έναν φάκελο.
2. Κατέβασε το `Data` από τη σελίδα του mod στο Nexus (`MadMax_Greek_v1.0_Data.zip`) και αποσυμπίεσέ το **στον ίδιο φάκελο**,
   ώστε δίπλα στο `MadMaxGreekPatcher.exe` να υπάρχει ο φάκελος `Data` (με μέσα `manifest.json`, `translations.json`, `font.diff`).
   *(Ο φάκελος `Data` υπάρχει και σε αυτό το repo.)*
3. Τρέξε το **`Install.bat`** (διπλό κλικ). Βρίσκει μόνο του το Steam· αν όχι, θα σου ζητήσει τον φάκελο,
   π.χ. `C:\Program Files (x86)\Steam\steamapps\common\Mad Max`.
4. Περίμενε ~1–2 λεπτά. Στο τέλος γράφει «Έτοιμο!».
5. Στο Steam: Mad Max → Ιδιότητες → Γλώσσα → **English**.

**Χώρος στο δίσκο:** ~3 GB ελεύθερα (το `patch_win64\game0.arc` γίνεται ~1,8 GB, + αντίγραφο ασφαλείας ~0,4 GB).
Το μέγεθος αυτό είναι φυσιολογικό.

### Linux / Steam Deck (Proton ή native)

Κατέβασε το `MadMaxGreekPatcher_v1.0.1_Linux.tar.gz`, βγάλ' το, βάλε δίπλα το φάκελο `Data` και τρέξε `./MadMaxGreekPatcher`
(ή `--game "/path/to/steamapps/common/Mad Max"`).

### Επιλογές / Options

```
Install.bat [--game "<φάκελος παιχνιδιού>"] [--data "<φάκελος Data>"] [--no-pause]     (Windows)
./MadMaxGreekPatcher [--game <φάκελος>] [--data <φάκελος>] [--restore] [--no-pause]    (Linux)
```

Ο φάκελος παιχνιδιού χρειάζεται μόνο αν δεν βρεθεί αυτόματα, π.χ. `Install.bat --game "D:\Games\Mad Max"`.

## Απεγκατάσταση / Uninstall

`Uninstall.bat` (Linux: `./MadMaxGreekPatcher --restore`) ή, εναλλακτικά, Steam → Ιδιότητες → Τοπικά αρχεία →
*Verify integrity of game files*. Σημείωση: το Steam «verify» **αναιρεί** το patch — τρέξε ξανά το εργαλείο μετά.

## Συμβατότητα / Compatibility

Δοκιμασμένο με την έκδοση του Steam (AppID 234140), Windows build και Linux/Proton. Το εργαλείο σταματά με μήνυμα αν τα αρχεία
του παιχνιδιού δεν είναι τα αναμενόμενα (άλλη έκδοση ή ήδη τροποποιημένα) — δεν γράφει ποτέ πάνω σε άγνωστο περιεχόμενο.

## Γνωστά ζητήματα / Known issues

- Η μετάφραση είναι **μηχανική (DeepL)** με χειροκίνητες διορθώσεις στο UI. Οι πολύ σύντομες φράσεις (φωνές μάχης,
  επιφωνήματα) μεταφράστηκαν χωρίς πλαίσιο και μπορεί να έχουν ασυνέπειες. Αναφορές λαθών: ανοίξτε ένα issue.
- Το ακουστικό μέρος παραμένει αγγλικό (μόνο κείμενο).

## Δημιουργία από τον πηγαίο κώδικα / Build

```
dotnet publish MadMaxGreekPatcher -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Απαιτεί .NET 8 SDK. Ο φάκελος `Data/` πρέπει να βρίσκεται δίπλα στο εκτελέσιμο.

## Τεχνικές σημειώσεις / Technical notes

- Τα κείμενα ζουν σε `.stringlookup` (ADF) αρχεία μέσα σε `SARC` containers των base archives. Το patch layer
  (`patch_win64/game0.*`) αντικαθιστά ολόκληρα τα SARC που περιέχουν αγγλικά κείμενα (366 συνολικά).
- Τα SARC μεγαλύτερα από 44 MiB (7) αποθηκεύονται σε chunks· εκεί το layout πρέπει να μείνει πανομοιότυπο, οπότε το ελληνικό
  αρχείο γράφεται πάνω σε συνεχόμενα slots άλλων γλωσσών και το chunk 0 ξανασυμπιέζεται σε ακριβώς το ίδιο μέγεθος.

## Άδεια / License

Ο κώδικας: MIT (βλ. `LICENSE`). Το Mad Max είναι κατοχυρωμένο σήμα των δικαιούχων του· αυτό το έργο είναι ανεπίσημο και
δεν σχετίζεται με τους Avalanche Studios, Warner Bros. Interactive ή Feral Interactive.
