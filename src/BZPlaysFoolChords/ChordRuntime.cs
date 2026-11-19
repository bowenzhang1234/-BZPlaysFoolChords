using System.Globalization;
using System.Reflection;

namespace BZPlaysFoolChords;

static class GameLog
{
    public static void Info(string message) => Write("Info", message);

    public static void Warn(string message) => Write("Warn", message);

    static void Write(string methodName, string message)
    {
        var text = "[BZPlaysFoolChords] " + message;
        try
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var log = assembly.GetType("MegaCrit.Sts2.Core.Logging.Log", throwOnError: false);
                var method = log?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(int)], null);
                if (method == null)
                    continue;

                method.Invoke(null, [text, 2]);
                return;
            }
        }
        catch
        {
            // The game log is optional. Console keeps a trace when the log type is unavailable.
        }

        Console.WriteLine(text);
    }
}

static class GameTypes
{
    public static Type? Find(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(fullName, throwOnError: false);
            if (type != null)
                return type;
        }

        return null;
    }

    public static object? Static(string fullName, string propertyName) =>
        Find(fullName)?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

    public static bool Flag(string fullName, string propertyName)
    {
        var value = Static(fullName, propertyName);
        return value is true;
    }
}

sealed class ChordSession
{
    readonly ChordSettings _settings;
    readonly IInstrument _instrument;
    readonly ProgressionState _state = new();
    readonly GodotChordOutput _output;

    public ChordSession(ChordSettings settings, string audioDirectory, IInstrument instrument)
    {
        _settings = settings;
        _instrument = instrument;
        _output = new GodotChordOutput(audioDirectory, settings.Volume);
    }

    public void ApplySlider(bool master, float slider) => _output.SetSlider(master, slider);

    public bool AllowOriginal(object[]? args)
    {
        if (GameTypes.Flag("MegaCrit.Sts2.Core.Helpers.NonInteractiveMode", "IsActive"))
            return true;

        var combat = GameTypes.Static("MegaCrit.Sts2.Core.Combat.CombatManager", "Instance");
        if (combat?.GetType().GetProperty("IsEnding")?.GetValue(combat) is true)
            return true;

        if (args == null || args.Length == 0 || args[0] is not string sfx)
            return true;

        var callVolume = args.Length > 1 && args[1] is float volume ? volume : 1f;
        var cardId = CardIds.FromStack();
        var decision = _state.Consider(sfx, cardId, _settings, Environment.TickCount64);
        switch (decision.Action)
        {
            case PlayAction.Ignore:
                return true;
            case PlayAction.SuppressOnly:
                return false;
            case PlayAction.PlayChord:
                var step = _settings.Steps[decision.ChordIndex];
                var clip = _instrument.ClipFor(new InstrumentRequest(step, decision.ChordIndex, cardId));
                if (clip == null)
                    return true;

                var played = _output.TryPlay(clip, callVolume);
                if (!played)
                {
                    GameLog.Warn($"Could not play {_instrument.DisplayName} {clip.Label}. Leaving the original melody sound in place.");
                    return true;
                }

                GameLog.Info(string.Create(CultureInfo.InvariantCulture,
                    $"Played {_instrument.DisplayName} {clip.Label} for {(cardId ?? "a melody card")} ({sfx})."));
                return !_settings.ReplaceVanilla;
            default:
                return true;
        }
    }
}

static class ChordRuntime
{
    static bool _started;
    public static ChordSession? Session { get; private set; }

    public static void Start()
    {
        if (_started)
            return;

        _started = true;
        var directory = Path.GetDirectoryName(typeof(ChordRuntime).Assembly.Location) ?? ".";
        var configPath = Path.Combine(directory, "chords.cfg");
        ChordSettings settings;
        if (File.Exists(configPath))
        {
            settings = ChordSettings.Parse(File.ReadAllText(configPath));
        }
        else
        {
            settings = ChordSettings.Parse("");
            GameLog.Warn("chords.cfg was not next to the mod DLL. Using C, G, Am, F.");
        }

        foreach (var warning in settings.Warnings)
            GameLog.Warn(warning);

        var instrument = InstrumentCatalog.Create().Resolve(settings.Instrument, out var instrumentWarning);
        if (instrumentWarning != null)
            GameLog.Warn(instrumentWarning);
        foreach (var warning in instrument.StartupWarnings)
            GameLog.Warn(warning);

        Session = new ChordSession(settings, Path.Combine(directory, "audio"), instrument);
        var (master, sfx) = ReadSliders();
        Session.ApplySlider(master: true, master);
        Session.ApplySlider(master: false, sfx);
        Patch();
        var cards = settings.CardIds.Count == 0 ? "every melody voice" : string.Join(", ", settings.CardIds);
        GameLog.Info($"Ready. Instrument {instrument.DisplayName}, mode {settings.Mode}, cards: {cards}, steps: {string.Join(" | ", settings.Steps.Select(step => step.Label))}.");
    }

    static void Patch()
    {
        var harmony = new HarmonyLib.Harmony("bowenzhang1234.BZPlaysFoolChords");
        var sfx = GameTypes.Find("MegaCrit.Sts2.Core.Commands.SfxCmd");
        var play = sfx?.GetMethod("Play", BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(float)], null);
        if (play == null)
        {
            GameLog.Warn("SfxCmd.Play(string, float) was not found. Melody chords will not play.");
        }
        else
        {
            harmony.Patch(play, prefix: new HarmonyLib.HarmonyMethod(typeof(MelodySfxPatch), nameof(MelodySfxPatch.Prefix)));
        }

        var audio = GameTypes.Find("MegaCrit.Sts2.Core.Nodes.Audio.NAudioManager");
        PatchVolume(harmony, audio, "SetMasterVol", nameof(VolumePatches.Master));
        PatchVolume(harmony, audio, "SetSfxVol", nameof(VolumePatches.Sfx));
    }

    static void PatchVolume(HarmonyLib.Harmony harmony, Type? audio, string methodName, string patchName)
    {
        var method = audio?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, [typeof(float)], null);
        if (method == null)
        {
            GameLog.Warn($"NAudioManager.{methodName} was not found. Chord volume will not follow that slider.");
            return;
        }

        harmony.Patch(method, postfix: new HarmonyLib.HarmonyMethod(typeof(VolumePatches), patchName));
    }

    static (float Master, float Sfx) ReadSliders()
    {
        try
        {
            var save = GameTypes.Static("MegaCrit.Sts2.Core.Saves.SaveManager", "Instance");
            var settings = save?.GetType().GetProperty("SettingsSave")?.GetValue(save);
            if (settings == null)
                return (0.5f, 0.5f);

            var master = Convert.ToSingle(settings.GetType().GetProperty("VolumeMaster")?.GetValue(settings) ?? 0.5f, CultureInfo.InvariantCulture);
            var sfx = Convert.ToSingle(settings.GetType().GetProperty("VolumeSfx")?.GetValue(settings) ?? 0.5f, CultureInfo.InvariantCulture);
            return (master, sfx);
        }
        catch (Exception error)
        {
            GameLog.Warn("Could not read volume settings, using the game defaults: " + error.Message);
            return (0.5f, 0.5f);
        }
    }
}

static class MelodySfxPatch
{
    public static bool Prefix(object[] __args) => ChordRuntime.Session?.AllowOriginal(__args) ?? true;
}

static class VolumePatches
{
    public static void Master(object[] __args)
    {
        if (__args.Length > 0 && __args[0] is float volume)
            ChordRuntime.Session?.ApplySlider(master: true, volume);
    }

    public static void Sfx(object[] __args)
    {
        if (__args.Length > 0 && __args[0] is float volume)
            ChordRuntime.Session?.ApplySlider(master: false, volume);
    }
}
