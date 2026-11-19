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
        Assert.Empty(settings.CardIds);
        Assert.Equal(1100, settings.DebounceMs);
        Assert.Equal("C|G|Am|F", string.Join('|', settings.Steps.Select(step => step.Label)));
        Assert.Equal("C.wav|G.wav|Am.wav|F.wav", string.Join('|', settings.Steps.Select(step => step.WavFile)));
    }

    [Fact]
    public void NoteSyntaxAndACardFilterAreAccepted()
    {
        var settings = ChordSettings.Parse("""
            mode = alongside
            cardIds = WahChoKi, KI_HOP
            progression = Dm:D4,F4,A4 | Bb:Bb3,D4,F4
            """);

        Assert.Empty(settings.Warnings);
        Assert.Equal(ChordMode.Alongside, settings.Mode);
        Assert.Equal(["KI_HOP", "WAH_CHO_KI"], settings.CardIds.OrderBy(id => id).ToArray());
        Assert.Equal("Dm", settings.Steps[0].Label);
        Assert.True(settings.Steps[0].CustomNotes);
        Assert.Equal(["D4", "F4", "A4"], settings.Steps[0].Notes);
        Assert.Null(settings.Steps[0].WavFile);
        Assert.Equal(["Bb3", "D4", "F4"], settings.Steps[1].Notes);
    }
}

public class ProgressionTests
{
    static ChordSettings AllCards() => ChordSettings.Parse("progression = C.wav | G.wav | Am.wav | F.wav");

    [Fact]
    public void SuccessivePlaysCycleAndACompoundCardStaysOnOneChord()
    {
        var state = new ProgressionState();
        var settings = AllCards();

        var first = state.Consider(MelodyEvents.Wah, "WAH_CHO_KI", settings, nowMs: 1_000);
        var secondSyllable = state.Consider(MelodyEvents.Cho, "WAH_CHO_KI", settings, nowMs: 1_450);
        var thirdSyllable = state.Consider(MelodyEvents.Ki, "WAH_CHO_KI", settings, nowMs: 1_900);
        var nextCard = state.Consider(MelodyEvents.Wah, "WAH_HAMMER", settings, nowMs: 3_200);

        Assert.Equal(PlayAction.PlayChord, first.Action);
        Assert.Equal(0, first.ChordIndex);
        Assert.Equal(PlayAction.SuppressOnly, secondSyllable.Action);
        Assert.Equal(PlayAction.SuppressOnly, thirdSyllable.Action);
        Assert.Equal(PlayAction.PlayChord, nextCard.Action);
        Assert.Equal(1, nextCard.ChordIndex);
    }

    [Fact]
    public void ACardFilterLeavesOtherMelodyCardsAlone()
    {
        var state = new ProgressionState();
        var settings = ChordSettings.Parse("cardIds = WAH_CHO_KI");

        var other = state.Consider(MelodyEvents.Wah, "WAH_HAMMER", settings, 0);
        var chosen = state.Consider(MelodyEvents.Ki, "WAH_CHO_KI", settings, 50);
        var strike = state.Consider("event:/sfx/ui/cards/card_movement_B_into_discard", "STRIKE", settings, 2_000);

        Assert.Equal(PlayAction.Ignore, other.Action);
        Assert.Equal(PlayAction.PlayChord, chosen.Action);
        Assert.Equal(PlayAction.Ignore, strike.Action);
        Assert.Equal(0, chosen.ChordIndex);
    }

    [Fact]
    public void AlongsideModeKeepsTheVanillaSyllables()
    {
        var state = new ProgressionState();
        var settings = ChordSettings.Parse("mode = alongside");
        state.Consider(MelodyEvents.Wah, "KI_HOP", settings, 0);
        var extra = state.Consider(MelodyEvents.Cho, "KI_HOP", settings, 400);

        Assert.Equal(PlayAction.Ignore, extra.Action);
    }
}

public class InstrumentTests
{
    [Fact]
    public void BuiltInVoicesResolveByNameAndANewVoiceNeedsOnlyRegistration()
    {
        var registry = InstrumentCatalog.Create();
        Assert.Equal(["piano", "acoustic", "electric", "jazz", "drums", "custom"], registry.All.Select(voice => voice.Id).ToArray());

        Assert.Equal("electric", registry.Resolve("Electric Guitar", out var warning).Id);
        Assert.Null(warning);
        Assert.Equal("acoustic", registry.Resolve("guitar", out _).Id);
        Assert.Equal("jazz", registry.Resolve("rhodes", out _).Id);

        var withBell = new InstrumentRegistry();
        withBell.Register(new PianoInstrument());
        withBell.Register(new BellInstrument());
        var step = ChordSettings.DefaultSteps()[0];
        var clip = withBell.Resolve("bell", out _).ClipFor(new InstrumentRequest(step, 0, null));

        Assert.NotNull(clip);
        Assert.Equal("bell", clip.InstrumentId);
        Assert.Equal("piano", withBell.Resolve("bagpipes", out var fallback).Id);
        Assert.Contains("bagpipes", fallback);
    }

    [Fact]
    public void CustomPackMapsCardsAndLeavesMissingClipsAlone()
    {
        var pack = CustomPack.Parse("""
            files = a.wav | b.wav
            card.WAH_CHO_KI = big.wav
            card.Wah Hammer = hit1.wav | hit2.wav
            card.bad = ../nope.wav
            """);
        Assert.Contains(pack.Warnings, warning => warning.Contains("nope", StringComparison.Ordinal));

        var voice = new CustomPackInstrument(pack);
        var step = ChordSettings.DefaultSteps()[0];
        Assert.Equal("big.wav", voice.ClipFor(new InstrumentRequest(step, 0, "WAH_CHO_KI"))!.WavFile);
        Assert.Null(voice.ClipFor(new InstrumentRequest(step, 0, "WAH_CHO_KI"))!.Synthesize);
        Assert.Equal("hit1.wav", voice.ClipFor(new InstrumentRequest(step, 0, "WAH_HAMMER"))!.WavFile);
        Assert.Equal("hit2.wav", voice.ClipFor(new InstrumentRequest(step, 3, "WAH_HAMMER"))!.WavFile);
        Assert.Equal("b.wav", voice.ClipFor(new InstrumentRequest(step, 1, "KI_HOP"))!.WavFile);
        Assert.Null(new CustomPackInstrument(CustomPack.Empty).ClipFor(new InstrumentRequest(step, 0, "KI_HOP")));
    }

    sealed class BellInstrument : IInstrument
    {
        public string Id => "bell";
        public string DisplayName => "Bell";
        public IEnumerable<string> Aliases => [];
        public InstrumentClip? ClipFor(InstrumentRequest request) =>
            PitchedClip.Create(this, request.Step, ["C6"], () => PianoInstrument.Render(["C6"]));
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
    public void JazzVoicingAddsTheSeventhAndNinth()
    {
        var jazz = Sustain(JazzRhodesInstrument.Render(JazzRhodesInstrument.Voicing("C")), 0.15, 0.25);
        var piano = Sustain(PianoInstrument.Render(["C4", "E4", "G4"]), 0.15, 0.25);

        Assert.True(Magnitude(jazz, NoteMath.Frequency("B4")) > Magnitude(piano, NoteMath.Frequency("B4")) * 3);
        Assert.True(Magnitude(jazz, NoteMath.Frequency("D5")) > Magnitude(piano, NoteMath.Frequency("D5")) * 3);
    }

    [Fact]
    public void InstrumentsDoNotSoundAlike()
    {
        var step = ChordSettings.DefaultSteps()[0];
        InstrumentRequest play = new(step, 0, null);
        var piano = InstrumentCatalog.Create().Resolve("piano", out _).ClipFor(play)!.Synthesize!();
        var acoustic = InstrumentCatalog.Create().Resolve("acoustic", out _).ClipFor(play)!.Synthesize!();
        var electric = InstrumentCatalog.Create().Resolve("electric", out _).ClipFor(play)!.Synthesize!();
        var jazz = InstrumentCatalog.Create().Resolve("jazz", out _).ClipFor(play)!.Synthesize!();
        var drums = InstrumentCatalog.Create().Resolve("drums", out _).ClipFor(play)!.Synthesize!();

        Assert.True(Difference(piano, acoustic) > 0.08);
        Assert.True(Difference(acoustic, electric) > 0.05);
        Assert.True(Difference(piano, jazz) > 0.08);
        Assert.True(Difference(piano, drums) > 0.15);
    }

    [Fact]
    public void DrumPatternsVaryAndTheFillHasACymbal()
    {
        var backbeat = DrumKitInstrument.Render(0);
        var fill = DrumKitInstrument.Render(3);
        Assert.True(Difference(backbeat, fill) > 0.08);

        var kick = Sustain(backbeat, 0.01, 0.08);
        var crash = Sustain(fill, 0.68, 0.16);
        Assert.True(Magnitude(kick, 70) > Magnitude(crash, 70));
        Assert.True(Magnitude(crash, 3300) > Magnitude(kick, 3300) * 2);
    }

    [Fact]
    public void ShippedWavsMatchTheSynth()
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "audio"));
        var steps = ChordSettings.DefaultSteps();
        foreach (var instrument in InstrumentCatalog.Create().All)
        {
            var count = instrument is DrumKitInstrument ? DrumKitInstrument.Patterns.Length : steps.Count;
            for (var index = 0; index < count; index++)
            {
                var clip = instrument.ClipFor(new InstrumentRequest(steps[Math.Min(index, steps.Count - 1)], index, null));
                if (clip?.Synthesize is not Func<short[]> synthesize)
                    continue;
                var path = Path.Combine(directory, instrument.Id, clip.WavFile ?? clip.Label + ".wav");
                var shipped = File.ReadAllBytes(path);
                Assert.Equal(ChordSynth.EncodeWav(synthesize()), shipped);
                var decoded = ChordSynth.DecodeWav(shipped);
                Assert.Contains(decoded, sample => Math.Abs(sample) > 1000);
                Assert.Equal(0, decoded[^1]);
            }
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

    static double Difference(short[] left, short[] right)
    {
        var count = Math.Min(left.Length, right.Length);
        double sum = 0;
        for (var i = 0; i < count; i++)
        {
            var delta = (left[i] - right[i]) / 32768.0;
            sum += delta * delta;
        }

        return Math.Sqrt(sum / count);
    }

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
