# BZ Plays Fool Chords

When the Fool plays a card that sings **Wah**, **Cho**, or **Ki**, this mod plays a piano chord. By default Wah is C major, Cho is G major, and Ki is A minor, so Wah Cho Ki plays those three chords in order. The sound source and the chord choice are separate settings.

The Fool is on the public beta (added in 0.113.0, November 2026). Opt into that beta in Steam before expecting the cards to exist. This mod does not change card text, damage, or block. `affects_gameplay` is false.

## Which sound, and which cards

The musical noise is not a separate "make a sound" effect written on one card. Melody cards call the game's melody VFX, and that VFX plays one of these FMOD events:

- `event:/sfx/characters/fugitive/fugitive_wa`
- `event:/sfx/characters/fugitive/fugitive_cho`
- `event:/sfx/characters/fugitive/fugitive_ki`

`fugitive` is the Fool's internal audio name. A single-melody card such as Wah Hammer plays one event. **Wah Cho Ki** (`WAH_CHO_KI`) plays all three in a row, so it is the card that most literally makes the musical noise. The mod plays a sound for every one of those events. Wah Cho Ki therefore plays three chords, at the same moments the game plays the three syllables. To limit which cards do this, set `cardIds` in `chords.cfg`.

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

## Choose a sound source

The base game’s mod screen only turns mods on and off. It does not have a per-mod options page. The setting lives in `chords.cfg`, next to the DLL. This build ships one synthesized instrument, piano, and a folder for your own recordings. The card-play hook only asks the active `IInstrument` for a clip. It does not know whether that clip is piano or a file.

```ini
instrument = piano
```

```ini
instrument = samples
samples = audio/samples
```

`samples` can be any folder. A path that is not absolute is relative to the mod folder. Drop wav or ogg files there. No code change and no extra manifest. The first file that exists is used, in this order:

1. The syllable: `wah.wav`, `cho.ogg`, `ki.wav`
2. The chord: `C.wav`, `G.ogg`, `Am.wav`
3. The card id: `WAH_CHO_KI.wav`

A recording of your own voice is the same kind of file. If nothing matches, the game's melody voice is left as it is.

## Choose how chords are picked

`mapping` is a separate setting. The hook asks the active `ISoundMapping` for a chord and does not know which strategy that is. Each syllable event is mapped on its own.

Per syllable, the default:

```ini
mapping = per-syllable
syllable.wah = C
syllable.cho = G
syllable.ki = Am
```

Loop, in order, ignoring which syllable or card it was. Wah Cho Ki walks three steps of the list:

```ini
mapping = loop
progression = C | G | Am | F
```

Random, from a pool:

```ini
mapping = random
pool = C | G | Am | F
```

One chord, or a sequence, for a card id or a card type. A sequence advances once per syllable of that card. Unmapped cards use `fallback`. If `fallback` is omitted, unmapped cards use the per-syllable chords. Card type is read from the game when that value is available. Card id is the reliable key.

```ini
mapping = per-card
card.WAH_CHO_KI = C | G | Am
card.WAH_HAMMER = C
type.Attack = F
fallback = Am
```

A chord token can be a name (`C`, `G`, `Am`, `F`) or notes (`Dm:D4,F4,A4`).

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

Or replace `audio\piano\C.wav`, `G.wav`, `Am.wav`, and `F.wav` with your own 16-bit mono files, keeping the names. For a voice recording, use `instrument = samples` instead of replacing the piano files.

`volume` is an extra multiplier. The chord also follows the Master and SFX sliders (each slider value is squared, which is what the game sends to its mixer). Try `volume = 2` or `4` if it is too quiet.

The shipped piano wavs are generated by `tools\GenerateChords`. Regenerate them after changing the piano synth:

```powershell
dotnet run --project tools\GenerateChords\GenerateChords.csproj -c Release -- src\BZPlaysFoolChords\audio
```

## Add a sound source or a mapping

Playback never decides which chord or which instrument. It receives a clip. The hook asks `ISoundMapping.Map` for a `MusicalRequest`, then `IInstrument.ClipFor` for a clip.

To add a synthesized voice, implement `IInstrument` and register it in `InstrumentCatalog.Create`. To add a way of choosing chords, implement `ISoundMapping` and register it in `MappingCatalog.Create`. Either class reads whatever keys it needs from the config dictionary. No edit to `MelodySfxPatch` or `GodotChordOutput` is required.

A folder of recordings does not need a new class. `instrument = samples` already plays files you add under `samples`.

Not built yet: an in-game settings page, more than one sample folder at a time, and mp3. Ogg and wav are both loaded.

## Build checks

From the repo root, with the .NET 9 SDK:

```powershell
dotnet test tests\BZPlaysFoolChords.Tests\BZPlaysFoolChords.Tests.csproj -c Release
dotnet build src\BZPlaysFoolChords\BZPlaysFoolChords.csproj -c Release
```

The tests cover note tuning, the card-id spelling, the config file, per-syllable, loop, random, and per-card mappings, the sample-folder lookup, and that a newly registered instrument or mapping is what the lookup returns.

## Troubleshooting

On v0.113.1 the mod could log `Ready` and then throw on every melody: `Object of type 'System.String' cannot be converted to type 'Godot.StringName'`. Godot node's `Name`, and calls such as `CallDeferred`, take a `StringName`, and reflection does not apply the implicit conversion from `string`. Playback now constructs a `StringName` or `NodePath` for those parameters. If playback still fails, the error is logged once and the game's own Wah / Cho / Ki voice is left in place instead of the exception repeating on every card.

## What was not run in the game

This environment cannot launch Slay the Spire 2, so nothing here was confirmed inside a combat. In particular, these are still assumptions taken from the public beta decompilation and from mods that do run on 0.113.1:

- The loader still calls `[ModInitializer]` and remaps a mismatched `sts2` / `0Harmony` reference onto the game's copies (`ModManager.HandleAssemblyResolveFailure`). A build on a PC that has the game references those DLLs directly instead of the stand-in project in `src\Sts2Refs`.
- `SfxCmd.Play(string sfx, float volume)` is still the method the melody VFX calls, and the parameter order is still the event path then the volume.
- `AudioStreamWav` can be constructed from 16-bit PCM, or `LoadFromFile` can open the wavs, and an `AudioStreamPlayer` parented to the combat room is audible. Playback is Godot audio shaped like the Master and SFX sliders. It is not routed through an FMOD bank, so a paused SFX bus may not pause a chord that has already started.
- The card id is read from the call stack (`MegaCrit.Sts2.Core.Models.Cards`) while the voice plays. That works if the voice still starts synchronously inside the card play. If a patch cannot see the card and `cardIds` is set, that sound is left vanilla. An empty `cardIds` does not need the stack.
- Menu names and the exact beta build on a given PC can move. `min_game_version` is `0.113.0`.
- Switching instrument or mapping, and loading a user wav or ogg in combat, were not heard in the game. Card type (`type.Attack`) is read from the live card when the game exposes it. If that read fails, use a `card.` line. Card id from the play stack is the reliable key. Raise `volume` if the piano is quieter than the game.
