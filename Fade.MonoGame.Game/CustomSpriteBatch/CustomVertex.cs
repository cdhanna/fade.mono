using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics.Fade;
    
/// <summary>
/// Describes a custom vertex format structure that contains position,
/// color, and one set of texture coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FadeSpriteVertex : IVertexType
{
    /// <inheritdoc cref="VertexPosition.Position"/>
    public Vector3 Position;
    /// <inheritdoc cref="VertexPositionColor.Color"/>
    public Color Color;
    /// <inheritdoc cref="VertexPositionTexture.TextureCoordinate"/>
    public Vector2 TextureCoordinate;

    public Vector4 TexCoord1;
    
    /// <inheritdoc cref="IVertexType.VertexDeclaration"/>
    public static readonly VertexDeclaration VertexDeclaration;

    /// <summary>
    /// Creates an instance of <see cref="VertexPositionColorTexture"/>.
    /// </summary>
    /// <param name="position">Position of the vertex.</param>
    /// <param name="color">Color of the vertex.</param>
    /// <param name="textureCoordinate">Texture coordinate of the vertex.</param>
    public FadeSpriteVertex(Vector3 position, Color color, Vector2 textureCoordinate, Vector4 texCoord1)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
        TexCoord1 = texCoord1;
    }
	
    VertexDeclaration IVertexType.VertexDeclaration
    {
        get
        {
            return VertexDeclaration;
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Position.GetHashCode();
            hashCode = (hashCode * 397) ^ Color.GetHashCode();
            hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
            hashCode = (hashCode * 397) ^ TexCoord1.GetHashCode();
            return hashCode;
        }
    }

    /// <inheritdoc cref="VertexPosition.ToString()"/>
    public override string ToString()
    {
        return "{{Position:" + this.Position + " Color:" + this.Color + " TextureCoordinate:" + this.TextureCoordinate + "}}";
    }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="VertexPositionColorTexture"/> are equal
    /// </summary>
    /// <param name="left">The object on the left of the equality operator.</param>
    /// <param name="right">The object on the right of the equality operator.</param>
    /// <returns>
    /// <see langword="true"/> if the objects are the same; <see langword="false"/> otherwise.
    /// </returns>
    public static bool operator ==(FadeSpriteVertex left, FadeSpriteVertex right)
    {
        return (((left.Position == right.Position) && (left.Color == right.Color)) && (left.TextureCoordinate == right.TextureCoordinate) && (left.TexCoord1 == right.TexCoord1));
    }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="VertexPositionColorTexture"/> are different
    /// </summary>
    /// <param name="left">The object on the left of the inequality operator.</param>
    /// <param name="right">The object on the right of the inequality operator.</param>
    /// <returns>
    /// <see langword="true"/> if the objects are different; <see langword="false"/> otherwise.
    /// </returns>
    public static bool operator !=(FadeSpriteVertex left, FadeSpriteVertex right)
    {
        return !(left == right);
    }

    /// <inheritdoc cref="VertexPosition.Equals(object)"/>
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        if (obj.GetType() != base.GetType())
            return false;

        return (this == ((FadeSpriteVertex)obj));
    }

    static FadeSpriteVertex()
    {
        var elements = new VertexElement[] 
        { 
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), 
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0), 
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0) ,
            new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1) 
        };
        VertexDeclaration = new VertexDeclaration(elements);
    }
}