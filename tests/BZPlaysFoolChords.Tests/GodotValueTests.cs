using BZPlaysFoolChords;
using Xunit;

namespace BZPlaysFoolChords.Tests;

public class GodotValueTests
{
    [Fact]
    public void StringBecomesStringNameThroughItsConstructor()
    {
        var adapted = GodotValues.Adapt("FoolChord_C", typeof(Godot.StringName));

        var name = Assert.IsType<Godot.StringName>(adapted);
        Assert.Equal("FoolChord_C", name.Text);
    }

    [Fact]
    public void StringBecomesNodePathThroughTheImplicitOperator()
    {
        var adapted = GodotValues.Adapt("queue_free", typeof(Godot.NodePath));

        var path = Assert.IsType<Godot.NodePath>(adapted);
        Assert.Equal("queue_free", path.Text);
    }

    [Fact]
    public void OrdinaryStringsStayStrings()
    {
        Assert.Equal("C.wav", GodotValues.Adapt("C.wav", typeof(string)));
    }
}
