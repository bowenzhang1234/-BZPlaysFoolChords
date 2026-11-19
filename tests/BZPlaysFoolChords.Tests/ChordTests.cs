using BZPlaysFoolChords;
using Xunit;

namespace BZPlaysFoolChords.Tests;

public class NoteTests
{
    [Theory]
    [InlineData("A4", 440)]
    [InlineData("C4", 261.6256)]
    [InlineData("E4", 329.6276)]
    [InlineData("G4", 391.9954)]
    public void EqualTemperament(string note, double expected) =>
        Assert.Equal(expected, NoteMath.Frequency(note), precision: 3);

    [Fact]
    public void RejectsWordsThatAreNotNotes() =>
        Assert.Throws<FormatException>(() => NoteMath.Frequency("Wah"));
}

public class CardIdTests
{
    [Theory]
    [InlineData("WahChoKi", "WAH_CHO_KI")]
    [InlineData("WahHammer", "WAH_HAMMER")]
    [InlineData("ChoChorus", "CHO_CHORUS")]
    [InlineData("KiHop", "KI_HOP")]
    [InlineData("ViolentChord", "VIOLENT_CHORD")]
    public void PascalNamesMatchTheGameEntry(string className, string entry) =>
        Assert.Equal(entry, CardIds.ToEntry(className));

    [Theory]
    [InlineData("WAH_CHO_KI", "WAH_CHO_KI")]
    [InlineData("wah-cho-ki", "WAH_CHO_KI")]
    [InlineData("Wah Cho Ki", "WAH_CHO_KI")]
    public void ConfigSpellingCollapsesToTheEntry(string written, string entry) =>
        Assert.Equal(entry, CardIds.Normalize(written));
}

public class SettingsTests
{
    [Fact]
    public void ShippedConfigCyclesTheFourChordsAndReplacesTheVoice()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "chords.cfg"));
        var settings = ChordSettings.Parse(File.ReadAllText(path));

        Assert.Empty(settings.Warnings);
        Assert.Equal(ChordMode.Replace, settings.Mode);
        Assert.Equal("piano", settings.Instrument);
        Assert.Equal("per-syllable", settings.Mapping);
        Assert.Empty(settings.CardIds);
        var syllables = new PerSyllableMapping(settings.Entries);
        Assert.Equal("C", syllables.Map(TestSounds.Event(MelodyEvents.Wah))!.Value.Label);
        Assert.Equal("G", syllables.Map(TestSounds.Event(MelodyEvents.Cho))!.Value.Label);
        Assert.Equal("Am", syllables.Map(TestSounds.Event(MelodyEvents.Ki))!.Value.Label);
    }

    [Fact]
    public void NoteSyntaxAndACardFilterAreAccepted()
    {
        var settings = ChordSettings.Parse("""
            mode = alongside
            cardIds = WahChoKi, KI_HOP
            mapping = loop
            progression = Dm:D4,F4,A4 | Bb:Bb3,D4,F4
            """);

        Assert.Empty(settings.Warnings);
        Assert.Equal(ChordMode.Alongside, settings.Mode);
        Assert.Equal(["KI_HOP", "WAH_CHO_KI"], settings.CardIds.OrderBy(id => id).ToArray());
        var loop = new LoopMapping(settings.Entries);
        var first = loop.Map(TestSounds.Event(MelodyEvents.Wah))!.Value;
        Assert.Equal("Dm", first.Label);
        Assert.True(first.CustomNotes);
        Assert.Equal(["D4", "F4", "A4"], first.Notes);
        Assert.Equal(["Bb3", "D4", "F4"], loop.Map(TestSounds.Event(MelodyEvents.Cho))!.Value.Notes);
    }
}

static class TestSounds
{
    public static SoundEvent Event(string path, string? cardId = null, string? cardType = null, int index = 0) =>
        new(path, cardId, cardType, index);
}

public class MappingTests
{
    [Fact]
    public void PerSyllablePlaysEveryVoiceOfACompoundCard()
    {
        var mapping = new PerSyllableMapping(ChordSettings.Parse("").Entries);
        Assert.Equal("C", mapping.Map(TestSounds.Event(MelodyEvents.Wah, "WAH_CHO_KI"))!.Value.Label);
        Assert.Equal("G", mapping.Map(TestSounds.Event(MelodyEvents.Cho, "WAH_CHO_KI"))!.Value.Label);
        Assert.Equal("Am", mapping.Map(TestSounds.Event(MelodyEvents.Ki, "WAH_CHO_KI"))!.Value.Label);
        Assert.Null(mapping.Map(TestSounds.Event("event:/sfx/ui/click")));
    }

    [Fact]
    public void LoopIgnoresTheSyllableAndWalksTheList()
    {
        var mapping = new LoopMapping(ChordSettings.Parse("progression = C | G | Am | F").Entries);
        Assert.Equal(["C", "G", "Am", "F", "C"], new[]
        {
            mapping.Map(TestSounds.Event(MelodyEvents.Ki))!.Value.Label,
            mapping.Map(TestSounds.Event(MelodyEvents.Wah))!.Value.Label,
            mapping.Map(TestSounds.Event(MelodyEvents.Cho))!.Value.Label,
            mapping.Map(TestSounds.Event(MelodyEvents.Ki))!.Value.Label,
            mapping.Map(TestSounds.Event(MelodyEvents.Wah))!.Value.Label,
        });
    }

    [Fact]
    public void RandomStaysInsideThePool()
    {
        var mapping = new RandomMapping(ChordSettings.Parse("pool = C | Am").Entries, new Random(1));
        var labels = Enumerable.Range(0, 12).Select(_ => mapping.Map(TestSounds.Event(MelodyEvents.Wah))!.Value.Label).ToArray();
        Assert.All(labels, label => Assert.Contains(label, new[] { "C", "Am" }));
        Assert.Contains("C", labels);
        Assert.Contains("Am", labels);
    }

    [Fact]
    public void PerCardUsesTheCardSequenceThenTheFallback()
    {
        var mapping = new PerCardMapping(ChordSettings.Parse("""
            card.WAH_CHO_KI = C | G | Am
            type.Attack = F
            fallback = Am
            """).Entries);

        Assert.Equal(["C", "G", "Am"], new[]
        {
            mapping.Map(TestSounds.Event(MelodyEvents.Wah, "WAH_CHO_KI"))!.Value.Label,
            mapping.Map(TestSounds.Event(MelodyEvents.Cho, "WAH_CHO_KI"))!.Value.Label,
            mapping.Map(TestSounds.Event(MelodyEvents.Ki, "WAH_CHO_KI"))!.Value.Label,
        });
        Assert.Equal("F", mapping.Map(TestSounds.Event(MelodyEvents.Wah, "STRIKE", "Attack"))!.Value.Label);
        Assert.Equal("Am", mapping.Map(TestSounds.Event(MelodyEvents.Ki, "KI_HOP"))!.Value.Label);
    }

    [Fact]
    public void ANewMappingIsSelectedOnlyByRegistration()
    {
        var registry = new MappingRegistry();
        registry.Register(new PerSyllableMapping(new Dictionary<string, string>()));
        registry.Register(new FixedMapping());
        Assert.Equal("fixed", registry.Resolve("fixed", out var warning).Id);
        Assert.Null(warning);
        Assert.Equal("per-syllable", registry.Resolve("nope", out var fallback).Id);
        Assert.Contains("nope", fallback);
    }

    sealed class FixedMapping : ISoundMapping
    {
        public string Id => "fixed";
        public string DisplayName => "Fixed";
        public IEnumerable<string> Aliases => [];
        public MusicalRequest? Map(SoundEvent sound) => new("C", "wah", ["C4", "E4", "G4"], "C.wav", false);
    }
}

public class InstrumentTests
{
    [Fact]
    public void BuiltInVoicesResolveByNameAndANewVoiceNeedsOnlyRegistration()
    {
        var registry = InstrumentCatalog.Create(Path.GetTempPath());
        Assert.Equal(["piano", "samples"], registry.All.Select(voice => voice.Id).ToArray());
        Assert.Equal("samples", registry.Resolve("voice", out var warning).Id);
        Assert.Null(warning);

        var withBell = new InstrumentRegistry();
        withBell.Register(new PianoInstrument());
        withBell.Register(new BellInstrument());
        var music = new MusicalRequest("C", "wah", ["C4", "E4", "G4"], "C.wav", false);
        var clip = withBell.Resolve("bell", out _).ClipFor(new InstrumentRequest(music, null));

        Assert.NotNull(clip);
        Assert.Equal("bell", clip.InstrumentId);
        Assert.Equal("piano", withBell.Resolve("bagpipes", out var fallback).Id);
        Assert.Contains("bagpipes", fallback);
    }

    [Fact]
    public void SampleFolderPrefersASyllableFileThenAChordFile()
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "fool-chords-" + Guid.NewGuid().ToString("N"))).FullName;
        File.WriteAllBytes(Path.Combine(folder, "C.wav"), [0]);
        File.WriteAllBytes(Path.Combine(folder, "wah.ogg"), [0]);
        var voice = new SampleFolderInstrument(folder);
        var music = new MusicalRequest("C", "wah", ["C4", "E4", "G4"], null, false);

        Assert.Equal(Path.Combine(folder, "wah.ogg"), voice.ClipFor(new InstrumentRequest(music, "WAH_HAMMER"))!.AudioPath);

        File.Delete(Path.Combine(folder, "wah.ogg"));
        Assert.Equal(Path.Combine(folder, "C.wav"), voice.ClipFor(new InstrumentRequest(music, null))!.AudioPath);
        Assert.Null(voice.ClipFor(new InstrumentRequest(music with { Label = "Missing", Syllable = "" }, null)));
    }

    sealed class BellInstrument : IInstrument
    {
        public string Id => "bell";
        public string DisplayName => "Bell";
        public IEnumerable<string> Aliases => [];
        public InstrumentClip? ClipFor(InstrumentRequest request) =>
            PitchedClip.Create(this, request.Music, ["C6"], () => PianoInstrument.Render(["C6"]));
    }
}

public class AudioTests
{
    [Fact]
    public void PianoTriadContainsItsThreePitches()
    {
        var sustain = Sustain(PianoInstrument.Render(["C4", "E4", "G4"]), 0.20, 0.25);
        AssertPitch(sustain, "C4");
        AssertPitch(sustain, "E4");
        AssertPitch(sustain, "G4");
    }

    [Fact]
    public void ShippedPianoWavsMatchTheSynth()
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "audio", "piano"));
        foreach (var (name, notes) in ChordLibrary.Defaults)
        {
            var shipped = File.ReadAllBytes(Path.Combine(directory, name + ".wav"));
            Assert.Equal(ChordSynth.EncodeWav(PianoInstrument.Render(notes.Split(','))), shipped);
            var decoded = ChordSynth.DecodeWav(shipped);
            Assert.Contains(decoded, sample => Math.Abs(sample) > 1000);
            Assert.Equal(0, decoded[^1]);
        }
    }

    static void AssertPitch(short[] samples, string note)
    {
        var target = Magnitude(samples, NoteMath.Frequency(note));
        var offChord = Magnitude(samples, 300);
        Assert.True(target > offChord * 3, $"{note} {target} should stand above 300 Hz {offChord}");
    }

    static short[] Sustain(short[] samples, double start, double length) =>
        samples.Skip(SampleAt(start)).Take(SampleAt(length)).ToArray();

    static int SampleAt(double seconds) => (int)(ChordSynth.SampleRate * seconds);

    static double Magnitude(short[] samples, double frequency)
    {
        double real = 0;
        double imag = 0;
        var step = 2 * Math.PI * frequency / ChordSynth.SampleRate;
        for (var i = 0; i < samples.Length; i++)
        {
            var window = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / samples.Length);
            var angle = step * i;
            real += samples[i] * window * Math.Cos(angle);
            imag -= samples[i] * window * Math.Sin(angle);
        }

        return Math.Sqrt(real * real + imag * imag) / samples.Length;
    }
}
