namespace BZPlaysFoolChords;

public static class SynthMath
{
    public const double DurationSeconds = 0.90;
    public const double Peak = 0.72;

    public static int Length(double seconds = DurationSeconds) => (int)(ChordSynth.SampleRate * seconds);

    public static short[] ToPcm(double[] mixed)
    {
        var peak = 0.0;
        for (var i = 0; i < mixed.Length; i++)
            peak = Math.Max(peak, Math.Abs(mixed[i]));

        var scale = peak < 1e-9 ? 0 : Peak / peak;
        var samples = new short[mixed.Length];
        const int fade = 64;
        for (var i = 0; i < mixed.Length; i++)
        {
            var edge = 1.0;
            if (i < fade)
                edge = (double)i / fade;
            else if (i > mixed.Length - fade)
                edge = Math.Max(0, (mixed.Length - i) / (double)fade);

            var shaped = Math.Clamp(mixed[i] * scale * edge, -1, 1);
            samples[i] = (short)Math.Round(shaped * short.MaxValue);
        }

        if (samples.Length > 0)
            samples[^1] = 0;

        return samples;
    }

    public static void AddSine(double[] mix, double frequency, double amplitude, double decaySeconds, int attackSamples = 0)
    {
        var tau = Math.Max(0.01, decaySeconds);
        for (var i = 0; i < mix.Length; i++)
        {
            var time = (double)i / ChordSynth.SampleRate;
            var envelope = Math.Exp(-time / tau);
            if (i < attackSamples)
                envelope *= (double)i / Math.Max(1, attackSamples);

            mix[i] += amplitude * envelope * Math.Sin(2 * Math.PI * frequency * time);
        }
    }

    public static void AddNoise(double[] mix, int start, int sampleCount, double amplitude, int seed, double decaySamples)
    {
        var random = new Random(seed);
        var tau = Math.Max(1, decaySamples);
        for (var i = 0; i < sampleCount && start + i < mix.Length; i++)
        {
            if (start + i < 0)
                continue;

            var noise = random.NextDouble() * 2 - 1;
            mix[start + i] += amplitude * Math.Exp(-i / tau) * noise;
        }
    }

    public static void Karplus(double[] mix, double frequency, double sustainSeconds, double brightness, int seed, double gain)
    {
        var period = Math.Max(2, (int)Math.Round(ChordSynth.SampleRate / Math.Max(20, frequency)));
        var buffer = new double[period];
        var random = new Random(seed);
        for (var i = 0; i < period; i++)
            buffer[i] = random.NextDouble() * 2 - 1;

        var loss = Math.Exp(-1.0 / (Math.Max(0.05, sustainSeconds) * ChordSynth.SampleRate));
        var index = 0;
        var previous = 0.0;
        for (var i = 0; i < mix.Length; i++)
        {
            var sample = buffer[index];
            var averaged = 0.5 * (sample + previous);
            var feedback = (1 - brightness) * averaged + brightness * sample;
            buffer[index] = loss * feedback;
            previous = sample;
            mix[i] += gain * sample;
            index++;
            if (index == period)
                index = 0;
        }
    }

    public static int Seed(double frequency, int salt) =>
        unchecked((int)Math.Round(frequency * 10) * 397) ^ (salt * 911);
}
