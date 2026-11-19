namespace BZPlaysFoolChords;

public sealed class DrumKitInstrument : IInstrument
{
    public string Id => "drums";
    public string DisplayName => "Drums";
    public IEnumerable<string> Aliases => ["drum", "drumkit"];

    public static readonly string[] Patterns = ["backbeat", "push", "hats", "fill"];

    public InstrumentClip ClipFor(ChordStep step, int stepIndex)
    {
        var pattern = Math.Abs(stepIndex) % Patterns.Length;
        var name = Patterns[pattern];
        return new InstrumentClip
        {
            InstrumentId = Id,
            Label = name,
            WavFile = name + ".wav",
            CacheKey = Id + "|" + name,
            Synthesize = () => Render(pattern),
        };
    }

    public static short[] Render(int pattern)
    {
        var mix = new double[SynthMath.Length()];
        foreach (var (time, voice) in Hits(pattern))
            Strike(mix, (int)(time * ChordSynth.SampleRate), voice, pattern);

        return SynthMath.ToPcm(mix);
    }

    static IEnumerable<(double Time, DrumVoice Voice)> Hits(int pattern) => pattern switch
    {
        0 =>
        [
            (0.00, DrumVoice.Kick), (0.00, DrumVoice.Hat),
            (0.18, DrumVoice.Hat),
            (0.36, DrumVoice.Snare), (0.36, DrumVoice.Hat),
            (0.54, DrumVoice.Hat),
            (0.72, DrumVoice.Kick),
        ],
        1 =>
        [
            (0.00, DrumVoice.Kick),
            (0.16, DrumVoice.Kick), (0.16, DrumVoice.Hat),
            (0.32, DrumVoice.Snare),
            (0.48, DrumVoice.OpenHat),
            (0.64, DrumVoice.Kick), (0.64, DrumVoice.Snare),
        ],
        2 =>
        [
            (0.00, DrumVoice.Kick), (0.00, DrumVoice.Hat),
            (0.12, DrumVoice.Hat),
            (0.24, DrumVoice.Snare), (0.24, DrumVoice.Hat),
            (0.36, DrumVoice.Hat),
            (0.48, DrumVoice.Kick), (0.48, DrumVoice.Hat),
            (0.60, DrumVoice.Snare), (0.60, DrumVoice.OpenHat),
            (0.72, DrumVoice.Hat),
        ],
        _ =>
        [
            (0.00, DrumVoice.Snare),
            (0.10, DrumVoice.Snare),
            (0.20, DrumVoice.Snare),
            (0.30, DrumVoice.Kick), (0.30, DrumVoice.Snare),
            (0.42, DrumVoice.Snare),
            (0.54, DrumVoice.Kick),
            (0.66, DrumVoice.Kick), (0.66, DrumVoice.Crash),
        ],
    };

    static void Strike(double[] mix, int start, DrumVoice voice, int pattern)
    {
        if (start < 0 || start >= mix.Length)
            return;

        var seed = 1000 * pattern + start + (int)voice * 17;
        switch (voice)
        {
            case DrumVoice.Kick:
                Kick(mix, start);
                break;
            case DrumVoice.Snare:
                Snare(mix, start, seed);
                break;
            case DrumVoice.Hat:
                Hat(mix, start, open: false, seed);
                break;
            case DrumVoice.OpenHat:
                Hat(mix, start, open: true, seed);
                break;
            case DrumVoice.Crash:
                Crash(mix, start, seed);
                break;
        }
    }

    static void Kick(double[] mix, int start)
    {
        var phase = 0.0;
        var length = Math.Min(mix.Length - start, (int)(0.24 * ChordSynth.SampleRate));
        for (var i = 0; i < length; i++)
        {
            var time = (double)i / ChordSynth.SampleRate;
            var frequency = 48 + 150 * Math.Exp(-time * 32);
            phase += 2 * Math.PI * frequency / ChordSynth.SampleRate;
            mix[start + i] += Math.Exp(-time * 16) * Math.Sin(phase);
        }

        SynthMath.AddNoise(mix, start, 180, 0.35, 7, decaySamples: 28);
    }

    static void Snare(double[] mix, int start, int seed)
    {
        var length = Math.Min(mix.Length - start, (int)(0.18 * ChordSynth.SampleRate));
        for (var i = 0; i < length; i++)
        {
            var time = (double)i / ChordSynth.SampleRate;
            var body = Math.Sin(2 * Math.PI * 188 * time) + 0.4 * Math.Sin(2 * Math.PI * 330 * time);
            mix[start + i] += 0.45 * Math.Exp(-time * 28) * body;
        }

        SynthMath.AddNoise(mix, start, length, 0.7, seed, decaySamples: 420);
    }

    static void Hat(double[] mix, int start, bool open, int seed)
    {
        var length = Math.Min(mix.Length - start, (int)(ChordSynth.SampleRate * (open ? 0.20 : 0.045)));
        var random = new Random(seed);
        var previous = 0.0;
        var rate = open ? 14.0 : 70.0;
        for (var i = 0; i < length; i++)
        {
            var white = random.NextDouble() * 2 - 1;
            var high = white - previous;
            previous = white;
            var time = (double)i / ChordSynth.SampleRate;
            mix[start + i] += 0.5 * Math.Exp(-time * rate) * high;
        }
    }

    static void Crash(double[] mix, int start, int seed)
    {
        double[] partials = [317, 469, 588, 812, 1233, 1710, 2488, 3370, 4910];
        var length = mix.Length - start;
        for (var partial = 0; partial < partials.Length; partial++)
        {
            for (var i = 0; i < length; i++)
            {
                var time = (double)i / ChordSynth.SampleRate;
                var amplitude = 0.22 * Math.Exp(-time * (3.2 + partial * 0.35));
                mix[start + i] += amplitude * Math.Sin(2 * Math.PI * partials[partial] * time);
            }
        }

        SynthMath.AddNoise(mix, start, length, 0.25, seed, decaySamples: 5000);
    }

    enum DrumVoice
    {
        Kick,
        Snare,
        Hat,
        OpenHat,
        Crash,
    }
}
