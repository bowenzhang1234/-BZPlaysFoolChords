using System.Diagnostics;
using System.Text;

namespace BZPlaysFoolChords;

public static class CardIds
{
    public const string CardNamespace = "MegaCrit.Sts2.Core.Models.Cards";

    public static string ToEntry(string pascalName)
    {
        var name = pascalName.Trim();
        var entry = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                var previous = name[i - 1];
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                    entry.Append('_');
            }

            entry.Append(char.ToUpperInvariant(c));
        }

        return entry.ToString();
    }

    public static string Normalize(string raw)
    {
        var parts = raw.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? "" : string.Join("_", parts.Select(ToEntry));
    }

    public static string? FromStack(StackTrace? trace = null)
    {
        trace ??= new StackTrace();
        foreach (var frame in trace.GetFrames() ?? [])
        {
            var type = frame.GetMethod()?.DeclaringType;
            while (type != null)
            {
                if (type.Namespace == CardNamespace && !type.Name.StartsWith('<'))
                    return ToEntry(type.Name);

                type = type.DeclaringType;
            }
        }

        return null;
    }
}
