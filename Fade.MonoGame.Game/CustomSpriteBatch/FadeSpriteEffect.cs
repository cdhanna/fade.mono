namespace Microsoft.Xna.Framework.Graphics.Fade;

public class FadeSpriteEffect : Effect
{
    public FadeSpriteEffect(Effect effect)
        :base(effect)
    {
        CacheEffectParameters();
    }
    
    private EffectParameter _matrixParam;
        private Viewport _lastViewport;
        private Matrix _projection;

        /// <summary>
        /// An optional matrix used to transform the sprite geometry. Uses <see cref="Matrix.Identity"/> if null.
        /// </summary>
        public Matrix? TransformMatrix { get; set; }


        /// <summary>
        /// Looks up shortcut references to our effect parameters.
        /// </summary>
        public void CacheEffectParameters()
        {
            _matrixParam = Parameters["MatrixTransform"];
        }

        /// <summary>
        /// Lazily computes derived parameter values immediately before applying the effect.
        /// </summary>
        protected internal override void OnApply()
        {
            var vp = GraphicsDevice.Viewport;
            if ((vp.Width != _lastViewport.Width) || (vp.Height != _lastViewport.Height))
            {
                // Normal 3D cameras look into the -z direction (z = 1 is in front of z = 0). The
                // sprite batch layer depth is the opposite (z = 0 is in front of z = 1).
                // --> We get the correct matrix with near plane 0 and far plane -1.
                Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, -100, out _projection);

                
                if (GraphicsDevice.UseHalfPixelOffset)
                {
                    _projection.M41 += -0.5f * _projection.M11;
                    _projection.M42 += -0.5f * _projection.M22;
                }

                _lastViewport = vp;
            }

            if (TransformMatrix.HasValue)
                _matrixParam.SetValue(TransformMatrix.GetValueOrDefault() * _projection);
            else
                _matrixParam.SetValue(_projection);
        }
}