using System;

namespace Fade.MonoGame.Contracts;

/// <summary>
/// What kind of reload the engine is about to apply, so a host system can refuse
/// one it cannot survive.
/// </summary>
public enum ReloadKind
{
    /// <summary>F1: rebuild the VM from scratch and reset all state.</summary>
    FullRestart,

    /// <summary>F2: state-preserving module reload over the live VM.</summary>
    ModuleReload
}

/// <summary>
/// A host-supplied system the engine drives at defined points in its frame.
///
/// Registered through <c>Game.Services</c> before the game runs, exactly like
/// <see cref="Fade.MonoGame.Content.IContentBuilder"/>:
///
/// <code>
/// game.Services.AddService(typeof(IFadeHostSystem), new MyNetSystem());
/// </code>
///
/// Register several by adding an <c>IFadeHostSystem[]</c> instead; the engine
/// invokes them in array order.
///
/// The hook points are named rather than generic on purpose. A system that must run
/// at a fixed rate *before the Fade program observes the world* — netcode being the
/// motivating case — cannot express that with a plain Update() callback, because the
/// engine pumps the VM partway through its own Update.
///
/// Deliberately free of MonoGame types: this assembly has no framework reference and
/// keeping it that way is why it can be packaged on its own. Times are plain
/// seconds; a system that needs the graphics device pulls it from the service
/// provider handed to <see cref="Initialize"/>.
/// </summary>
public interface IFadeHostSystem
{
    /// <summary>
    /// Called once, after the graphics device exists and before the first frame.
    /// </summary>
    /// <param name="services">
    /// The game's service provider. Resolve <c>IGraphicsDeviceService</c> from it if
    /// the system needs a device.
    /// </param>
    void Initialize(IServiceProvider services);

    /// <summary>
    /// Runs BEFORE the Fade program's frame. Fixed-rate work belongs here: pump a
    /// transport, advance a simulation, publish state the Fade program will read
    /// through commands during this same frame.
    /// </summary>
    /// <param name="elapsedSeconds">Seconds since the previous frame.</param>
    /// <param name="totalSeconds">Seconds since the game started.</param>
    void BeforeVmTick(double elapsedSeconds, double totalSeconds);

    /// <summary>
    /// Runs AFTER the Fade program's frame, still inside Update. Use it to drain
    /// whatever the program produced — submitted input, requested actions.
    /// </summary>
    void AfterVmTick(double elapsedSeconds, double totalSeconds);

    /// <summary>
    /// Runs during Draw, after the engine has composited the Fade program's output
    /// so overlays land on top. A debug panel or lobby UI goes here.
    /// </summary>
    void Draw(double elapsedSeconds, double totalSeconds);

    /// <summary>
    /// Veto a reload. Return false to block it.
    ///
    /// Both reload kinds reset the presentation VM, which is normally harmless. It is
    /// not harmless when the host holds state the reload would invalidate — a live
    /// lockstep session, for instance, where reloading only *our* copy of the program
    /// desyncs us from every peer.
    /// </summary>
    /// <param name="kind">Which reload the engine wants to apply.</param>
    /// <param name="reason">
    /// Filled in when returning false, so the engine can tell the user why the blue
    /// or red bar is not doing anything.
    /// </param>
    bool CanReload(ReloadKind kind, out string reason);

    /// <summary>Called when the game is shutting down.</summary>
    void Shutdown();
}

/// <summary>
/// No-op base class, so an implementation only overrides the hooks it needs.
/// </summary>
public abstract class FadeHostSystem : IFadeHostSystem
{
    public virtual void Initialize(IServiceProvider services) { }
    public virtual void BeforeVmTick(double elapsedSeconds, double totalSeconds) { }
    public virtual void AfterVmTick(double elapsedSeconds, double totalSeconds) { }
    public virtual void Draw(double elapsedSeconds, double totalSeconds) { }

    public virtual bool CanReload(ReloadKind kind, out string reason)
    {
        reason = null!;
        return true;
    }

    public virtual void Shutdown() { }
}
