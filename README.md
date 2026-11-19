# BZ Plays Fool Chords

When the Fool plays a card that sings **Wah**, **Cho**, or **Ki**, this mod plays a real musical chord, or a short drum groove if drums are selected. By default the vanilla voice is replaced. Pitched instruments cycle C, G, Am, and F. The default instrument is piano.

The Fool is on the public beta (added in 0.113.0, November 2026). Opt into that beta in Steam before expecting the cards to exist. This mod does not change card text, damage, or block. `affects_gameplay` is false.

## Which sound, and which cards

The musical noise is not a separate "make a sound" effect written on one card. Melody cards call the game's melody VFX, and that VFX plays one of these FMOD events:

- `event:/sfx/characters/fugitive/fugitive_wa`
- `event:/sfx/characters/fugitive/fugitive_cho`
- `event:/sfx/characters/fugitive/fugitive_ki`

`fugitive` is the Fool's internal audio name. A single-melody card such as Wah Hammer plays one event. **Wah Cho Ki** (`WAH_CHO_KI`) plays all three in a row, so it is the card that most literally makes the musical noise. The mod still defaults to every card that triggers those events, and collapses the extra syllables of a compound card into one chord.

That default is deliberate: the noise belongs to the melody voice, not to one card id, and a future melody card will keep working. To limit it, set `cardIds` in `chords.cfg`.

Known melody card ids as of beta 0.113.x:

| Voice | Card ids |
| --- | --- |
| Wah | `WAH_HAMMER`, `WAH_SWEEP`, `WAH_TOUCH`, `WAH_BLAST`, `WAH_CHO`, `WAH_KI`, `WAH_CHO_KI`, `WAH_SYMPHONY` |
| Cho | `CHO_BARBS`, `CHO_FIRM`, `CHO_READY`, `WAH_CHO`, `CHO_KI`, `CHO_LIMBS`, `WAH_CHO_KI`, `CHO_CHORUS` |
| Ki | `KI_HOP`, `KI_WHIP`, `KI_CHANT`, `WAH_KI`, `CHO_KI`, `KI_REST`, `WAH_CHO_KI`, `KI_VERSE` |

Violent Chord (`VIOLENT_CHORD`) is a power that deals damage when you gain Wah. Playing it does not itself sing, so it is not in the default list.

There is a separate workshop mod, [Chowayo](https://github.com/guppi-tw/sts2-chowayo), that replaces these same three events with different voice clips and renames Ki to Yo. It does not play chords. Do not enable both if you want a predictable result: each one prefixes the same play method, and the one that skips the original sound also skips the other mod.

## Install on Windows

1. In Steam, right-click Slay the Spire 2, open Properties, then Betas, and select the public beta.
2. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (the SDK, not only the runtime).
3. In a terminal at this repo:

   ```powershell
   dotnet build src\BZPlaysFoolChords\BZPlaysFoolChords.csproj -c Release
   ```

   If the game is in the default Steam folder, the build references that install's `sts2.dll` and `0Harmony.dll`. For another library:

   ```powershell
   dotnet build src\BZPlaysFoolChords\BZPlaysFoolChords.csproj -c Release -p:Sts2DataDir="D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64"
   ```

4. The staged folder is `dist\BZPlaysFoolChords\`. Copy that whole folder into the game's `mods` directory. Create `mods` if it is not there. Default location:

   ```text
   C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\BZPlaysFoolChords\
   ```

   The game looks for `mods` next to `SlayTheSpire2.exe`. The folder must contain `BZPlaysFoolChords.json`, `BZPlaysFoolChords.dll`, `chords.cfg`, and `audio\`. Do not copy `bin\` itself. Extra `.json` files in `mods` are read as mod manifests.

5. Start the game, open Settings, and enable **BZ Plays Fool Chords** in the mod list. Restart if the game asks.
6. Play the Fool and play a Wah, Cho, or Ki card. The log line starts with `[BZPlaysFoolChords]`. The developer console (`~`) command `open logs` shows it.

## Choose an instrument

The base game’s mod screen only turns mods on and off. It does not have a per-mod options page, and this mod does not require the third-party Mod Config or RitsuLib settings screens. The setting lives in `chords.cfg`, next to the DLL.

Edit `mods\BZPlaysFoolChords\chords.cfg`, then restart the game:

```ini
instrument = jazz
```

| Value | What you hear |
| --- | --- |
| `piano` | Hammered piano triads. This is the default. |
| `acoustic` or `guitar` | Plucked acoustic guitar. |
| `electric` | The same chords through a soft-clipped electric tone. |
| `jazz` or `rhodes` | A tremolo electric piano. C, G, Am, and F become maj7 / min9 voicings (Cmaj9, Gmaj7, Am9, Fmaj9). |
| `drums` | A short kit groove instead of a chord. The four steps are backbeat, push, hats, and a snare fill with a crash. |
| `custom` | Wav files you add under `audio\custom\`. See below. |

`piano`, `acoustic`, `electric`, and `jazz` read `audio\<instrument>\C.wav` and the other progression files. `drums` reads `audio\drums\backbeat.wav`, `push.wav`, `hats.wav`, and `fill.wav`. If a file is missing, the mod synthesizes that voice. All of these sounds are generated in this repo. No third-party samples.

## Change the chords

Edit the same `chords.cfg`, then restart.

Play only Wah Cho Ki:

```ini
cardIds = WAH_CHO_KI
```

Keep the vanilla voice and add the chord:

```ini
mode = alongside
```

Replace the whole progression with notes. `C4` is middle C. Accidentals are `#` and `b`.

```ini
progression = C:C4,E4,G4 | Am:A3,C4,E4 | F:F3,A3,C4 | G:G3,B3,D4
```

Or replace the wavs under `audio\piano\`, `audio\acoustic\`, `audio\electric\`, `audio\jazz\`, or `audio\drums\` with your own 16-bit mono files, keeping the file names. A note list in `progression` is played on the selected instrument and skips that instrument’s built-in jazz voicing. Drums ignore note names and still cycle their four grooves, one per progression step.

`volume` is an extra multiplier. The chord also follows the Master and SFX sliders (each slider value is squared, which is what the game sends to its mixer). Try `volume = 2` or `4` if it is too quiet.

`debounceMs` is how long extra Wah/Cho/Ki syllables from the same card stay on the chord that just played. Wah Cho Ki sings three times; 1100 ms keeps that as one chord. Lower it if two different melody cards played back to back are sharing a chord.

The shipped wavs are generated by `tools\GenerateChords`. Regenerate them after changing a voice:

```powershell
dotnet run --project tools\GenerateChords\GenerateChords.csproj -c Release -- src\BZPlaysFoolChords\audio
```

## Use your own sounds

`instrument = custom` loads another instrument from `mods\BZPlaysFoolChords\audio\custom\`. The hook does not treat it differently from piano or drums. It asks for a clip, and the pack either names a wav or declines. A decline leaves the game's melody voice in place, so a missing file does not play silence.

Edit `audio\custom\pack.cfg` (this is not a `.json` file, because the game treats every `.json` under `mods\` as a mod manifest):

```ini
files = my-chord.wav | another-chord.wav
card.WAH_CHO_KI = wah-cho-ki.wav
card.WAH_HAMMER = hit-1.wav | hit-2.wav
```

Put those wavs in the same folder. `files` is the cycle for cards you did not name. A `card.` line wins for that card, and several files on one line take turns each time that card is played. Card ids are the same ones listed above (`WAH_CHO_KI`, `WAH_HAMMER`, and the rest). Names like `Wah Hammer` are accepted. Paths such as `..\something.wav` are ignored.

Only `.wav` files are loaded. The player does not synthesize a fallback chord for this pack.

Not built yet: an in-game pack editor, more than one pack folder, ogg or mp3, and picking a pack without restarting.

## Add an instrument

The card-play hook only asks the active `IInstrument` for a clip. The request carries the progression step and the card id, so a voice can map individual cards. Playback only receives the clip. Neither one contains a list of instruments. `custom` is the built-in example of a file-backed voice.

1. Add a class that implements `IInstrument` (see `src\BZPlaysFoolChords\Instruments\`). `ClipFor` returns a clip, or null to keep the vanilla melody sound.
2. Register one instance in `InstrumentCatalog.Create`. That is the only registration list.
3. Put 16-bit mono wavs in `audio\<id>\`, or let `ClipFor` synthesize when the file is absent. Run `GenerateChords` if you want generated files checked in.

`chords.cfg` can then set `instrument` to the new id or any alias the class reports. No edit to `MelodySfxPatch`, `ChordSession`, or `GodotChordOutput` is required.

## Build checks

From the repo root, with the .NET 9 SDK:

```powershell
dotnet test tests\BZPlaysFoolChords.Tests\BZPlaysFoolChords.Tests.csproj -c Release
dotnet build src\BZPlaysFoolChords\BZPlaysFoolChords.csproj -c Release
```

The tests cover note tuning, the card-id spelling, the config file, the C–G–Am–F cycle, the one-chord-per-card debounce, that each shipped wav matches its instrument, that the five voices sound different, and that a newly registered instrument is what the lookup returns.

## What was not run in the game

This environment cannot launch Slay the Spire 2, so nothing here was confirmed inside a combat. In particular, these are still assumptions taken from the public beta decompilation and from mods that do run on 0.113.1:

- The loader still calls `[ModInitializer]` and remaps a mismatched `sts2` / `0Harmony` reference onto the game's copies (`ModManager.HandleAssemblyResolveFailure`). A build on a PC that has the game references those DLLs directly instead of the stand-in project in `src\Sts2Refs`.
- `SfxCmd.Play(string sfx, float volume)` is still the method the melody VFX calls, and the parameter order is still the event path then the volume.
- `AudioStreamWav` can be constructed from 16-bit PCM, or `LoadFromFile` can open the wavs, and an `AudioStreamPlayer` parented to the combat room is audible. Playback is Godot audio shaped like the Master and SFX sliders. It is not routed through an FMOD bank, so a paused SFX bus may not pause a chord that has already started.
- The card id is read from the call stack (`MegaCrit.Sts2.Core.Models.Cards`) while the voice plays. That works if the voice still starts synchronously inside the card play. If a patch cannot see the card and `cardIds` is set, that sound is left vanilla. An empty `cardIds` does not need the stack.
- Menu names and the exact beta build on a given PC can move. `min_game_version` is `0.113.0`.
- The instrument choice was not exercised in a combat. Switching it is a `chords.cfg` edit plus a restart, as described above. Whether each voice is loud enough next to the game’s mixer is unverified; raise `volume` if a voice is quiet.
