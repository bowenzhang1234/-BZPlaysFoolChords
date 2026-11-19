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

public class AudioTests
{
    [Fact]
    public void MajorTriadContainsItsThreePitches()
    {
        var samples = ChordSynth.Render(new[] { "C4", "E4", "G4" }.Select(NoteMath.Frequency).ToArray());
        var sustain = samples.Skip(SampleAt(0.25)).Take(SampleAt(0.30)).ToArray();

        var root = Magnitude(sustain, NoteMath.Frequency("C4"));
        var third = Magnitude(sustain, NoteMath.Frequency("E4"));
        var fifth = Magnitude(sustain, NoteMath.Frequency("G4"));
        var offChord = Magnitude(sustain, 300);

        Assert.True(root > offChord * 4, $"C4 {root} should stand above 300 Hz {offChord}");
        Assert.True(third > offChord * 4, $"E4 {third} should stand above 300 Hz {offChord}");
        Assert.True(fifth > offChord * 4, $"G4 {fifth} should stand above 300 Hz {offChord}");
    }

    [Fact]
    public void ShippedWavsMatchTheSynth()
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "audio"));
        foreach (var (name, notes) in ChordLibrary.Defaults)
        {
            var path = Path.Combine(directory, name + ".wav");
            var shipped = File.ReadAllBytes(path);
            var rendered = ChordSynth.RenderWav(notes.Split(','));
            Assert.Equal(rendered, shipped);
            var decoded = ChordSynth.DecodeWav(shipped);
            Assert.Contains(decoded, sample => Math.Abs(sample) > 1000);
            Assert.Equal(0, decoded[^1]);
        }
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
