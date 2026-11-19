using System.Collections.Concurrent;
using System.Reflection;

namespace BZPlaysFoolChords;

sealed class GodotChordOutput
{
    readonly string _audioDirectory;
    readonly float _configVolume;
    readonly ConcurrentDictionary<string, object> _streams = new();
    readonly List<LivePlayer> _players = [];
    float _master = 0.5f;
    float _sfx = 0.5f;

    public GodotChordOutput(string audioDirectory, float configVolume)
    {
        _audioDirectory = audioDirectory;
        _configVolume = configVolume;
    }

    public void SetSlider(bool master, float slider)
    {
        var clamped = Math.Clamp(slider, 0f, 1f);
        if (master)
            _master = clamped;
        else
            _sfx = clamped;

        lock (_players)
        {
            for (var i = _players.Count - 1; i >= 0; i--)
            {
                if (!GodotRef.IsAlive(_players[i].Player))
                {
                    _players.RemoveAt(i);
                    continue;
                }

                GodotRef.SetVolume(_players[i].Player, Gain(_players[i].CallVolume));
            }
        }
    }

    public bool TryPlay(InstrumentClip clip, float callVolume)
    {
        var stream = StreamFor(clip);
        var parent = FindParent();
        if (stream == null || parent == null)
            return false;

        var playerType = GameTypes.Find("Godot.AudioStreamPlayer");
        if (playerType == null)
        {
            GameLog.Warn("Godot.AudioStreamPlayer is not loaded.");
            return false;
        }

        var player = Activator.CreateInstance(playerType);
        if (player == null)
            return false;

        playerType.GetProperty("Stream")?.SetValue(player, stream);
        playerType.GetProperty("Name")?.SetValue(player, "FoolChord_" + clip.Label);
        if (!GodotRef.AddChild(parent, player))
            return false;

        var live = new LivePlayer(player, callVolume);
        GodotRef.SetVolume(player, Gain(callVolume));
        GodotRef.WhenFinished(player, () => Release(live));
        lock (_players)
            _players.Add(live);

        if (!GodotRef.Play(player))
        {
            Release(live);
            return false;
        }

        return true;
    }

    object? StreamFor(InstrumentClip clip)
    {
        if (_streams.TryGetValue(clip.CacheKey, out var cached) && GodotRef.IsAlive(cached))
            return cached;

        object? stream = null;
        if (!string.IsNullOrEmpty(clip.WavFile))
        {
            var path = Path.Combine(_audioDirectory, clip.InstrumentId, Path.GetFileName(clip.WavFile));
            if (File.Exists(path))
                stream = GodotRef.LoadWav(path);
        }

        stream ??= GodotRef.FromPcm(clip.Synthesize());
        if (stream != null)
            _streams[clip.CacheKey] = stream;

        return stream;
    }

    void Release(LivePlayer live)
    {
        lock (_players)
            _players.Remove(live);

        GodotRef.QueueFree(live.Player);
    }

    float Gain(float callVolume) =>
        Math.Clamp(callVolume, 0f, 4f) * _configVolume * _master * _master * _sfx * _sfx;

    static object? FindParent()
    {
        foreach (var candidate in new[]
        {
            GameTypes.Static("MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom", "Instance"),
            GameTypes.Static("MegaCrit.Sts2.Core.Nodes.NGame", "Instance"),
            SceneRoot(),
        })
        {
            if (candidate != null && GodotRef.IsAlive(candidate) && GodotRef.HasAddChild(candidate))
                return candidate;
        }

        GameLog.Warn("No combat node was available to host the chord player.");
        return null;
    }

    static object? SceneRoot()
    {
        var loop = GameTypes.Find("Godot.Engine")?.GetMethod("GetMainLoop", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        return loop?.GetType().GetProperty("Root")?.GetValue(loop);
    }

    sealed record LivePlayer(object Player, float CallVolume);
}

static class GodotRef
{
    public static bool IsAlive(object node)
    {
        var valid = GameTypes.Find("Godot.GodotObject")?.GetMethod("IsInstanceValid", BindingFlags.Public | BindingFlags.Static);
        if (valid == null)
            return true;

        return valid.Invoke(null, [node]) is not false;
    }

    public static bool HasAddChild(object node) =>
        node.GetType().GetMethods().Any(method => method.Name == "AddChild" && method.GetParameters().Length > 0);

    public static object? LoadWav(string path)
    {
        var wavType = GameTypes.Find("Godot.AudioStreamWav");
        var load = wavType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "LoadFromFile");
        if (load == null)
            return null;

        var args = new object?[load.GetParameters().Length];
        args[0] = path;
        for (var i = 1; i < args.Length; i++)
            args[i] = load.GetParameters()[i].HasDefaultValue ? load.GetParameters()[i].DefaultValue : null;

        return load.Invoke(null, args);
    }

    public static object? FromPcm(short[] samples)
    {
        var wavType = GameTypes.Find("Godot.AudioStreamWav");
        if (wavType == null)
            return null;

        var wav = Activator.CreateInstance(wavType);
        if (wav == null)
            return null;

        var pcm = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, pcm, 0, pcm.Length);
        SetEnum(wav, "Format", "Format16Bits", 1);
        SetEnum(wav, "LoopMode", "Disabled", 0);
        wavType.GetProperty("MixRate")?.SetValue(wav, ChordSynth.SampleRate);
        wavType.GetProperty("Stereo")?.SetValue(wav, false);
        var data = wavType.GetProperty("Data");
        if (data == null || !data.CanWrite || data.PropertyType != typeof(byte[]))
        {
            var temp = Path.Combine(Path.GetTempPath(), "fool-chord-" + Guid.NewGuid().ToString("N") + ".wav");
            File.WriteAllBytes(temp, ChordSynth.EncodeWav(samples));
            return LoadWav(temp);
        }

        data.SetValue(wav, pcm);
        return wav;
    }

    public static bool AddChild(object parent, object child)
    {
        var add = parent.GetType().GetMethods()
            .Where(method => method.Name == "AddChild" && method.GetParameters().Length > 0 &&
                method.GetParameters()[0].ParameterType.IsInstanceOfType(child))
            .OrderBy(method => method.GetParameters().Length)
            .FirstOrDefault();
        if (add == null)
            return false;

        var parameters = add.GetParameters();
        var args = new object?[parameters.Length];
        args[0] = child;
        for (var i = 1; i < parameters.Length; i++)
        {
            args[i] = parameters[i].HasDefaultValue
                ? parameters[i].DefaultValue
                : parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
        }

        add.Invoke(parent, args);
        return true;
    }

    public static void SetVolume(object player, float linear)
    {
        var type = player.GetType();
        var linearProperty = type.GetProperty("VolumeLinear");
        if (linearProperty != null && linearProperty.CanWrite)
        {
            linearProperty.SetValue(player, linear);
            return;
        }

        var db = type.GetProperty("VolumeDb");
        if (db != null && db.CanWrite)
            db.SetValue(player, linear <= 0.0001f ? -80f : (float)(20.0 * Math.Log10(linear)));
    }

    public static bool Play(object player)
    {
        var play = player.GetType().GetMethods().FirstOrDefault(method => method.Name == "Play");
        if (play == null)
            return false;

        var parameters = play.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            args[i] = parameters[i].HasDefaultValue
                ? parameters[i].DefaultValue
                : parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
        }

        play.Invoke(player, args);
        return true;
    }

    public static void WhenFinished(object player, Action release)
    {
        var finished = player.GetType().GetEvent("Finished");
        if (finished == null)
            return;

        try
        {
            var holder = new FinishHook(release);
            var handler = Delegate.CreateDelegate(finished.EventHandlerType!, holder, nameof(FinishHook.OnFinished));
            finished.AddEventHandler(player, handler);
        }
        catch (Exception error)
        {
            GameLog.Warn("Could not hook chord playback cleanup: " + error.Message);
        }
    }

    public static void QueueFree(object player)
    {
        var queue = player.GetType().GetMethod("QueueFree", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (queue != null)
        {
            queue.Invoke(player, null);
            return;
        }

        player.GetType().GetMethod("CallDeferred", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(player, ["queue_free"]);
    }

    static void SetEnum(object target, string propertyName, string name, int fallback)
    {
        var property = target.GetType().GetProperty(propertyName);
        if (property == null || !property.CanWrite)
            return;

        object value = property.PropertyType.IsEnum && Enum.GetNames(property.PropertyType).Contains(name)
            ? Enum.Parse(property.PropertyType, name)
            : Enum.ToObject(property.PropertyType, fallback);
        property.SetValue(target, value);
    }

    sealed class FinishHook(Action release)
    {
        public void OnFinished() => release();
    }
}
