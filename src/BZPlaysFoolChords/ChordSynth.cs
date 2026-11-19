using System.Globalization;
using System.Text;

namespace BZPlaysFoolChords;

public static class NoteMath
{
    public static double Frequency(string note)
    {
        if (!TryFrequency(note, out var frequency))
            throw new FormatException($"'{note}' is not a note. Use names like C4, Eb4, or F#3.");

        return frequency;
    }

    public static bool TryFrequency(string note, out double frequency)
    {
        frequency = 0;
        if (string.IsNullOrWhiteSpace(note))
            return false;

        var text = note.Trim();
        if (text.Length < 2)
            return false;

        var letter = char.ToUpperInvariant(text[0]);
        if (letter is < 'A' or > 'G')
            return false;

        var index = 1;
        var accidental = 0;
        if (index < text.Length && text[index] is '#' or 'b')
        {
            accidental = text[index] == '#' ? 1 : -1;
            index++;
        }

        if (index >= text.Length || !int.TryParse(text[index..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var octave))
            return false;

        var semitoneFromC = letter switch
        {
            'C' => 0,
            'D' => 2,
            'E' => 4,
            'F' => 5,
            'G' => 7,
            'A' => 9,
            'B' => 11,
            _ => -1,
        };
        if (semitoneFromC < 0)
            return false;

        var midi = (octave + 1) * 12 + semitoneFromC + accidental;
        frequency = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
        return true;
    }
}

public static class ChordSynth
{
    public const int SampleRate = 44100;
    public const double DurationSeconds = 0.90;
    const double AttackSeconds = 0.02;
    const double DecaySeconds = 0.16;
    const double SustainLevel = 0.62;
    const double ReleaseSeconds = 0.28;
    const double Peak = 0.72;

    public static byte[] RenderWav(IReadOnlyList<string> notes)
    {
        if (notes.Count == 0)
            throw new ArgumentException("A chord needs at least one note.", nameof(notes));

        var frequencies = new double[notes.Count];
        for (var i = 0; i < notes.Count; i++)
            frequencies[i] = NoteMath.Frequency(notes[i]);

        return EncodeWav(Render(frequencies));
    }

    public static short[] Render(IReadOnlyList<double> frequencies)
    {
        var count = (int)(SampleRate * DurationSeconds);
        var mixed = new double[count];
        foreach (var frequency in frequencies)
            AddVoice(mixed, frequency);

        var peak = 0.0;
        for (var i = 0; i < mixed.Length; i++)
            peak = Math.Max(peak, Math.Abs(mixed[i]));

        var scale = peak < 1e-9 ? 0 : Peak / peak;
        var samples = new short[count];
        for (var i = 0; i < count; i++)
        {
            var shaped = mixed[i] * scale * EdgeFade(i, count);
            var clamped = Math.Clamp(shaped, -1, 1);
            samples[i] = (short)Math.Round(clamped * short.MaxValue);
        }

        if (samples.Length > 0)
            samples[^1] = 0;

        return samples;
    }

    public static byte[] EncodeWav(ReadOnlySpan<short> samples)
    {
        var dataBytes = samples.Length * 2;
        var buffer = new byte[44 + dataBytes];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(buffer, 0);
        WriteInt(buffer, 4, 36 + dataBytes);
        Encoding.ASCII.GetBytes("WAVE").CopyTo(buffer, 8);
        Encoding.ASCII.GetBytes("fmt ").CopyTo(buffer, 12);
        WriteInt(buffer, 16, 16);
        WriteShort(buffer, 20, 1);
        WriteShort(buffer, 22, 1);
        WriteInt(buffer, 24, SampleRate);
        WriteInt(buffer, 28, SampleRate * 2);
        WriteShort(buffer, 32, 2);
        WriteShort(buffer, 34, 16);
        Encoding.ASCII.GetBytes("data").CopyTo(buffer, 36);
        WriteInt(buffer, 40, dataBytes);
        for (var i = 0; i < samples.Length; i++)
            WriteShort(buffer, 44 + i * 2, samples[i]);

        return buffer;
    }

    public static short[] DecodeWav(ReadOnlySpan<byte> wav)
    {
        if (wav.Length < 44 || Encoding.ASCII.GetString(wav[..4]) != "RIFF" || Encoding.ASCII.GetString(wav[8..12]) != "WAVE")
            throw new InvalidDataException("Not a WAV file.");

        var data = -1;
        for (var offset = 12; offset + 8 <= wav.Length;)
        {
            var id = Encoding.ASCII.GetString(wav.Slice(offset, 4));
            var size = BitConverter.ToInt32(wav.Slice(offset + 4, 4));
            if (id == "data")
            {
                data = offset + 8;
                break;
            }

            offset += 8 + size;
        }

        if (data < 0 || (wav.Length - data) % 2 != 0)
            throw new InvalidDataException("WAV data chunk is missing.");

        var samples = new short[(wav.Length - data) / 2];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(wav.Slice(data + i * 2, 2));

        return samples;
    }

    static void AddVoice(double[] mixed, double frequency)
    {
        var attack = (int)(SampleRate * AttackSeconds);
        var decay = (int)(SampleRate * DecaySeconds);
        var release = (int)(SampleRate * ReleaseSeconds);
        var sustainStart = Math.Min(mixed.Length, attack + decay);
        var releaseStart = Math.Max(sustainStart, mixed.Length - release);
        for (var i = 0; i < mixed.Length; i++)
        {
            var time = (double)i / SampleRate;
            var envelope = Envelope(i, attack, sustainStart, releaseStart, mixed.Length);
            var fundamental = Math.Sin(2 * Math.PI * frequency * time);
            var second = Math.Sin(2 * Math.PI * frequency * 2 * time);
            var third = Math.Sin(2 * Math.PI * frequency * 3 * time);
            mixed[i] += envelope * (fundamental + 0.18 * second + 0.05 * third);
        }
    }

    static double Envelope(int i, int attack, int sustainStart, int releaseStart, int length)
    {
        if (i < attack)
            return attack == 0 ? 1 : (double)i / attack;

        if (i < sustainStart)
        {
            var t = (double)(i - attack) / Math.Max(1, sustainStart - attack);
            return 1 + (SustainLevel - 1) * t;
        }

        if (i < releaseStart)
            return SustainLevel;

        var releaseT = (double)(i - releaseStart) / Math.Max(1, length - releaseStart);
        return SustainLevel * (1 - releaseT);
    }

    static double EdgeFade(int i, int length)
    {
        const int fade = 64;
        if (i < fade)
            return (double)i / fade;
        if (i > length - fade)
            return Math.Max(0, (length - i) / (double)fade);
        return 1;
    }

    static void WriteInt(byte[] buffer, int offset, int value) =>
        BitConverter.TryWriteBytes(buffer.AsSpan(offset, 4), value);

    static void WriteShort(byte[] buffer, int offset, short value) =>
        BitConverter.TryWriteBytes(buffer.AsSpan(offset, 2), value);
}
