# Writing Docs for FadeBasicCommand Methods

This guide describes how to write XML doc comments for any method decorated with `[FadeBasicCommand]`. The goal is that someone reading only the docs — without looking at the source — can understand what the command does, when to use it, and how it fits into the bigger picture.

## General Rules

When talking about `bool`s, use `1` for `true`, and `0` for `false`.
Never refer to "basic compatability", because the docs are being read from the point of view of a BASIC user.
Never reference C# directly, because the docs are being read from the point of view of a BASIC user.
Don't use the `--` convention. 
Make sure words have proper spaces between them. 
Don't use bare `<` or `>` symbols when not part of an xml tag. Make sure you use `&lt;` and `&gt;` and associated escapes. 

## Structure

Every `[FadeBasicCommand]` method should have the following sections, in order:

### 1. Summary — the quick hit

The `<summary>` tag has two parts:

**First paragraph:** A single sentence that says what the command does. Keep it short enough to scan. This is the thing that shows up in autocomplete tooltips, so it needs to land immediately.

**Second paragraph:** One or two sentences that call out the most important "gotcha" or detail. If there's a precondition, a side effect, or a non-obvious behavior, this is where it goes. If the command is truly straightforward, this paragraph can be skipped.

```xml
/// <summary>
/// Checks whether two colliders are currently overlapping.
///
/// You must call <see cref="DoHitCheck">perform collider checks</see> earlier
/// in the frame for this to return up-to-date results. Without that, you're
/// reading stale hit data from the previous frame.
/// </summary>
```

### 2. Remarks — the domain expert talks

The `<remarks>` section is where you get conversational. Imagine you're sitting next to someone who built the command, and they're explaining the _why_ behind it. This section should cover:

- **When to call it.** Where does this command typically appear in a game loop? Is it a setup-once thing, or called every frame?
- **Why it exists.** What problem does it solve? What would you have to do manually without it?
- **Related commands.** What other commands does this one naturally pair with? What's the typical call sequence? Reference them with `<c>command name</c>` tags so they're visually distinct.
- **Edge cases and gotchas.** What happens if you call it twice? What happens with bad input? Are there limits?

Don't be afraid to write several sentences here. This is the section for people who want to actually understand the system, not just call a function.

```xml
/// <remarks>
/// Collision detection in Fade works in two phases. First, you call
/// <see cref="DoHitCheck">perform collider checks</see> to sweep all active
/// colliders and build up the internal hit list. Then you query specific pairs
/// with <see cref="AreCollidersHitting">get collision</see>. This two-phase
/// design means the expensive broad-phase only runs once per frame, no matter
/// how many pairs you check.
///
/// Colliders on their own don't move — they sit at whatever position you gave
/// them when you called <see cref="CreateBoxCollider">box collider</see>. If
/// you want a collider to track a game object, attach it to a transform with
/// <see cref="AttachColliderToTransform">attach collider to transform</see>.
/// The collision system will read the transform's world position each frame
/// before doing its sweep.
///
/// The order of the two collider IDs doesn't matter — checking (a, b) is the
/// same as checking (b, a).
/// </remarks>
```

### 3. Examples
There should be some code samples showing how to use the command. If the command has valuable counterparts, show those too. 
Make sure to use proper code formatting rules.
Include comments in the code that document the code as well.
Feel free to use multiple examples to illustrate different points.

**Indentation:** Code inside block structures should be indented with two spaces per nesting level. This applies to `DO...LOOP`, `IF...ENDIF`, `FOR...NEXT`, `WHILE...ENDWHILE`, `REPEAT...UNTIL`, and any other block. Nested blocks stack, so code inside an `IF` that is itself inside a `DO` gets four spaces total. Top-level statements outside any block sit at column zero (no indentation).

```
` top-level setup, no indentation
texture 1, "Images/Player"
sprite 1, 100, 200, 1

set sync rate 16
DO
  ` one level in: inside DO...LOOP
  px = px + 1
  IF px &gt; 640
    ` two levels in: inside IF inside DO
    px = 0
  ENDIF
  position sprite 1, px, 200
  sync
LOOP
```

### 4. Related Commands

Add a `<seealso>` tag for every command that this command naturally pairs with. This includes commands that are referenced in the examples, as well as commands that are part of the same workflow or subsystem. Use `<see cref="MethodName">fade basic name</see>` formatting inside each tag.

```xml
/// <seealso cref="DoHitCheck">perform collider checks</seealso>
/// <seealso cref="AreCollidersHitting">get collision</seealso>
/// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
```

Place the `<seealso>` tags after `<returns>` (or after `<param>` if there is no return value), just before the `[FadeBasicCommand]` attribute. The goal is that someone reading the docs can quickly jump to every command in the neighborhood without hunting through prose.

### 5. Params

Every parameter gets a `<param>` tag. Keep these brief — a short phrase is fine. If the parameter has a specific range or set of valid values, mention it here.

```xml
/// <param name="colliderId">The ID of the collider. Must have been created with <c>box collider</c>.</param>
/// <param name="x">The X position of the collider's top-left corner.</param>
```

For packed color values, scan codes, easing types, or other "magic number" parameters, say what the number represents and point to the command that produces it:

```xml
/// <param name="colorCode">A packed RGBA color value. Use <see cref="Rgb">rgb</see> to build one.</param>
/// <param name="scanCode">The key's scan code. Use <see cref="ScanCode">scanCode</see> to convert a key name like "Space" to its code.</param>
```

### 6. Returns

If the method returns a value, include a `<returns>` tag. Say what the value represents and, if relevant, what the range is.

```xml
/// <returns>True if the two colliders are overlapping, false otherwise.</returns>
/// <returns>The tween's progress ratio, from 0.0 (just started) to 1.0 (finished).</returns>
```

## Full Example

Here's a complete example putting it all together:

```csharp
/// <summary>
/// Creates an axis-aligned box collider at the given position and size.
///
/// The collider is static by default — it won't move on its own. Attach it
/// to a transform with
/// <see cref="AttachColliderToTransform">attach collider to transform</see>
/// if you need it to follow a game object.
/// </summary>
/// <remarks>
/// Box colliders are the building blocks of Fade's collision system. You
/// create them, optionally parent them to transforms, and then each frame
/// you call <see cref="DoHitCheck">perform collider checks</see> to find
/// out what's overlapping. After that, use
/// <see cref="AreCollidersHitting">get collision</see> to query specific pairs.
///
/// A typical setup for a game entity looks like this: create a transform
/// with <see cref="CreateTransform">transform</see>, create a sprite with
/// <see cref="Sprite">sprite</see> and attach it via
/// <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>,
/// then create a collider here and attach it with
/// <see cref="AttachColliderToTransform">attach collider to transform</see>.
/// Now moving the transform moves everything together.
///
/// Collider positions are relative to their attached transform (if any).
/// If you set x=<c>0</c>, y=<c>0</c> and attach to a transform, the collider
/// is centered on the transform's origin. Offset x and y to shift it relative
/// to that anchor point.
///
/// There's no limit on the number of colliders you can create, but keep in
/// mind that <see cref="DoHitCheck">perform collider checks</see> is an
/// O(n^2) broad-phase, so hundreds of active colliders will start to cost you.
/// </remarks>
/// <param name="colliderId">The ID to assign to this collider.</param>
/// <param name="x">The X position of the collider.</param>
/// <param name="y">The Y position of the collider.</param>
/// <param name="w">The width of the collider in pixels.</param>
/// <param name="h">The height of the collider in pixels.</param>
/// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
/// <seealso cref="DoHitCheck">perform collider checks</seealso>
/// <seealso cref="AreCollidersHitting">get collision</seealso>
/// <seealso cref="CreateTransform">transform</seealso>
[FadeBasicCommand("box collider")]
public static void CreateBoxCollider(int colliderId, int x, int y, int w, int h)
```

## Cross-Reference Accuracy

When an example references another command, the usage **must match that command's actual signature**. The generated JSON docs (`FadeCommandDocs.md`) are the source of truth for parameter order, parameter types, and whether a command is a statement or a function.

Common mistakes to avoid:

- **Inventing commands that don't exist.** There is no `load image` command — the correct command is `texture textureId, filePath`. Always check the actual command list before using a name in an example.
- **Treating statements as functions.** The `font` command is a statement: `font 1, "Fonts/Arial"`. It is *not* a function call like `fontId = font("arial")`. If a command does not have a return type in the generated docs, it cannot appear on the right-hand side of an assignment.
- **Getting parameter order wrong.** The `text` command takes `textId, x, y, spriteFontId, text$` — five parameters in that exact order. Double-check the generated docs before writing an example that calls another command.

Before writing or reviewing an example, verify each referenced command against the generated docs:
1. Does the command exist by that exact name?
2. Does it return a value (function) or not (statement)?
3. Are the parameters in the right order with the right types?

## Voice

- Write like you're explaining it to a friend who knows how to code but hasn't used Fade before.
- Prefer concrete language ("call this once per frame") over abstract language ("this should be invoked periodically").
- It's fine to say what _not_ to do, or to warn about foot-guns.
- Reference related commands by their FadeBasic name (the string in the attribute), not the C# method name.
- use this as a style reference: https://github.com/cdhanna/fadebasic/blob/main/FadeBasic/book/FadeBook/Language.md
- use these commands as a style reference: https://github.com/Dark-Basic-Software-Limited/Dark-Basic-Pro/tree/Initial-Files/Install/Help/commands

## Referencing Other Commands

When you mention another FadeBasic command inside a doc comment, use the `<see cref="MethodName"/>` tag with the C# method name. This creates a navigable link in IDEs and doc generators. In the display text, use the FadeBasic command name so it reads naturally.

Use `<see>` (not `<c>`) when pointing to another command — it gives you real cross-referencing rather than just monospace styling.

```xml
/// Attach it to a transform with <see cref="AttachColliderToTransform">attach collider to transform</see>
/// if you need it to follow a game object.
```

If you're referencing a command in a different class (or a future overload), use the fully qualified name:

```xml
/// <see cref="FadeMonoGameCommands.CreateBoxCollider">box collider</see>
```

For inline code that is _not_ a command reference (e.g. a parameter value, a literal keyword, or a concept), use `<c>` as usual:

```xml
/// Pass <c>0</c> for textureId to auto-allocate one, or <c>-1</c> to clear it.
```

**Quick rule of thumb:**
- Referring to another `[FadeBasicCommand]` method? Use `<see cref="...">fade basic name</see>`.
- Referring to a value, keyword, or concept? Use `<c>...</c>`.