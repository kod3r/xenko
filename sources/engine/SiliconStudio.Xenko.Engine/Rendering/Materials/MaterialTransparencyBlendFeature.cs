// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Materials.ComputeColors;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering.Materials
{
    /// <summary>
    /// A transparent blend material.
    /// </summary>
    [DataContract("MaterialTransparencyBlendFeature")]
    [Display("Blend")]
    public class MaterialTransparencyBlendFeature : MaterialFeature, IMaterialTransparencyFeature
    {
        private static readonly MaterialStreamDescriptor AlphaBlendStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend", "matDiffuseSpecularAlphaBlend", MaterialKeys.DiffuseSpecularAlphaBlendValue.PropertyType);

        private static readonly MaterialStreamDescriptor AlphaBlendColorStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend - Color", "matAlphaBlendColor", MaterialKeys.AlphaBlendColorValue.PropertyType);

        private static readonly PropertyKey<bool> HasFinalCallback = new PropertyKey<bool>("MaterialTransparencyAdditiveFeature.HasFinalCallback", typeof(MaterialTransparencyAdditiveFeature));
    
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransparencyBlendFeature"/> class.
        /// </summary>
        public MaterialTransparencyBlendFeature()
        {
            Alpha = new ComputeFloat(1f);
            Tint = new ComputeColor(Color.White);
        }
    
        /// <summary>
        /// Gets or sets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>An additional factor that can be used to modulate original alpha of the material.</userdoc>
        [NotNull]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar Alpha { get; set; }

        /// <summary>
        /// Gets or sets the tint color.
        /// </summary>
        /// <value>The tint.</value>
        /// <userdoc>The tint color to apply on the material during the blend.</userdoc>
        [NotNull]
        [DataMember(20)]
        public IComputeColor Tint { get; set; }

        public override void VisitFeature(MaterialGeneratorContext context)
        {
            var alpha = Alpha ?? new ComputeFloat(1f);
            var tint = Tint ?? new ComputeColor(Color.White);
    
            // Use pre-multiplied alpha to support both additive and alpha blending
            var blendDesc = new BlendStateDescription(Blend.One, Blend.InverseSourceAlpha);
            context.Material.HasTransparency = true;
            context.Parameters.Set(Effect.BlendStateKey, BlendState.NewFake(blendDesc));

            context.SetStream(AlphaBlendStream.Stream, alpha, MaterialKeys.DiffuseSpecularAlphaBlendMap, MaterialKeys.DiffuseSpecularAlphaBlendValue, Color.White);
            context.SetStream(AlphaBlendColorStream.Stream, tint, MaterialKeys.AlphaBlendColorMap, MaterialKeys.AlphaBlendColorValue, Color.White);
    
            if (!context.Tags.Get(HasFinalCallback))
            {
                context.Tags.Set(HasFinalCallback, true);
                context.AddFinalCallback(MaterialShaderStage.Pixel, AddDiffuseSpecularAlphaBlendColor);
            }
        }
    
        private void AddDiffuseSpecularAlphaBlendColor(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddSurfaceShader(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceDiffuseSpecularAlphaBlendColor"));
        }
    }
}