namespace MegaCrit.Sts2.Core.Modding;

/// <summary>
/// Matches the game's attribute so this project can compile without a local install.
/// At runtime the game remaps the sts2 assembly reference onto its own assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModInitializerAttribute : Attribute
{
    public string initializerMethod;

    public ModInitializerAttribute(string initializerMethod)
    {
        this.initializerMethod = initializerMethod;
    }
}
