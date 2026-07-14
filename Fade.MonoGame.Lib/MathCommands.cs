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
    /// texture 1, "ghost"
    /// t = 0
    /// baseY = 200
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   t = t + 0.05
    ///   ` sin(t) swings between -1 and 1, so y oscillates around baseY
    ///   y = baseY + sin(t) * 30
    ///   sprite 1, 100, y, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Move in a circle using both <see cref="Sin">sin</see> and <see cref="Cos">cos</see>.
    /// <code>
    /// ` orbit a sprite around a center point
    /// texture 1, "ghost"
    /// angle = 0
    /// cx = 320
    /// cy = 240
    /// radius = 80
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   angle = angle + 0.02
    ///   ` cos drives x and sin drives y to trace a circle
    ///   x = cx + cos(angle) * radius
    ///   y = cy + sin(angle) * radius
    ///   sprite 1, x, y, 1
    ///   sync
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
    /// ` arrange 8 sprites evenly in a ring
    /// texture 1, "ghost"
    /// cx = 320
    /// cy = 240
    /// radius = 100
    /// count = 8
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   FOR i = 0 TO count - 1
    ///     angle = rad(360 / count * i)
    ///     ` cos gives the horizontal position on the circle
    ///     x = cx + cos(angle) * radius
    ///     y = cy + sin(angle) * radius
    ///     sprite i + 1, x, y, 1
    ///   NEXT i
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Scale movement speed by facing direction.
    /// <code>
    /// ` move a sprite forward along the direction it is facing
    /// texture 1, "ghost"
    /// facing = rad(45)
    /// speed = 3
    /// px = 100
    /// py = 100
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   ` cos of the facing angle is the horizontal step each frame
    ///   px = px + cos(facing) * speed
    ///   py = py + sin(facing) * speed
    ///   sprite 1, px, py, 1
    ///   sync
    /// LOOP
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
    /// ` rotate a turret sprite to point at the mouse
    /// texture 1, "ghost"
    /// turretX = 320
    /// turretY = 240
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   dx = mouse x() - turretX
    ///   dy = mouse y() - turretY
    ///   ` atan2 returns the correct-quadrant angle (radians) toward the cursor
    ///   angle = atan2(dy, dx)
    ///   sprite 1, turretX, turretY, 1
    ///   rotate sprite 1, angle
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Move an enemy toward the player at a fixed speed.
    /// <code>
    /// ` chase the mouse cursor at a fixed speed
    /// texture 1, "ghost"
    /// enemyX = 100
    /// enemyY = 100
    /// speed = 2
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   dx = mouse x() - enemyX
    ///   dy = mouse y() - enemyY
    ///   ` atan2 gives the heading; cos/sin step along it
    ///   angle = atan2(dy, dx)
    ///   enemyX = enemyX + cos(angle) * speed
    ///   enemyY = enemyY + sin(angle) * speed
    ///   sprite 1, enemyX, enemyY, 1
    ///   sync
    /// LOOP
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
    /// ` find a ramp angle from rise over run and tilt a sprite to match
    /// texture 1, "ghost"
    /// rise = 3
    /// run = 4
    /// slope = rise / run
    /// ` atan turns the slope into an angle in radians (about 0.6435)
    /// angle = atan(slope)
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   sprite 1, 320, 240, 1
    ///   rotate sprite 1, angle
    ///   sync
    /// LOOP
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
    /// ` light up the background when the ghost is near the mouse
    /// texture 1, "ghost"
    /// px = 320
    /// py = 240
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   dx = px - mouse x()
    ///   dy = py - mouse y()
    ///   ` sqrt turns the squared offsets into a real distance
    ///   dist = sqrt(dx * dx + dy * dy)
    ///   IF dist &lt; 50
    ///     ` cursor is close enough to react
    ///     set background color rgb(80, 20, 20)
    ///   ENDIF
    ///   sprite 1, px, py, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Normalize a direction vector to unit length.
    /// <code>
    /// ` move a sprite toward the mouse at constant speed using a unit vector
    /// texture 1, "ghost"
    /// x = 100
    /// y = 100
    /// speed = 3
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   dx = mouse x() - x
    ///   dy = mouse y() - y
    ///   length = sqrt(dx * dx + dy * dy)
    ///   IF length &gt; 0
    ///     ` divide by length to get a unit vector, then scale by speed
    ///     nx = dx / length
    ///     ny = dy / length
    ///     x = x + nx * speed
    ///     y = y + ny * speed
    ///   ENDIF
    ///   sprite 1, x, y, 1
    ///   sync
    /// LOOP
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

    [FadeBasicCommand("abs")]
    public static float Abs(float x) => Math.Abs(x);
    
    [FadeBasicCommand("sign")]
    public static float Sign(float x) => Math.Sign(x);
    
    [FadeBasicCommand("max")]
    public static float Max(float a, float b) => Math.Max(a, b);
    
    [FadeBasicCommand("min")]
    public static float Min(float a, float b) => Math.Min(a, b);

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
    /// ` show a compass label from the angle to the mouse, using deg
    /// texture 1, "ghost"
    /// font 1, "font"
    /// cx = 320
    /// cy = 240
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   dx = mouse x() - cx
    ///   dy = mouse y() - cy
    ///   ` atan2 returns radians; deg makes it easy to read in degrees
    ///   angleDeg = deg(atan2(dy, dx))
    ///   sprite 1, cx, cy, 1
    ///   ` the east half is within 90 degrees of straight right
    ///   IF abs(angleDeg) &lt; 90
    ///     text 1, 20, 20, 1, "EAST"
    ///   ELSE
    ///     text 1, 20, 20, 1, "WEST"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Convert an <see cref="Atan2">atan2</see> result to rotate a sprite.
    /// <code>
    /// ` rotate a sprite toward the mouse, and read the angle with deg
    /// texture 1, "ghost"
    /// ax = 320
    /// ay = 240
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   dx = mouse x() - ax
    ///   dy = mouse y() - ay
    ///   angle = atan2(dy, dx)
    ///   sprite 1, ax, ay, 1
    ///   ` rotate sprite wants radians; deg gives the same angle in degrees
    ///   rotate sprite 1, angle
    ///   angleDeg = deg(angle)
    ///   IF abs(angleDeg) &lt; 10
    ///     ` pointing roughly east
    ///     set background color rgb(20, 60, 20)
    ///   ENDIF
    ///   sync
    /// LOOP
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
    /// ` launch a projectile at 45 degrees using rad
    /// texture 1, "ghost"
    /// angleDeg = 45
    /// ` rad converts degrees into the radians cos/sin expect
    /// angleRad = rad(angleDeg)
    /// speed = 4
    /// velX = cos(angleRad) * speed
    /// velY = sin(angleRad) * speed
    /// bx = 50
    /// by = 50
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   bx = bx + velX
    ///   by = by + velY
    ///   sprite 1, bx, by, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Rotate something by a fixed number of degrees each frame.
    /// <code>
    /// ` spin a sprite around a center, 2 degrees per frame
    /// texture 1, "ghost"
    /// angleDeg = 0
    /// DO
    ///   set background color rgb(20, 20, 40)
    ///   angleDeg = angleDeg + 2
    ///   ` rad converts the running degree count for cos/sin
    ///   x = 320 + cos(rad(angleDeg)) * 100
    ///   y = 240 + sin(rad(angleDeg)) * 100
    ///   sprite 1, x, y, 1
    ///   sync
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