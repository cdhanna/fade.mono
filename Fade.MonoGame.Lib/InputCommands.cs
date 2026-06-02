using System;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{


    /// <summary>
    /// <para>Returns the mouse X position in render-buffer coordinates.</para>
    /// <para>This accounts for any offset or scaling between the OS window and the actual
    /// render area, so you always get coordinates that match your game's internal resolution.</para>
    /// </summary>
    /// <remarks>
    /// If your window size and render size differ (e.g., a 320x240 render buffer in an
    /// 800x600 window), the mouse position is automatically mapped into render space. This
    /// means you can compare the result directly against sprite positions without doing any
    /// math yourself.
    ///
    /// Read this every frame after <see cref="Sync(VirtualMachine)">sync</see> to get fresh
    /// input. Pairs with <see cref="GetMouseY">mouse y</see> to get the full cursor position.
    /// </remarks>
    /// <example>
    /// Track the mouse and position a cursor sprite on it each frame:
    /// <code>
    /// ` load a cursor texture and create a sprite for it
    /// texture 1, "Images/Cursor"
    /// sprite 1, 0, 0, 1
    ///
    /// DO
    ///   mx = mouse x()
    ///   my = mouse y()
    ///   sprite 1, mx, my, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The mouse X position in render-space pixels.</returns>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="IsLeftMouse">left click</seealso>
    /// <seealso cref="IsRightMouse">right click</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("mouse x")]
    public static int GetMouseX()
    {
        var v = (float)InputSystem.mouseState.X;

        v -= RenderSystem.mainBufferPosition.X;

        v /= GameSystem.graphicsDeviceManager.PreferredBackBufferWidth - RenderSystem.mainBufferPosition.X*2;

        v *= RenderSystem.mainBuffer.Width;

        return (int)v;

    }

    /// <summary>
    /// <para>Returns the mouse Y position in render-buffer coordinates.</para>
    /// <para>This accounts for any offset or scaling between the OS window and the actual
    /// render area, so you always get coordinates that match your game's internal resolution.</para>
    /// </summary>
    /// <remarks>
    /// If your window size and render size differ, the mouse position is automatically
    /// mapped into render space. This means you can compare the result directly against
    /// sprite positions without doing any math yourself.
    ///
    /// Read this every frame after <see cref="Sync(VirtualMachine)">sync</see> to get fresh
    /// input. Pairs with <see cref="GetMouseX">mouse x</see> to get the full cursor position.
    /// </remarks>
    /// <example>
    /// Check if the mouse is inside a rectangular region:
    /// <code>
    /// ` define a button area
    /// btnX = 100
    /// btnY = 200
    /// btnW = 120
    /// btnH = 40
    ///
    /// DO
    ///   mx = mouse x()
    ///   my = mouse y()
    ///
    ///   ` check if mouse is inside the button
    ///   IF mx &gt;= btnX AND mx &lt;= btnX + btnW
    ///     IF my &gt;= btnY AND my &lt;= btnY + btnH
    ///       text 10, 10, "Hovering over button!"
    ///     ENDIF
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns>The mouse Y position in render-space pixels.</returns>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="IsLeftMouse">left click</seealso>
    /// <seealso cref="IsRightMouse">right click</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("mouse y")]
    public static int GetMouseY()
    {
        var v = (float)InputSystem.mouseState.Y;

        v -= RenderSystem.mainBufferPosition.Y;

        v /= GameSystem.graphicsDeviceManager.PreferredBackBufferHeight - RenderSystem.mainBufferPosition.Y*2;

        v *= RenderSystem.mainBuffer.Height;

        return (int)v;
    }

    /// <summary>
    /// <para>Returns <c>1</c> while the left mouse button is held down.</para>
    /// <para>This fires every frame the button is pressed, not just the first one. Use
    /// <see cref="IsNewLeftMouse">new left click</see> if you only want to detect the
    /// initial press.</para>
    /// </summary>
    /// <remarks>
    /// Good for continuous actions like dragging, holding to charge, or painting. If you
    /// need a one-shot click (e.g., pressing a button in a menu), use
    /// <see cref="IsNewLeftMouse">new left click</see> instead, because otherwise the
    /// action will fire every frame the player holds the button.
    /// </remarks>
    /// <example>
    /// Draw a trail of dots while the player holds the left mouse button:
    /// <code>
    /// DO
    ///   IF left click() = 1
    ///     mx = mouse x()
    ///     my = mouse y()
    ///     dot mx, my
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Hold the left button to charge a power meter:
    /// <code>
    /// power = 0
    /// maxPower = 100
    ///
    /// DO
    ///   IF left click() = 1
    ///     IF power &lt; maxPower
    ///       power = power + 1
    ///     ENDIF
    ///   ELSE
    ///     power = 0
    ///   ENDIF
    ///
    ///   text 10, 10, "Power: " + str$(power)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> while the left button is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsNewLeftMouse">new left click</seealso>
    /// <seealso cref="IsRightMouse">right click</seealso>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("left click")]
    public static bool IsLeftMouse()
    {
        return InputSystem.mouseState.LeftButton == ButtonState.Pressed;
    }

    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame the left mouse button is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the player keeps
    /// holding the button. The player must release and press again to trigger it.</para>
    /// </summary>
    /// <remarks>
    /// This is edge detection: it fires once per press, not continuously. Use this for
    /// discrete actions like clicking a menu button, selecting a tile, or firing a single
    /// shot. If you need to detect a held button (e.g., dragging), use
    /// <see cref="IsLeftMouse">left click</see> instead.
    /// </remarks>
    /// <example>
    /// Click a button to start the game:
    /// <code>
    /// btnX = 100
    /// btnY = 200
    /// btnW = 120
    /// btnH = 40
    /// started = 0
    ///
    /// DO
    ///   mx = mouse x()
    ///   my = mouse y()
    ///
    ///   IF started = 0
    ///     text btnX + 10, btnY + 10, "Start Game"
    ///
    ///     ` only fires once per click, so we won't skip frames
    ///     IF new left click() = 1
    ///       IF mx &gt;= btnX AND mx &lt;= btnX + btnW
    ///         IF my &gt;= btnY AND my &lt;= btnY + btnH
    ///           started = 1
    ///         ENDIF
    ///       ENDIF
    ///     ENDIF
    ///   ELSE
    ///     text 10, 10, "Game is running!"
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the left button transitioned from released to pressed.</returns>
    /// <seealso cref="IsLeftMouse">left click</seealso>
    /// <seealso cref="IsRightMouse">right click</seealso>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new left click")]
    public static bool IsNewLeftMouse()
    {
        return InputSystem.mouseState.LeftButton == ButtonState.Pressed && InputSystem.oldMouseState.LeftButton == ButtonState.Released;
    }
    
    [FadeBasicCommand("new right click")]
    public static bool IsNewRightMouse()
    {
        return InputSystem.mouseState.RightButton == ButtonState.Pressed && InputSystem.oldMouseState.RightButton == ButtonState.Released;
    }

    /// <summary>
    /// <para>Returns <c>1</c> while the right mouse button is held down.</para>
    /// <para>This fires every frame the button is pressed. There is currently no
    /// <c>new right click</c> command, so use
    /// <see cref="IsNewKeyPressed">new key down</see> with the right mouse scan code if
    /// you need edge detection for the right button.</para>
    /// </summary>
    /// <remarks>
    /// Works the same as <see cref="IsLeftMouse">left click</see> but for the right button.
    /// Good for secondary actions like context menus, alternate fire, or camera controls.
    /// </remarks>
    /// <example>
    /// Use right click to place a waypoint at the mouse position:
    /// <code>
    /// wpX = 0
    /// wpY = 0
    /// hasWaypoint = 0
    ///
    /// DO
    ///   IF right click() = 1
    ///     wpX = mouse x()
    ///     wpY = mouse y()
    ///     hasWaypoint = 1
    ///   ENDIF
    ///
    ///   IF hasWaypoint = 1
    ///     text wpX, wpY, "X"
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> while the right button is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsLeftMouse">left click</seealso>
    /// <seealso cref="IsNewLeftMouse">new left click</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("right click")]
    public static bool IsRightMouse()
    {
        return InputSystem.mouseState.RightButton == ButtonState.Pressed;
    }

    /// <summary>
    /// <para>Returns <c>1</c> if the up arrow key is currently held down, <c>0</c> otherwise.</para>
    /// <para>This is a convenience wrapper. For a more general approach, use
    /// <see cref="IsKeyPressed">key down</see> with
    /// <see cref="ScanCode">scanCode</see> to check any key.</para>
    /// </summary>
    /// <remarks>
    /// You can use the result directly in arithmetic (e.g., multiply it by a speed value).
    /// The "new" variant <see cref="upKeyNew">new upkey</see> fires only on the first frame.
    /// </remarks>
    /// <example>
    /// Move a sprite up and down with the arrow keys:
    /// <code>
    /// ` load a player texture and create a sprite for it
    /// texture 1, "Images/Player"
    /// sprite 1, 160, 120, 1
    /// px = 160
    /// py = 120
    /// speed = 3
    ///
    /// DO
    ///   ` subtract upkey to move up, add downkey to move down
    ///   py = py - upkey() * speed
    ///   py = py + downkey() * speed
    ///
    ///   sprite 1, px, py, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if the up arrow is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="downKey">downkey</seealso>
    /// <seealso cref="upKeyNew">new upkey</seealso>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("upkey")]
    public static int upKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Up) ? 1 : 0;
    }

    /// <summary>
    /// <para>Returns <c>1</c> if the down arrow key is currently held down, <c>0</c> otherwise.</para>
    /// <para>This is a convenience wrapper. For a more general approach, use
    /// <see cref="IsKeyPressed">key down</see> with
    /// <see cref="ScanCode">scanCode</see> to check any key.</para>
    /// </summary>
    /// <remarks>
    /// Pairs with <see cref="upKey">upkey</see>
    /// for vertical movement. The "new" variant <see cref="downKeyNew">new downkey</see>
    /// fires only on the first frame.
    /// </remarks>
    /// <example>
    /// Scroll a camera offset down while the key is held:
    /// <code>
    /// camY = 0
    /// scrollSpeed = 2
    ///
    /// DO
    ///   camY = camY + downkey() * scrollSpeed
    ///   camY = camY - upkey() * scrollSpeed
    ///
    ///   text 10, 10, "Camera Y: " + str$(camY)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if the down arrow is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="upKey">upkey</seealso>
    /// <seealso cref="downKeyNew">new downkey</seealso>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("downkey")]
    public static int downKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Down) ? 1 : 0;
    }

    /// <summary>
    /// <para>Returns <c>1</c> if the right arrow key is currently held down, <c>0</c> otherwise.</para>
    /// <para>This is a convenience wrapper. For a more general approach, use
    /// <see cref="IsKeyPressed">key down</see> with
    /// <see cref="ScanCode">scanCode</see> to check any key.</para>
    /// </summary>
    /// <remarks>
    /// Pairs with <see cref="leftKey">leftKey</see>
    /// for horizontal movement. The "new" variant <see cref="rightKeyNew">new rightKey</see>
    /// fires only on the first frame.
    /// </remarks>
    /// <example>
    /// Move a character left and right with arrow keys:
    /// <code>
    /// px = 160
    /// speed = 4
    ///
    /// DO
    ///   px = px + rightKey() * speed
    ///   px = px - leftKey() * speed
    ///
    ///   text px, 120, "@"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if the right arrow is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="leftKey">leftKey</seealso>
    /// <seealso cref="rightKeyNew">new rightKey</seealso>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("rightKey")]
    public static int rightKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Right) ? 1 : 0;
    }

    /// <summary>
    /// <para>Returns <c>1</c> if the left arrow key is currently held down, <c>0</c> otherwise.</para>
    /// <para>This is a convenience wrapper. For a more general approach, use
    /// <see cref="IsKeyPressed">key down</see> with
    /// <see cref="ScanCode">scanCode</see> to check any key.</para>
    /// </summary>
    /// <remarks>
    /// Pairs with <see cref="rightKey">rightKey</see>
    /// for horizontal movement. The "new" variant <see cref="leftKeyNew">new leftKey</see>
    /// fires only on the first frame.
    /// </remarks>
    /// <example>
    /// Full four-direction movement using all arrow keys:
    /// <code>
    /// ` load a player texture and create a sprite for it
    /// texture 1, "Images/Player"
    /// sprite 1, 160, 120, 1
    /// px = 160
    /// py = 120
    /// speed = 3
    ///
    /// DO
    ///   px = px + rightKey() * speed
    ///   px = px - leftKey() * speed
    ///   py = py + downkey() * speed
    ///   py = py - upkey() * speed
    ///
    ///   sprite 1, px, py, 1
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if the left arrow is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="rightKey">rightKey</seealso>
    /// <seealso cref="upKey">upkey</seealso>
    /// <seealso cref="downKey">downkey</seealso>
    /// <seealso cref="leftKeyNew">new leftKey</seealso>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("leftKey")]
    public static int leftKey()
    {
        if (InputSystem.keyboardState.IsKeyDown(Keys.Left))
        {

        }
        return InputSystem.keyboardState.IsKeyDown(Keys.Left) ? 1 : 0;
    }
    /// <summary>
    /// <para>Returns <c>1</c> if the space bar is currently held down, <c>0</c> otherwise.</para>
    /// <para>This is a convenience wrapper. For a more general approach, use
    /// <see cref="IsKeyPressed">key down</see> with
    /// <see cref="ScanCode">scanCode</see> to check any key.</para>
    /// </summary>
    /// <remarks>
    /// The "new" variant
    /// <see cref="spaceKeyNew">new spaceKey</see> fires only on the first frame.
    /// </remarks>
    /// <example>
    /// Hold space to boost speed:
    /// <code>
    /// px = 0
    /// baseSpeed = 2
    /// boostSpeed = 6
    ///
    /// DO
    ///   ` pick speed based on whether space is held
    ///   IF spaceKey() = 1
    ///     speed = boostSpeed
    ///   ELSE
    ///     speed = baseSpeed
    ///   ENDIF
    ///
    ///   px = px + rightKey() * speed
    ///   px = px - leftKey() * speed
    ///
    ///   text px, 120, ">"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> if space is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="spaceKeyNew">new spaceKey</seealso>
    /// <seealso cref="rightKey">rightKey</seealso>
    /// <seealso cref="leftKey">leftKey</seealso>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("spaceKey")]
    public static int spaceKey()
    {
        return InputSystem.keyboardState.IsKeyDown(Keys.Space) ? 1 : 0;
    }



    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame the up arrow is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the key is still held.
    /// The player must release and press again to trigger it.</para>
    /// </summary>
    /// <remarks>
    /// Edge detection variant of <see cref="upKey">upkey</see>. Use this for discrete
    /// actions like menu navigation where you want one step per press, not continuous
    /// scrolling. For the general-purpose version, use
    /// <see cref="IsNewKeyPressed">new key down</see> with a scan code.
    /// </remarks>
    /// <example>
    /// Navigate a menu with up and down arrow keys (one step per press):
    /// <code>
    /// menuIndex = 0
    /// menuCount = 3
    ///
    /// DO
    ///   ` move selection up
    ///   IF new upkey() = 1
    ///     menuIndex = menuIndex - 1
    ///     IF menuIndex &lt; 0
    ///       menuIndex = menuCount - 1
    ///     ENDIF
    ///   ENDIF
    ///
    ///   ` move selection down
    ///   IF new downkey() = 1
    ///     menuIndex = menuIndex + 1
    ///     IF menuIndex &gt;= menuCount
    ///       menuIndex = 0
    ///     ENDIF
    ///   ENDIF
    ///
    ///   ` draw menu items
    ///   FOR i = 0 TO menuCount - 1
    ///     IF i = menuIndex
    ///       text 20, 40 + i * 20, "> Option " + str$(i)
    ///     ELSE
    ///       text 20, 40 + i * 20, "  Option " + str$(i)
    ///     ENDIF
    ///   NEXT i
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the up arrow transitioned from released to pressed.</returns>
    /// <seealso cref="upKey">upkey</seealso>
    /// <seealso cref="downKeyNew">new downkey</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new upkey")]
    public static bool upKeyNew()
    {
        return IsNewKeyPressed((int)Keys.Up);
    }

    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame the down arrow is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the key is still held.</para>
    /// </summary>
    /// <remarks>
    /// Edge detection variant of <see cref="downKey">downkey</see>. Pairs with
    /// <see cref="upKeyNew">new upkey</see> for menu navigation. For the general-purpose
    /// version, use <see cref="IsNewKeyPressed">new key down</see> with a scan code.
    /// </remarks>
    /// <example>
    /// Step through a list of items one at a time:
    /// <code>
    /// selected = 0
    /// total = 5
    ///
    /// DO
    ///   IF new downkey() = 1
    ///     IF selected &lt; total - 1
    ///       selected = selected + 1
    ///     ENDIF
    ///   ENDIF
    ///
    ///   text 10, 10, "Selected: " + str$(selected) + " of " + str$(total)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the down arrow transitioned from released to pressed.</returns>
    /// <seealso cref="downKey">downkey</seealso>
    /// <seealso cref="upKeyNew">new upkey</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new downkey")]
    public static bool downKeyNew()
    {        return IsNewKeyPressed((int)Keys.Down);

    }
    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame the right arrow is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the key is still held.</para>
    /// </summary>
    /// <remarks>
    /// Edge detection variant of <see cref="rightKey">rightKey</see>. Pairs with
    /// <see cref="leftKeyNew">new leftKey</see> for horizontal menu navigation. For the
    /// general-purpose version, use <see cref="IsNewKeyPressed">new key down</see> with a
    /// scan code.
    /// </remarks>
    /// <example>
    /// Cycle through tabs with left and right arrows:
    /// <code>
    /// tab = 0
    /// tabCount = 4
    ///
    /// DO
    ///   IF new rightKey() = 1
    ///     tab = tab + 1
    ///     IF tab &gt;= tabCount
    ///       tab = 0
    ///     ENDIF
    ///   ENDIF
    ///
    ///   IF new leftKey() = 1
    ///     tab = tab - 1
    ///     IF tab &lt; 0
    ///       tab = tabCount - 1
    ///     ENDIF
    ///   ENDIF
    ///
    ///   text 10, 10, "Tab: " + str$(tab)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the right arrow transitioned from released to pressed.</returns>
    /// <seealso cref="rightKey">rightKey</seealso>
    /// <seealso cref="leftKeyNew">new leftKey</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new rightKey")]
    public static bool rightKeyNew()
    {        return IsNewKeyPressed((int)Keys.Right);

    }
    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame the left arrow is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the key is still held.</para>
    /// </summary>
    /// <remarks>
    /// Edge detection variant of <see cref="leftKey">leftKey</see>. Pairs with
    /// <see cref="rightKeyNew">new rightKey</see> for horizontal menu navigation. For the
    /// general-purpose version, use <see cref="IsNewKeyPressed">new key down</see> with a
    /// scan code.
    /// </remarks>
    /// <example>
    /// Go back one page in a book viewer:
    /// <code>
    /// page = 0
    /// maxPage = 10
    ///
    /// DO
    ///   IF new leftKey() = 1
    ///     IF page &gt; 0
    ///       page = page - 1
    ///     ENDIF
    ///   ENDIF
    ///
    ///   IF new rightKey() = 1
    ///     IF page &lt; maxPage
    ///       page = page + 1
    ///     ENDIF
    ///   ENDIF
    ///
    ///   text 10, 10, "Page " + str$(page) + " of " + str$(maxPage)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the left arrow transitioned from released to pressed.</returns>
    /// <seealso cref="leftKey">leftKey</seealso>
    /// <seealso cref="rightKeyNew">new rightKey</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new leftKey")]
    public static bool leftKeyNew()
    {        return IsNewKeyPressed((int)Keys.Left);

    }
    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame the space bar is pressed.</para>
    /// <para>After that first frame it returns <c>0</c>, even if the key is still held.</para>
    /// </summary>
    /// <remarks>
    /// Edge detection variant of <see cref="spaceKey">spaceKey</see>. Use this for actions
    /// like jumping or confirming a selection where you want one action per press. For the
    /// general-purpose version, use <see cref="IsNewKeyPressed">new key down</see> with a
    /// scan code.
    /// </remarks>
    /// <example>
    /// Press space to jump (one jump per press):
    /// <code>
    /// py = 200
    /// vy = 0
    /// gravity = 1
    /// ground = 200
    ///
    /// DO
    ///   ` start a jump only on the first frame space is pressed
    ///   IF new spaceKey() = 1
    ///     IF py &gt;= ground
    ///       vy = -12
    ///     ENDIF
    ///   ENDIF
    ///
    ///   ` apply gravity
    ///   vy = vy + gravity
    ///   py = py + vy
    ///
    ///   ` land on the ground
    ///   IF py &gt; ground
    ///     py = ground
    ///     vy = 0
    ///   ENDIF
    ///
    ///   text 160, py, "O"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <returns><c>1</c> on the frame the space bar transitioned from released to pressed.</returns>
    /// <seealso cref="spaceKey">spaceKey</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new spaceKey")]
    public static bool spaceKeyNew()
    {        return IsNewKeyPressed((int)Keys.Space);

    }

    /// <summary>
    /// <para>Returns <c>1</c> only on the first frame a key is pressed.</para>
    /// <para>This is the general-purpose edge detection command. It works with any key
    /// via its scan code. The convenience wrappers like <see cref="upKeyNew">new upkey</see>
    /// call this under the hood.</para>
    /// </summary>
    /// <remarks>
    /// Use this when you need to detect a fresh press for a key that doesn't have its own
    /// convenience command. Get the scan code with <see cref="ScanCode">scanCode</see>,
    /// for example, <c>scanCode("A")</c> gives you the code for the A key.
    ///
    /// This detects the transition from released to pressed. Once the key is held, it
    /// returns <c>0</c> on subsequent frames. The player has to release and press again
    /// to trigger it. For continuous held-key detection, use
    /// <see cref="IsKeyPressed">key down</see> instead.
    /// </remarks>
    /// <example>
    /// Press E to interact with something:
    /// <code>
    /// ` get the scan code for E once at startup
    /// eKey = scanCode("E")
    ///
    /// DO
    ///   IF new key down(eKey) = 1
    ///     text 10, 10, "Interacted!"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Press Escape to toggle a pause menu:
    /// <code>
    /// escKey = scanCode("Escape")
    /// paused = 0
    ///
    /// DO
    ///   IF new key down(escKey) = 1
    ///     IF paused = 0
    ///       paused = 1
    ///     ELSE
    ///       paused = 0
    ///     ENDIF
    ///   ENDIF
    ///
    ///   IF paused = 1
    ///     text 100, 100, "PAUSED"
    ///   ELSE
    ///     text 100, 100, "Playing..."
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="scanCode">The scan code of the key. Use <see cref="ScanCode">scanCode</see> to convert a name like <c>"Space"</c> to its code.</param>
    /// <returns><c>1</c> on the frame the key transitioned from released to pressed.</returns>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="upKeyNew">new upkey</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("new key down")]
    public static bool IsNewKeyPressed(int scanCode)
    {
        var keyDown = !InputSystem.oldKeyboardState.IsKeyDown((Keys)scanCode) && InputSystem.keyboardState.IsKeyDown((Keys)scanCode);
        return keyDown;
    }

    /// <summary>
    /// <para>Returns <c>1</c> while a key is held down.</para>
    /// <para>This fires every frame the key is pressed, not just the first one. Use
    /// <see cref="IsNewKeyPressed">new key down</see> if you only want the initial press.</para>
    /// </summary>
    /// <remarks>
    /// This is the general-purpose held-key detection command. It works with any key via
    /// its scan code. Get the code with <see cref="ScanCode">scanCode</see>, for example,
    /// <c>scanCode("LeftShift")</c> for the left shift key.
    ///
    /// Good for continuous actions like movement, sprinting, or camera control where you
    /// want the action to keep going as long as the key is held. The convenience wrappers
    /// like <see cref="upKey">upkey</see> do the same thing but are limited to specific keys.
    /// </remarks>
    /// <example>
    /// WASD movement using scan codes:
    /// <code>
    /// ` look up scan codes once at startup
    /// wKey = scanCode("W")
    /// aKey = scanCode("A")
    /// sKey = scanCode("S")
    /// dKey = scanCode("D")
    ///
    /// px = 160
    /// py = 120
    /// speed = 3
    ///
    /// DO
    ///   py = py - key down(wKey) * speed
    ///   py = py + key down(sKey) * speed
    ///   px = px - key down(aKey) * speed
    ///   px = px + key down(dKey) * speed
    ///
    ///   text px, py, "@"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Hold shift to sprint:
    /// <code>
    /// shiftKey = scanCode("LeftShift")
    /// px = 0
    ///
    /// DO
    ///   IF key down(shiftKey) = 1
    ///     speed = 6
    ///   ELSE
    ///     speed = 2
    ///   ENDIF
    ///
    ///   px = px + rightKey() * speed
    ///   px = px - leftKey() * speed
    ///
    ///   text px, 120, ">"
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="scanCode">The scan code of the key. Use <see cref="ScanCode">scanCode</see> to convert a name to its code.</param>
    /// <returns><c>1</c> while the key is pressed, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="ScanCode">scanCode</seealso>
    /// <seealso cref="upKey">upkey</seealso>
    /// <seealso cref="rightKey">rightKey</seealso>
    /// <seealso cref="leftKey">leftKey</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("key down")]
    public static bool IsKeyPressed(int scanCode)
    {
        var keyDown = InputSystem.keyboardState.IsKeyDown((Keys)scanCode);
        return keyDown;
    }

    /// <summary>
    /// <para>Converts a key name string to its integer scan code.</para>
    /// <para>Pass the result to <see cref="IsKeyPressed">key down</see> or
    /// <see cref="IsNewKeyPressed">new key down</see> to check that key's state.</para>
    /// </summary>
    /// <remarks>
    /// The key name must match one of the MonoGame <c>Keys</c> enum values. Common
    /// examples: <c>"A"</c> through <c>"Z"</c>, <c>"D0"</c> through <c>"D9"</c> for
    /// number keys, <c>"Space"</c>, <c>"Enter"</c>, <c>"LeftShift"</c>, <c>"Escape"</c>,
    /// <c>"Tab"</c>.
    ///
    /// You typically call this once during setup and store the result in a variable, rather
    /// than converting the string every frame. The scan code does not change at runtime.
    /// </remarks>
    /// <example>
    /// Store scan codes at startup and use them in the game loop:
    /// <code>
    /// ` resolve scan codes once
    /// jumpKey = scanCode("Space")
    /// shootKey = scanCode("Z")
    /// pauseKey = scanCode("Escape")
    ///
    /// DO
    ///   IF new key down(jumpKey) = 1
    ///     text 10, 10, "Jump!"
    ///   ENDIF
    ///
    ///   IF key down(shootKey) = 1
    ///     text 10, 30, "Shooting..."
    ///   ENDIF
    ///
    ///   IF new key down(pauseKey) = 1
    ///     text 10, 50, "Paused"
    ///   ENDIF
    ///
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <example>
    /// Check number keys to select inventory slots:
    /// <code>
    /// ` D1 through D9 are the number row keys
    /// FOR i = 1 TO 9
    ///   slotKey(i) = scanCode("D" + str$(i))
    /// NEXT i
    ///
    /// slot = 1
    ///
    /// DO
    ///   FOR i = 1 TO 9
    ///     IF new key down(slotKey(i)) = 1
    ///       slot = i
    ///     ENDIF
    ///   NEXT i
    ///
    ///   text 10, 10, "Active slot: " + str$(slot)
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="key">The name of the key. Must match a MonoGame <c>Keys</c> value (e.g., <c>"A"</c>, <c>"Space"</c>, <c>"LeftShift"</c>).</param>
    /// <returns>The integer scan code for the given key.</returns>
    /// <seealso cref="IsKeyPressed">key down</seealso>
    /// <seealso cref="IsNewKeyPressed">new key down</seealso>
    /// <seealso cref="Sync">sync</seealso>
    [FadeBasicCommand("scanCode")]
    public static int ScanCode(string key)
    {
        var code = Enum.Parse<Keys>(key);
        return (int)code;
    }

    // ── Hit-test helpers ────────────────────────────────────────────────
    // Resolve the world matrix of a transform fresh, walking up the
    // parent chain by index rather than reading transform.computedWorld.
    // The cached field is only repopulated by TransformSystem.Calculate-
    // Transforms once per frame, so a hit-test that runs after a user
    // command mutates a transform (and before the next frame's pass)
    // would otherwise see stale data. Cost is O(depth) matrix multiplies
    // per call — negligible in practice and isolated to these two
    // commands; the rendering pipeline keeps using the cache.
    private static Matrix ResolveWorldMatrixByIndex(int transformIndex)
    {
        if (transformIndex <= 0) return Matrix.Identity;
        var t = TransformSystem.transforms[transformIndex];
        var local = TransformSystem.CreateMatrix(t.position, t.angle, t.scale);
        return local * ResolveWorldMatrixByIndex(t.parentIndex);
    }

    private static Matrix ResolveWorldMatrixByTransformId(int transformId)
    {
        if (transformId <= 0) return Matrix.Identity;
        TransformSystem.GetTransformIndex(transformId, out var index, out _);
        return ResolveWorldMatrixByIndex(index);
    }

    // Float-precision mouse coordinates in render-target space. The
    // public `mouse x` / `mouse y` commands cast to int for fbasic;
    // hit-tests want the raw float so a sprite edge at, say, x=99.7
    // doesn't get rounded to 100 and produce a false miss/hit.
    private static float GetMouseRenderX()
    {
        var v = (float)InputSystem.mouseState.X;
        v -= RenderSystem.mainBufferPosition.X;
        v /= GameSystem.graphicsDeviceManager.PreferredBackBufferWidth - RenderSystem.mainBufferPosition.X * 2;
        v *= RenderSystem.mainBuffer.Width;
        return v;
    }
    private static float GetMouseRenderY()
    {
        var v = (float)InputSystem.mouseState.Y;
        v -= RenderSystem.mainBufferPosition.Y;
        v /= GameSystem.graphicsDeviceManager.PreferredBackBufferHeight - RenderSystem.mainBufferPosition.Y * 2;
        v *= RenderSystem.mainBuffer.Height;
        return v;
    }

    /// <summary>
    /// <para>Returns <c>1</c> if the mouse cursor is currently over the bounding
    /// rectangle of the given sprite, <c>0</c> otherwise.</para>
    /// <para>The hit-test honors the sprite's position, scale, rotation, origin,
    /// and any transform it's attached to via
    /// <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>.
    /// Hidden sprites (via <see cref="HideSprite">hide sprite</see>) always
    /// return <c>0</c>.</para>
    /// </summary>
    /// <remarks>
    /// <para>The test is a rectangle hit (oriented to the sprite's rotation),
    /// not pixel-perfect — a transparent corner of the texture still counts as
    /// a hit. Mouse coordinates are pulled in render-target space, matching
    /// <see cref="GetMouseX">mouse x</see>, so the comparison is direct.</para>
    /// <para>Transform-attached sprites are handled correctly even when the
    /// parent transform was just modified this frame: the world matrix is
    /// resolved fresh by walking the parent chain, not from the cached
    /// per-frame computation.</para>
    /// </remarks>
    /// <example>
    /// Highlight a button sprite while the mouse hovers it:
    /// <code>
    /// texture 1, "Images/Button"
    /// sprite 1, 100, 100, 1
    /// DO
    ///   IF mouse over sprite(1) = 1
    ///     color sprite 1, 255, 255, 128
    ///   ELSE
    ///     color sprite 1, 255, 255, 255
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to test against.</param>
    /// <returns><c>1</c> when the cursor is inside the sprite's drawn region, <c>0</c> otherwise.</returns>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="IsMouseOverCollider">mouse over collider</seealso>
    /// <seealso cref="IsPointOverSprite">point over sprite</seealso>
    [FadeBasicCommand("mouse over sprite")]
    public static bool IsMouseOverSprite(int spriteId)
    {
        return PointHitsSprite(spriteId, GetMouseRenderX(), GetMouseRenderY());
    }

    // Shared by `mouse over sprite` and `point over sprite`. x/y are in
    // render-target / world coordinates — the same space sprite positions
    // live in. Honors the same hidden/anchor-transform semantics as the
    // mouse-over check, so the point-over command is a drop-in replacement
    // when the caller already has a coordinate (touch input, AI raycast,
    // a controller-driven cursor).
    private static bool PointHitsSprite(int spriteId, float x, float y)
    {
        SpriteSystem.GetSpriteIndex(spriteId, out _, out var sprite);
        if (sprite.hidden) return false;

        // Frame size in unscaled texture pixels — same call the renderer
        // uses when computing the source rect for SpriteBatch.Draw.
        TextureSystem.GetTextureIndex(sprite.imageId, out _, out var runtimeTex);
        var src = TextureSystem.GetSourceRect(ref runtimeTex, ref sprite);
        float frameW = src.Width;
        float frameH = src.Height;
        if (frameW <= 0 || frameH <= 0) return false;
        var originPx = new Vector2(frameW * sprite.origin.X, frameH * sprite.origin.Y);

        // Composite with the anchor transform if any. Mirrors the render
        // path at RenderSystem.cs ~line 491, but reads no cached matrices.
        var position = sprite.position;
        var rotation = sprite.rotation;
        var scale = sprite.scale;
        if (sprite.anchorTransformId > 0)
        {
            var localMat = TransformSystem.CreateMatrix(position, rotation, scale);
            var parentMat = ResolveWorldMatrixByTransformId(sprite.anchorTransformId);
            var worldMat = localMat * parentMat;
            TransformSystem.DecomposeMatrix(worldMat, out var p3, out var r3, out var s3);
            position = new Vector2(p3.X, p3.Y);
            rotation = r3.Z;
            scale = new Vector2(s3.X, s3.Y);
        }
        if (scale.X == 0 || scale.Y == 0) return false;

        // Inverse-transform input point → sprite-local pixel coords. The
        // sprite draw is (translate position) ∘ (rotate angle) ∘
        // (scale scale) ∘ (translate -origin) applied to local-pixel space,
        // so the inverse is the same chain reversed.
        var rx = x - position.X;
        var ry = y - position.Y;
        var cos = (float)Math.Cos(-rotation);
        var sin = (float)Math.Sin(-rotation);
        var ux = rx * cos - ry * sin;
        var uy = rx * sin + ry * cos;
        var localX = ux / scale.X;
        var localY = uy / scale.Y;

        return localX >= -originPx.X
            && localX <= frameW - originPx.X
            && localY >= -originPx.Y
            && localY <= frameH - originPx.Y;
    }

    /// <summary>
    /// Returns <c>1</c> if the given world-space point lands inside the sprite's drawn region, <c>0</c> otherwise.
    ///
    /// This is the same hit-test <see cref="IsMouseOverSprite">mouse over sprite</see> uses, but you supply the point yourself instead of reading the cursor — handy for touch input, AI vision checks, or a controller-driven cursor.
    /// </summary>
    /// <remarks>
    /// The test is a rectangle hit oriented to the sprite's rotation, not pixel-perfect — a transparent corner of the texture still counts as a hit. The sprite's position, scale, rotation, origin, and any attached transform (via <see cref="SetSpriteRelativeToAnother">attach sprite to transform</see>) are all honored.
    ///
    /// Coordinates are in render-target space — the same space sprite positions live in, and the same space <see cref="GetMouseX">mouse x</see> reports. If you're projecting from a different coordinate system (a UI panel, a camera-relative position), convert first.
    ///
    /// Hidden sprites (via <see cref="HideSprite">hide sprite</see>) always return <c>0</c>, matching the mouse-over behavior. If you need to hit-test a hidden sprite, show it first.
    ///
    /// Transform-attached sprites are resolved fresh each call, so a sprite whose parent transform was just moved this frame produces correct hits without waiting for the next frame's transform pass.
    /// </remarks>
    /// <example>
    /// Hit-test a touch point against several buttons:
    /// <code>
    /// touchX = 200.0
    /// touchY = 150.0
    /// FOR i = 1 TO 5
    ///   IF point over sprite(i, touchX, touchY) = 1
    ///     print "tapped button "; i
    ///   ENDIF
    /// NEXT i
    /// </code>
    /// </example>
    /// <example>
    /// AI "can I see the player?" check by sampling a ray every few pixels:
    /// <code>
    /// rayX = 100.0
    /// rayY = 100.0
    /// targetX = 400.0
    /// targetY = 300.0
    /// dx = (targetX - rayX) / 20.0
    /// dy = (targetY - rayY) / 20.0
    /// blocked = 0
    /// FOR step = 1 TO 20
    ///   px = rayX + dx * step
    ///   py = rayY + dy * step
    ///   IF point over sprite(wallSpriteId, px, py) = 1
    ///     blocked = 1
    ///     EXIT
    ///   ENDIF
    /// NEXT step
    /// </code>
    /// </example>
    /// <param name="spriteId">The sprite to test against.</param>
    /// <param name="x">The X coordinate of the point in render-target space.</param>
    /// <param name="y">The Y coordinate of the point in render-target space.</param>
    /// <returns><c>1</c> when the point falls inside the sprite's drawn region, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsMouseOverSprite">mouse over sprite</seealso>
    /// <seealso cref="IsPointOverCollider">point over collider</seealso>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="HideSprite">hide sprite</seealso>
    /// <seealso cref="SetSpriteRelativeToAnother">attach sprite to transform</seealso>
    [FadeBasicCommand("point over sprite")]
    public static bool IsPointOverSprite(int spriteId, float x, float y)
    {
        return PointHitsSprite(spriteId, x, y);
    }

    /// <summary>
    /// <para>Returns <c>1</c> if the mouse cursor is currently inside the given
    /// collider's bounding box, <c>0</c> otherwise.</para>
    /// <para>The hit-test honors the collider's position, size, and any transform
    /// it's attached to via
    /// <see cref="AttachColliderToTransform">attach collider to transform</see>.
    /// Colliders are axis-aligned; rotation on an attached transform shifts the
    /// collider's origin but does not rotate its bounds (matching the rest of
    /// the collision system's behavior).</para>
    /// </summary>
    /// <remarks>
    /// Mouse coordinates are in render-target space, matching the collider's
    /// own coordinate system. Transform-attached colliders are resolved fresh
    /// each call, so a collider whose parent transform was just moved this
    /// frame still produces correct hits without waiting for the next frame's
    /// transform pass.
    /// </remarks>
    /// <example>
    /// Detect clicks inside a free-floating button collider:
    /// <code>
    /// make collider 1, 50, 50, 200, 80
    /// DO
    ///   IF mouse over collider(1) = 1 AND new left click() = 1
    ///     print "clicked!"
    ///   ENDIF
    ///   sync
    /// LOOP
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider to test against.</param>
    /// <returns><c>1</c> when the cursor is inside the collider's bounds, <c>0</c> otherwise.</returns>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="IsMouseOverSprite">mouse over sprite</seealso>
    /// <seealso cref="IsPointOverCollider">point over collider</seealso>
    [FadeBasicCommand("mouse over collider")]
    public static bool IsMouseOverCollider(int colliderId)
    {
        return PointHitsCollider(colliderId, GetMouseRenderX(), GetMouseRenderY());
    }

    // Shared by `mouse over collider` and `point over collider`. Colliders
    // are axis-aligned even if their parent transform has rotation, so this
    // is a straight AABB test against the composed world position/size.
    private static bool PointHitsCollider(int colliderId, float x, float y)
    {
        CollisionSystem.GetColliderIndex(colliderId, out _, out var box);

        var position = box.position;
        var size = box.size;
        if (box.targetTransformId > 0)
        {
            // Mirrors CollisionSystem's per-frame compute (CollisionSystem.cs
            // ~line 104) but reads no cached matrices — walks the parent
            // chain fresh.
            var localMat = TransformSystem.CreateMatrix(position, 0, size);
            var parentMat = ResolveWorldMatrixByTransformId(box.targetTransformId);
            var worldMat = localMat * parentMat;
            TransformSystem.DecomposeMatrix(worldMat, out var p3, out _, out var s3);
            position = new Vector2(p3.X, p3.Y);
            size = new Vector2(s3.X, s3.Y);
        }
        if (size.X <= 0 || size.Y <= 0) return false;

        return x >= position.X
            && x < position.X + size.X
            && y >= position.Y
            && y < position.Y + size.Y;
    }

    /// <summary>
    /// Returns <c>1</c> if the given world-space point lands inside the collider's bounds, <c>0</c> otherwise.
    ///
    /// This is the same hit-test <see cref="IsMouseOverCollider">mouse over collider</see> uses, but you supply the point yourself instead of reading the cursor — handy for touch input, AI sight-line checks, or a controller-driven cursor.
    /// </summary>
    /// <remarks>
    /// Colliders are axis-aligned. Even if the parent transform has rotation, the collider's bounds stay AABB — rotation shifts the collider's center but doesn't tilt the box. This matches how the rest of the collision system behaves.
    ///
    /// Coordinates are in render-target space — the same space colliders live in, and the same space <see cref="GetMouseX">mouse x</see> reports.
    ///
    /// Transform-attached colliders are resolved fresh each call (the parent chain is walked, not the cached per-frame matrix), so a collider whose parent was just moved this frame still produces correct hits without waiting for the next frame's transform pass.
    ///
    /// Unlike <see cref="IsPointOverSprite">point over sprite</see>, this command isn't affected by any "hidden" flag — colliders don't have one. If you want a collider to stop responding, detach or destroy it.
    /// </remarks>
    /// <example>
    /// Check whether a touch point lands on any of several pickup colliders:
    /// <code>
    /// touchX = 200.0
    /// touchY = 150.0
    /// FOR id = 1 TO 5
    ///   IF point over collider(id, touchX, touchY) = 1
    ///     print "tapped pickup "; id
    ///   ENDIF
    /// NEXT id
    /// </code>
    /// </example>
    /// <example>
    /// Walk a vector forward in small steps to find the first collider along the way:
    /// <code>
    /// rayX = 100.0
    /// rayY = 100.0
    /// dx = 4.0
    /// dy = 0.0
    /// hitId = 0
    /// FOR step = 1 TO 80
    ///   px = rayX + dx * step
    ///   py = rayY + dy * step
    ///   IF point over collider(wallColliderId, px, py) = 1
    ///     hitId = wallColliderId
    ///     EXIT
    ///   ENDIF
    /// NEXT step
    /// </code>
    /// </example>
    /// <param name="colliderId">The collider to test against.</param>
    /// <param name="x">The X coordinate of the point in render-target space.</param>
    /// <param name="y">The Y coordinate of the point in render-target space.</param>
    /// <returns><c>1</c> when the point falls inside the collider's bounds, <c>0</c> otherwise.</returns>
    /// <seealso cref="IsMouseOverCollider">mouse over collider</seealso>
    /// <seealso cref="IsPointOverSprite">point over sprite</seealso>
    /// <seealso cref="GetMouseX">mouse x</seealso>
    /// <seealso cref="GetMouseY">mouse y</seealso>
    /// <seealso cref="CreateBoxCollider">box collider</seealso>
    /// <seealso cref="AttachColliderToTransform">attach collider to transform</seealso>
    [FadeBasicCommand("point over collider")]
    public static bool IsPointOverCollider(int colliderId, float x, float y)
    {
        return PointHitsCollider(colliderId, x, y);
    }

}
