namespace Godot;

public sealed class StringName
{
    public StringName(string text) => Text = text;
    public string Text { get; }
}

public sealed class NodePath
{
    NodePath(string text) => Text = text;
    public string Text { get; }
    public static implicit operator NodePath(string from) => new(from);
}
