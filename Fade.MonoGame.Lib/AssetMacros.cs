using Fade.MonoGame.Core;
using FadeBasic.SourceGenerators;

namespace Fade.MonoGame.Lib;

public partial class FadeMonoGameCommands
{

    /// <summary>
    /// <para>Pushes an asset file into the content build pipeline.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.</para>
    /// </summary>
    /// <remarks>
    /// Use this inside a macro block (lines prefixed with <c>#</c>) to tell the content
    /// pipeline about an asset your game needs. The pipeline will process and pack it so
    /// it is available at runtime through commands like
    /// <see cref="LoadTexture">texture</see>, <see cref="LoadSpriteFont">font</see>, or
    /// <see cref="LoadSoundEffect">load sfx clip</see>.
    ///
    /// After pushing, you can rename the asset with
    /// <see cref="RenameCurrent">rename asset</see> if the original filename is unwieldy.
    /// The push/rename pair is the most common macro pattern for setting up content.
    /// </remarks>
    /// <example>
    /// Push a texture asset so it is available at runtime:
    /// <code>
    /// ` push an image into the content pipeline
    /// # push asset "Assets/Images/player-sprite-v2.png"
    /// # rename asset "Images/Player"
    ///
    /// ` later at runtime, load it by its renamed path
    /// texture 1, "Images/Player"
    /// sprite 1, 100, 100, 1
    /// </code>
    /// </example>
    /// <example>
    /// Push a font asset for text rendering:
    /// <code>
    /// ` push a font into the content pipeline
    /// # push asset "Assets/Fonts/MyFont.spritefont"
    /// # rename asset "Fonts/Main"
    ///
    /// ` later at runtime, load and use the font
    /// font 1, "Fonts/Main"
    /// text 1, 1, 100, 50, "Hello!"
    /// </code>
    /// </example>
    /// <param name="path">The file path of the asset to add to the content build.</param>
    /// <seealso cref="RenameCurrent">rename asset</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    /// <seealso cref="Sprite">sprite</seealso>
    /// <seealso cref="Text">text</seealso>
    [FadeBasicCommand("push asset", FadeBasicCommandUsage.Macro)]
    public static void Push(string path)
    {
        ContentSystem.Push(path);
    }

    /// <summary>
    /// <para>Renames the most recently pushed asset in the content build pipeline.</para>
    /// <para>This is a macro-time command. It runs during compilation, not at game runtime.
    /// It operates on whatever <see cref="Push">push asset</see> last added.</para>
    /// </summary>
    /// <remarks>
    /// Call this right after <see cref="Push">push asset</see> when the original filename
    /// is too long, includes version numbers, or does not match the name you want to use in
    /// your runtime code. The new name becomes the content path you pass to loading
    /// commands like <see cref="LoadTexture">texture</see> or
    /// <see cref="LoadSoundEffect">load sfx clip</see>.
    /// </remarks>
    /// <example>
    /// Rename a pushed asset to a shorter, cleaner path:
    /// <code>
    /// ` push an audio file with a long filename and give it a short name
    /// # push asset "Assets/Audio/bubble-pop-2-293341.mp3"
    /// # rename asset "Audio/BubblePop"
    ///
    /// ` at runtime, load using the short name
    /// load sfx clip 1, "Audio/BubblePop"
    /// </code>
    /// </example>
    /// <example>
    /// Rename multiple assets in sequence:
    /// <code>
    /// ` push and rename several textures
    /// # push asset "Assets/Images/enemy_spritesheet_final_v3.png"
    /// # rename asset "Images/Enemy"
    /// # push asset "Assets/Images/bg-tiles-large.png"
    /// # rename asset "Images/Background"
    ///
    /// ` at runtime, load them by their clean names
    /// texture 1, "Images/Enemy"
    /// texture 2, "Images/Background"
    /// </code>
    /// </example>
    /// <param name="name">The new content name for the asset.</param>
    /// <seealso cref="Push">push asset</seealso>
    /// <seealso cref="LoadTexture">texture</seealso>
    /// <seealso cref="LoadSpriteFont">font</seealso>
    [FadeBasicCommand("rename asset", FadeBasicCommandUsage.Macro)]
    public static void RenameCurrent(string name)
    {
        ContentSystem.GetCurrent().name = name;
    }
    
    public static void Set()
    {
        // # push asset Fish/Audio/bubble-pop-2-293341.mp3
        // # rename asset Fish/Audio/bubble-pop-2.mp3
        // # set asset importer "Mp3Importer"
        // # set asset processor "SoundEffectProcessor"
        // # set asset param "Quality" "Best"
        
        
        
        // # rename asset Fish/Audio/bubble-pop-2-293341.mp3 Fish/Audio/bubble-pop-2.mp3 
        // # asset param Fish/Audio/bubble-pop-2-293341.mp3 
        
        // set importer 
        // set processor, 
        // set parameter
        // set output name
    }

}