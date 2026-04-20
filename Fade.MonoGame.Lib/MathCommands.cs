using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Returns the sine of the given angle.</para>
    /// <para>The angle must be in radians. Use <see cref="Rad">rad</see> to convert from degrees first if needed.</para>
    /// </summary>
    /// <remarks>
    /// Standard trig helper. You'll use this alongside <see cref="Cos">cos</see> for
    /// circular motion, wave effects, and oscillation. If you have an angle from
    /// <see cref="Atan2">atan2</see>, you can feed it straight in here since it's
    /// already in radians.
    ///
    /// Passing values outside 0..2*pi is fine. It wraps naturally.
    /// </remarks>
    /// <example>
    /// Move a sprite up and down in a wave pattern using <see cref="Sin">sin</see>.
    /// <code>
    /// ` bob a sprite up and down over time
    /// t = 0
    /// baseY = 200
    /// DO
    ///   t = t + 0.05
    ///   y = baseY + sin(t) * 30
    ///   draw_sprite 1, 100, y
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Move in a circle using both <see cref="Sin">sin</see> and <see cref="Cos">cos</see>.
    /// <code>
    /// ` orbit a point around a center
    /// angle = 0
    /// cx = 320
    /// cy = 240
    /// radius = 80
    /// DO
    ///   angle = angle + 0.02
    ///   x = cx + cos(angle) * radius
    ///   y = cy + sin(angle) * radius
    ///   draw_sprite 1, x, y
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="x">The angle in radians.</param>
    /// <returns>The sine of the angle, in the range <c>-1.0</c> to <c>1.0</c>.</returns>
    /// <seealso cref="Cos">cos</seealso>
    /// <seealso cref="Rad">rad</seealso>
    /// <seealso cref="Deg">deg</seealso>
    /// <seealso cref="Atan2">atan2</seealso>
    [FadeBasicCommand("sin")]
    public static float Sin(float x)
    {
        return (float)Math.Sin(x);
    }

    /// <summary>
    /// <para>Returns the cosine of the given angle.</para>
    /// <para>The angle must be in radians. Use <see cref="Rad">rad</see> to convert from degrees first if needed.</para>
    /// </summary>
    /// <remarks>
    /// Pairs with <see cref="Sin">sin</see> for circular motion and positioning.
    /// A common pattern is <c>x = cos(angle) * radius</c> and <c>y = sin(angle) * radius</c>
    /// to place things on a circle.
    ///
    /// Like all the trig functions here, values outside 0..2*pi wrap naturally.
    /// </remarks>
    /// <example>
    /// Place 8 items evenly around a circle.
    /// <code>
    /// ` arrange 8 sprites in a ring
    /// cx = 320
    /// cy = 240
    /// radius = 100
    /// count = 8
    /// FOR i = 0 TO count - 1
    ///   angle = rad(360 / count * i)
    ///   x = cx + cos(angle) * radius
    ///   y = cy + sin(angle) * radius
    ///   draw_sprite i + 1, x, y
    /// NEXT i
    /// </code>
    /// </example>
    /// <example>
    /// Scale movement speed by facing direction.
    /// <code>
    /// ` move forward in the direction the player is facing
    /// facing = rad(45)
    /// speed = 3
    /// px = px + cos(facing) * speed
    /// py = py + sin(facing) * speed
    /// </code>
    /// </example>
    /// <param name="x">The angle in radians.</param>
    /// <returns>The cosine of the angle, in the range <c>-1.0</c> to <c>1.0</c>.</returns>
    /// <seealso cref="Sin">sin</seealso>
    /// <seealso cref="Rad">rad</seealso>
    /// <seealso cref="Deg">deg</seealso>
    [FadeBasicCommand("cos")]
    public static float Cos(float x)
    {
        return (float)Math.Cos(x);
    }

    /// <summary>
    /// <para>Returns the angle (in radians) whose tangent is <paramref name="y"/>/<paramref name="x"/>.</para>
    /// <para>Unlike <see cref="Atan">atan</see>, this takes both components so it returns the correct quadrant every time.</para>
    /// </summary>
    /// <remarks>
    /// This is the one you want for finding the angle between two points. Given a
    /// direction vector (dx, dy), <c>atan2(dy, dx)</c> gives you the angle you can
    /// feed into <see cref="Sin">sin</see> and <see cref="Cos">cos</see> to move
    /// along that direction.
    ///
    /// The result is in radians. If you need degrees for display, pipe it through
    /// <see cref="Deg">deg</see>. Passing <c>(0, 0)</c> returns <c>0</c>.
    /// </remarks>
    /// <example>
    /// Point a turret sprite toward the mouse cursor.
    /// <code>
    /// ` calculate angle from turret to mouse
    /// dx = mouseX - turretX
    /// dy = mouseY - turretY
    /// angle = atan2(dy, dx)
    /// rotate_sprite 1, deg(angle)
    /// </code>
    /// </example>
    /// <example>
    /// Move an enemy toward the player at a fixed speed.
    /// <code>
    /// ` chase the player
    /// dx = playerX - enemyX
    /// dy = playerY - enemyY
    /// angle = atan2(dy, dx)
    /// speed = 2
    /// enemyX = enemyX + cos(angle) * speed
    /// enemyY = enemyY + sin(angle) * speed
    /// </code>
    /// </example>
    /// <param name="y">The y component of the direction vector.</param>
    /// <param name="x">The x component of the direction vector.</param>
    /// <returns>The angle in radians, in the range <c>-pi</c> to <c>pi</c>.</returns>
    /// <seealso cref="Sin">sin</seealso>
    /// <seealso cref="Cos">cos</seealso>
    /// <seealso cref="Deg">deg</seealso>
    /// <seealso cref="RotateSprite">rotate sprite</seealso>
    [FadeBasicCommand("atan2")]
    public static float Atan2(float y, float x)
    {
        return (float)Math.Atan2(y, x);
    }

    /// <summary>
    /// <para>Returns the arctangent of the given value, in radians.</para>
    /// <para>For finding angles between two points, you almost certainly want <see cref="Atan2">atan2</see> instead. It handles quadrants for you.</para>
    /// </summary>
    /// <remarks>
    /// Plain atan only takes one argument and can't distinguish which quadrant the
    /// angle falls in. It's here for completeness, but <see cref="Atan2">atan2</see>
    /// is what you'll reach for in practice. The result is in radians; convert with
    /// <see cref="Deg">deg</see> if you need degrees.
    /// </remarks>
    /// <example>
    /// Find the angle of a slope from rise over run.
    /// <code>
    /// ` calculate the angle of a ramp
    /// rise = 3
    /// run = 4
    /// slope = rise / run
    /// angle = atan(slope)
    /// angleDeg = deg(angle)
    /// ` angleDeg is about 36.87
    /// </code>
    /// </example>
    /// <param name="x">The tangent value to find the angle for.</param>
    /// <returns>The angle in radians, in the range <c>-pi/2</c> to <c>pi/2</c>.</returns>
    /// <seealso cref="Atan2">atan2</seealso>
    /// <seealso cref="Deg">deg</seealso>
    [FadeBasicCommand("atan")]
    public static float Atan(float x)
    {
        return (float)Math.Atan(x);
    }

    /// <summary>
    /// <para>Returns the square root of the given value.</para>
    /// <para>Passing a negative value returns <c>NaN</c>.</para>
    /// </summary>
    /// <remarks>
    /// Most commonly used for distance calculations. If you have dx and dy between
    /// two points, <c>sqrt(dx*dx + dy*dy)</c> gives you the distance. If you only
    /// need to compare distances (e.g., "is this closer than that?"), you can skip the
    /// sqrt and compare the squared values directly, which is a bit faster.
    ///
    /// Pairs well with <see cref="Atan2">atan2</see> when you need both the distance
    /// and the angle to a target.
    /// </remarks>
    /// <example>
    /// Check if two sprites are within range of each other.
    /// <code>
    /// ` calculate distance between player and enemy
    /// dx = playerX - enemyX
    /// dy = playerY - enemyY
    /// dist = sqrt(dx * dx + dy * dy)
    /// IF dist &lt; 50
    ///   ` enemy is close enough to attack
    ///   take_damage 10
    /// ENDIF
    /// </code>
    /// </example>
    /// <example>
    /// Normalize a direction vector to unit length.
    /// <code>
    /// ` turn a direction into a unit vector
    /// dx = targetX - startX
    /// dy = targetY - startY
    /// length = sqrt(dx * dx + dy * dy)
    /// IF length &gt; 0
    ///   nx = dx / length
    ///   ny = dy / length
    /// ENDIF
    /// </code>
    /// </example>
    /// <param name="x">A non-negative value to take the square root of.</param>
    /// <returns>The square root of <paramref name="x"/>. Returns <c>NaN</c> if <paramref name="x"/> is negative.</returns>
    /// <seealso cref="Atan2">atan2</seealso>
    [FadeBasicCommand("sqrt")]
    public static float Sqrt(float x)
    {
        return (float)Math.Sqrt(x);
    }

    /// <summary>
    /// <para>Converts an angle from radians to degrees.</para>
    /// <para>All trig functions (<see cref="Sin">sin</see>, <see cref="Cos">cos</see>, <see cref="Atan2">atan2</see>, etc.) work in radians, so use this when you need degrees for display or human-friendly output.</para>
    /// </summary>
    /// <remarks>
    /// The inverse of <see cref="Rad">rad</see>. A full circle is <c>360</c> degrees
    /// or roughly <c>6.283</c> radians. If you are doing all your math in radians
    /// (recommended), you may only need this for debug printing or UI display.
    /// </remarks>
    /// <example>
    /// Display the angle to a target in degrees.
    /// <code>
    /// ` show the player what direction the objective is
    /// dx = objectiveX - playerX
    /// dy = objectiveY - playerY
    /// angleRad = atan2(dy, dx)
    /// angleDeg = deg(angleRad)
    /// ` angleDeg is now in 0..360 range for display
    /// </code>
    /// </example>
    /// <example>
    /// Convert an <see cref="Atan2">atan2</see> result to rotate a sprite.
    /// <code>
    /// ` rotate arrow sprite toward the mouse
    /// dx = mouseX - arrowX
    /// dy = mouseY - arrowY
    /// angle = deg(atan2(dy, dx))
    /// rotate_sprite 1, angle
    /// </code>
    /// </example>
    /// <param name="radians">The angle in radians to convert.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    /// <seealso cref="Rad">rad</seealso>
    /// <seealso cref="Atan2">atan2</seealso>
    /// <seealso cref="RotateSprite">rotate sprite</seealso>
    [FadeBasicCommand("deg")]
    public static float Deg(float radians)
    {
        return (float)MathHelper.ToDegrees(radians);
    }

    /// <summary>
    /// <para>Converts an angle from degrees to radians.</para>
    /// <para>Use this to feed degree values into trig functions like <see cref="Sin">sin</see> and <see cref="Cos">cos</see>, which expect radians.</para>
    /// </summary>
    /// <remarks>
    /// The inverse of <see cref="Deg">deg</see>. If you're working with angles that
    /// come from user input or config files in degrees, run them through this before
    /// passing to any trig function. A common pattern:
    /// <c>x = cos(rad(angleDeg)) * radius</c>.
    /// </remarks>
    /// <example>
    /// Fire a bullet at a 45-degree angle.
    /// <code>
    /// ` launch a projectile at 45 degrees
    /// angleDeg = 45
    /// angleRad = rad(angleDeg)
    /// speed = 10
    /// velX = cos(angleRad) * speed
    /// velY = sin(angleRad) * speed
    /// </code>
    /// </example>
    /// <example>
    /// Rotate something by a fixed number of degrees each frame.
    /// <code>
    /// ` spin a sprite 2 degrees per frame
    /// angleDeg = 0
    /// DO
    ///   angleDeg = angleDeg + 2
    ///   x = 320 + cos(rad(angleDeg)) * 100
    ///   y = 240 + sin(rad(angleDeg)) * 100
    ///   draw_sprite 1, x, y
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="degrees">The angle in degrees to convert.</param>
    /// <returns>The equivalent angle in radians.</returns>
    /// <seealso cref="Deg">deg</seealso>
    /// <seealso cref="Sin">sin</seealso>
    /// <seealso cref="Cos">cos</seealso>
    [FadeBasicCommand("rad")]
    public static float Rad(float degrees)
    {
        return (float)MathHelper.ToRadians(degrees);
    }
}