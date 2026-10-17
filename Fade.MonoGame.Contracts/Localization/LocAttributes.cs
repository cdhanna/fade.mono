namespace Fade.MonoGame.Localization;

/// <summary>
/// Marks the parameter of a command that takes a string table key (<c>loc$</c>, <c>text loc</c>,
/// <c>set text loc</c>, <c>lockey$</c>). The strings build check finds every call to such a
/// command and checks the key passed there. The check matches this attribute by name, so a game
/// can declare its own copy instead of referencing this one.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FadeLocKeyAttribute : Attribute
{
    /// <summary>
    /// When set, the key must be a constant string. <c>lockey$</c> has this, because it is how a
    /// key stored in a variable stays visible to the check.
    /// </summary>
    public bool constantOnly;
}

/// <summary>
/// Marks the name/value pairs that follow a key (<c>"count", n, "fish", "@fish.tuna"</c>), so the
/// build check can compare the arg names with the placeholders in the English text.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FadeLocArgsAttribute : Attribute
{
    /// <summary>
    /// When set, the text is resolved right away (<c>loc$</c>), so every arg it uses must be
    /// passed here. A loc text can be given more later with <c>set text arg</c>, so it is not.
    /// </summary>
    public bool complete;
}
