using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Fade.MonoGame.Core;

public static class EffectExtensions
{
    public static bool ContainsParameter(this EffectParameterCollection parameters, string parameterName)
    {
        return parameters[parameterName] != null;
    }
}

public static class ContentManagerExtensions
{
    public static string GetRootDirectoryFullPath(this ContentManager content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        var type = content.GetType();

        var prop = type.GetProperty(
            "RootDirectoryFullPath",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (prop == null)
            throw new InvalidOperationException(
                "Property 'RootDirectoryFullPath' not found.");

        return prop.GetValue(content) as string;
    }
}


public static class TextureExtensions
{
    extension (Texture2D tex)
    {
        public float TexelWidth => 1f / tex.Width; // TODO: this is a waste of computation. I wish we could cache these. 
        public float TexelHeight => 1f / tex.Height;
    }
}