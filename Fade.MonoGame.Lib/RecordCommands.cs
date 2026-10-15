using System;
using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{
    /// <summary>
    /// <para>Starts recording the game to a video file.</para>
    /// <para>While it records, the game's clock moves on by exactly one frame of the video for every frame,
    /// however long that frame really took, so the video never drops a frame and comes out the same every time.</para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every frame from here until <see cref="RecordStop">record stop</see> goes into the file: the finished
    /// picture, with the screen effect, scaled up to fit the recording's size and centered on black. The window
    /// size makes no difference to the video, and the debug windows and gizmos are not in it.
    /// </para>
    /// <para>
    /// Recording is slow. Every frame is copied back from the graphics card and handed to ffmpeg, so the game runs
    /// slower than real time while it records. That is fine: <see cref="GameTime">game ms</see> moves on by one
    /// frame of the video at a time, not by the real time, so everything that animates by the game's clock looks
    /// right in the video. Anything that reads <c>wall ms</c> does not. The sounds are not recorded.
    /// </para>
    /// <para>
    /// The file's ending picks what it is: <c>.mp4</c> (the default, and what is added when there is no ending)
    /// is h.264 for watching and sharing, <c>.mov</c> is ProRes for a video editor, and <c>.png</c> writes one
    /// picture per frame, so the path should hold a <c>%05d</c> for the number. Folders that do not exist yet are made.
    /// </para>
    /// <para>
    /// This needs ffmpeg. It is looked for on the PATH, or set the FADE_FFMPEG environment variable to it. Starting
    /// a recording while another runs finishes that one first. A recording that is still running when the
    /// program ends or restarts is finished then. There is no recording in the browser.
    /// </para>
    /// </remarks>
    /// <example>
    /// Record three seconds of the game:
    /// <code>
    /// record start "videos/three_seconds"
    /// WHILE record frame count() &lt; 180
    ///   ` the game goes on as it always does
    ///   sync
    /// ENDWHILE
    /// record stop
    /// </code>
    /// </example>
    /// <param name="filePath">Where to write the video. <c>.mp4</c> is added when there is no ending.</param>
    /// <seealso cref="RecordStop">record stop</seealso>
    /// <seealso cref="SetRecordSize">set record size</seealso>
    /// <seealso cref="SetRecordRate">set record rate</seealso>
    /// <seealso cref="RecordFrameCount">record frame count</seealso>
    /// <seealso cref="GameTime">game ms</seealso>
    [FadeBasicCommand("record start")]
    public static void RecordStart(string filePath)
    {
        RecordSystem.Start(filePath);
    }

    /// <summary>
    /// <para>Finishes the recording and closes the video file.</para>
    /// <para>The game waits here until ffmpeg has written the end of the file, which can take a moment.</para>
    /// </summary>
    /// <remarks>
    /// After this the game is paced by real time again, and its clock carries on from where the recording left it,
    /// so it never runs backwards. Calling it when nothing is recording does nothing.
    /// </remarks>
    /// <example>
    /// Record until the space key is pressed:
    /// <code>
    /// record start "videos/until_space"
    /// spaceKey = scanCode("Space")
    /// DO
    ///   IF key down(spaceKey) = 1 THEN EXIT
    ///   sync
    /// LOOP
    /// record stop
    /// </code>
    /// </example>
    /// <seealso cref="RecordStart">record start</seealso>
    /// <seealso cref="RecordFrameCount">record frame count</seealso>
    [FadeBasicCommand("record stop")]
    public static void RecordStop()
    {
        RecordSystem.Stop();
    }

    /// <summary>
    /// <para>Sets the size, in pixels, of the videos that <see cref="RecordStart">record start</see> makes.</para>
    /// <para>It is <c>1920</c> by <c>1080</c> until this is called. It takes effect at the next recording.</para>
    /// </summary>
    /// <remarks>
    /// The game is drawn as big as it fits and centered, with black around it when the shapes differ. A size that
    /// is a whole number of times the render size keeps hard pixel edges; any other is smoothed.
    /// </remarks>
    /// <example>
    /// Record in 4K:
    /// <code>
    /// set record size 3840, 2160
    /// record start "videos/big"
    /// </code>
    /// </example>
    /// <param name="width">The width of the video in pixels. Use an even number.</param>
    /// <param name="height">The height of the video in pixels. Use an even number.</param>
    /// <seealso cref="RecordStart">record start</seealso>
    /// <seealso cref="SetRecordRate">set record rate</seealso>
    /// <seealso cref="SetRenderSize">set render size</seealso>
    [FadeBasicCommand("set record size")]
    public static void SetRecordSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Console.WriteLine($"[record] {width}x{height} is not a size.");
            return;
        }
        RecordSystem.width = width;
        RecordSystem.height = height;
    }

    /// <summary>
    /// <para>Sets how many frames per second the videos that <see cref="RecordStart">record start</see> makes have.</para>
    /// <para>It is <c>60</c> until this is called. It takes effect at the next recording.</para>
    /// </summary>
    /// <remarks>
    /// While a recording runs, the game's clock moves on by one frame at this rate per frame drawn, so a rate of
    /// <c>30</c> makes the game take bigger steps, not slower ones.
    /// </remarks>
    /// <example>
    /// <code>
    /// set record rate 30
    /// record start "videos/thirty"
    /// </code>
    /// </example>
    /// <param name="framesPerSecond">Frames per second of the video, such as <c>30</c> or <c>60</c>.</param>
    /// <seealso cref="RecordStart">record start</seealso>
    /// <seealso cref="SetRecordSize">set record size</seealso>
    [FadeBasicCommand("set record rate")]
    public static void SetRecordRate(int framesPerSecond)
    {
        if (framesPerSecond <= 0)
        {
            Console.WriteLine($"[record] {framesPerSecond} is not a frame rate.");
            return;
        }
        RecordSystem.framesPerSecond = framesPerSecond;
    }

    /// <summary>
    /// <para>Gives a name to this moment of the recording, such as the frame that a swap pops.</para>
    /// <para>The marks go in the .json that is written next to the video when it stops, with the frame that each was made on.</para>
    /// </summary>
    /// <remarks>
    /// The sounds are not in the video. What is written next to it says every sound that played, and on
    /// which frame, and every mark. Something that puts the videos together into one can mix the sounds
    /// back in from that, and use a mark to line a moment up with the music. A mark made when nothing is
    /// recording does nothing.
    /// </remarks>
    /// <example>
    /// <code>
    /// record start "videos/swap"
    /// ` ... the swap is made ...
    /// record mark "pop"
    /// </code>
    /// </example>
    /// <param name="name">What the moment is called.</param>
    /// <seealso cref="RecordStart">record start</seealso>
    /// <seealso cref="RecordFrameCount">record frame count</seealso>
    [FadeBasicCommand("record mark")]
    public static void RecordMark(string name)
    {
        RecordSystem.Mark(name);
    }

    /// <summary>
    /// <para>Records only a part of the screen, which fills the whole video: a close up.</para>
    /// <para>The part is in the game's own pixels, the ones of <see cref="SetRenderSize">set render size</see>. It can change on every
    /// frame, to push in or to follow something.</para>
    /// </summary>
    /// <remarks>
    /// The game itself is drawn as it always is, and the window shows all of it; only the video is a close up. Because
    /// the part is scaled up to the size of the video, a small part looks soft. Pick a record size that is big enough
    /// with <see cref="SetRecordSize">set record size</see>. A part that has a different shape than the video gets
    /// black bars. <see cref="ClearRecordView">clear record view</see> goes back to all of the screen.
    /// </remarks>
    /// <example>
    /// Push in on the middle of a 570 by 320 screen over two seconds:
    /// <code>
    /// record start "videos/push_in"
    /// FOR f = 0 TO 120
    ///   ` from all of it, to half of it
    ///   k# = 1.0 - f / 240.0
    ///   w# = 570 * k#
    ///   h# = 320 * k#
    ///   set record view (570 - w#) / 2, (320 - h#) / 2, w#, h#
    ///   sync
    /// NEXT
    /// record stop
    /// </code>
    /// </example>
    /// <param name="x">The left edge of the part, in pixels of the game's screen.</param>
    /// <param name="y">The top edge of the part.</param>
    /// <param name="width">How wide the part is.</param>
    /// <param name="height">How tall the part is.</param>
    /// <seealso cref="ClearRecordView">clear record view</seealso>
    /// <seealso cref="RecordStart">record start</seealso>
    /// <seealso cref="SetRecordSize">set record size</seealso>
    [FadeBasicCommand("set record view")]
    public static void SetRecordView(float x, float y, float width, float height)
    {
        RecordSystem.viewX = x;
        RecordSystem.viewY = y;
        RecordSystem.viewWidth = width;
        RecordSystem.viewHeight = height;
    }

    /// <summary>
    /// <para>Records all of the screen again, after <see cref="SetRecordView">set record view</see>.</para>
    /// </summary>
    /// <remarks>
    /// The view also goes back to all of the screen when the program restarts.
    /// </remarks>
    /// <example>
    /// <code>
    /// set record view 100, 50, 285, 160
    /// ` ... a close up ...
    /// clear record view
    /// </code>
    /// </example>
    /// <seealso cref="SetRecordView">set record view</seealso>
    [FadeBasicCommand("clear record view")]
    public static void ClearRecordView()
    {
        RecordSystem.viewWidth = 0;
        RecordSystem.viewHeight = 0;
    }

    /// <summary>
    /// <para>Returns how many frames the recording that is running has written so far.</para>
    /// <para>It counts from <c>0</c> at <see cref="RecordStart">record start</see>, and keeps its last count after
    /// <see cref="RecordStop">record stop</see>.</para>
    /// </summary>
    /// <remarks>
    /// Use it to make a shot last an exact length: divide by the rate from
    /// <see cref="SetRecordRate">set record rate</see> for seconds of video.
    /// </remarks>
    /// <example>
    /// Hold a shot for two seconds:
    /// <code>
    /// record start "videos/hold"
    /// WHILE record frame count() &lt; 120
    ///   sync
    /// ENDWHILE
    /// record stop
    /// </code>
    /// </example>
    /// <returns>The number of frames written to the recording.</returns>
    /// <seealso cref="RecordStart">record start</seealso>
    /// <seealso cref="RecordStop">record stop</seealso>
    [FadeBasicCommand("record frame count")]
    public static int RecordFrameCount()
    {
        return (int)RecordSystem.frameCount;
    }

    /// <summary>
    /// <para>Returns <c>1</c> while a recording is running, and <c>0</c> when not.</para>
    /// </summary>
    /// <remarks>
    /// Handy to keep something out of a recording, or to only show something in one.
    /// </remarks>
    /// <example>
    /// <code>
    /// IF is recording() = 0
    ///   print "not recording"
    /// ENDIF
    /// </code>
    /// </example>
    /// <returns><c>1</c> while recording, <c>0</c> otherwise.</returns>
    /// <seealso cref="RecordStart">record start</seealso>
    [FadeBasicCommand("is recording")]
    public static int IsRecording()
    {
        return RecordSystem.IsRecording ? 1 : 0;
    }
}
