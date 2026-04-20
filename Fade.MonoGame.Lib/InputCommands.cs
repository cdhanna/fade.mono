using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;
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

}
