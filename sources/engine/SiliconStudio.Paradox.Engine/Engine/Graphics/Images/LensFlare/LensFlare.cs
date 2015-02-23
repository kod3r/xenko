﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Effects.Images
{
    /// <summary>
    /// Applies some lens-flare effect to an image. 
    /// This takes in input a bright-pass buffer, calculates the lens-flares and blends them 
    /// additively to the specified output.
    /// </summary>
    [DataContract("LensFlare")]
    public class LensFlare : ImageEffect
    {
        private GaussianBlur blur;
        private ImageEffectShader flareArtifactEffect;
        private ImageEffectShader flareReplicateEffect;

        /// <summary>
        /// Initializes a new instance of the <see cref="LensFlare"/> class.
        /// </summary>
        public LensFlare()
        {
            Amount = 1f;

            // Presets
            const float distortionFactor = 0.39f;
            ZoomOffsetsDistortions = new Vector2[]
            {
                new Vector2( -3.2f  * distortionFactor,  1.4f),
                new Vector2( -5.6f  * distortionFactor,  1.4f),
                new Vector2( -3.2f  * distortionFactor,  0.5f),
                new Vector2( -0.96f * distortionFactor, -0.2f),
                new Vector2( -1.14f * distortionFactor, -0.2f),
                new Vector2( -1.14f * distortionFactor, -0.23f),
                new Vector2( -1.49f * distortionFactor, -0.2f),
                new Vector2( -1.52f * distortionFactor, -0.2f)
            };

            HaloFactor = 1f;

            ColorAberrationStrength = 0.2f;

            ColorAberrations = new Vector3[]
            {
                new Vector3(0.5f, 0.6f, 1.6f),
                new Vector3(0.8f, 0.9f, 1.2f),
                new Vector3(0.3f, 1.0f, 0.3f),
                new Vector3(0.5f, 0.8f, 1.5f),
                new Vector3(1.5f, 0.5f, 0.8f),
                new Vector3(1.5f, 0.5f, 0.8f),
                new Vector3(0.2f, 1.5f, 0.3f),
                new Vector3(0.2f, 1.5f, 0.3f)
            };

        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            blur = ToLoadAndUnload(new GaussianBlur());
            flareArtifactEffect = ToLoadAndUnload(new ImageEffectShader("FlareArtifactEffect"));
            flareReplicateEffect = ToLoadAndUnload(new ImageEffectShader("FlareReplicate"));
        }

        /// <summary>
        /// Amount of light streak (intensity).
        /// </summary>
        [DataMember(10)]
        [DefaultValue(1f)]
        public float Amount { get; set; }

        /// <summary>
        /// Strength of the color aberration.
        /// </summary>
        [DataMember(20)]
        [DefaultValue(0.2f)]
        public float ColorAberrationStrength { get; set; }

        /// <summary>
        /// Defines the offsets (through zooming), and the distortion factor of each tap.
        /// A distortion factor > 0 will stretch in a circular way around the center (fisheye-like); 
        /// in [-1, 0] it will stretch the texture towards the center. 
        /// </summary>
        [DataMemberIgnore]
        public Vector2[] ZoomOffsetsDistortions { get; set; }

        /// <summary>
        /// Modulate the RGB color of each tap.
        /// </summary>
        [DataMemberIgnore]
        public Vector3[] ColorAberrations { get; set; }

        /// <summary>
        /// Halo factor when the camera is pointing exactly at a light source.
        /// </summary>
        [DataMember(20)]
        [DefaultValue(1f)]
        public float HaloFactor { get; set; }

        protected override void DrawCore(RenderContext contextParameters)
        {
            var input = GetInput(0);
            var output = GetOutput(0) ?? input;

            if (input == null )
            {
                return;
            }

            // Downscale to 1/2
            var halfSize = input.Size.Down2();
            var halfSizeRenderTarget = NewScopedRenderTarget2D(halfSize.Width, halfSize.Height, input.Format);
            Scaler.SetInput(input);
            Scaler.SetOutput(halfSizeRenderTarget);
            Scaler.Draw(contextParameters, "Downsize to 0.5");

            // Work on a blurred bright map
            var blurredBright = NewScopedRenderTarget2D(halfSizeRenderTarget.Description);
            blur.Radius = 8;
            blur.SetInput(halfSizeRenderTarget);
            blur.SetOutput(blurredBright);
            blur.Draw(contextParameters);

            // Draws a few artifacts
            var flareRenderTargetInitial = NewScopedRenderTarget2D(halfSizeRenderTarget.Description);
            var flareRenderTarget = NewScopedRenderTarget2D(halfSizeRenderTarget.Description);

            flareArtifactEffect.Parameters.Set(FlareArtifactKeys.Count, ZoomOffsetsDistortions.Length);
            flareArtifactEffect.Parameters.Set(FlareArtifactShaderKeys.ZoomOffsetsDistortions, ZoomOffsetsDistortions);
            flareArtifactEffect.Parameters.Set(FlareArtifactShaderKeys.AberrationStrength, ColorAberrationStrength);
            flareArtifactEffect.Parameters.Set(FlareArtifactShaderKeys.ColorAberrations, ColorAberrations);
            flareArtifactEffect.Parameters.Set(FlareArtifactShaderKeys.Amount, Amount * 0.0005f);
            flareArtifactEffect.SetInput(0, blurredBright);
            flareArtifactEffect.SetOutput(flareRenderTargetInitial);
            flareArtifactEffect.Draw(contextParameters);
            
            // Replicates the artifacts around
            flareReplicateEffect.Parameters.Set(FlareReplicateKeys.Amount, Amount * 0.0005f);
            flareReplicateEffect.Parameters.Set(FlareReplicateKeys.HaloFactor, Amount * 0.0005f * HaloFactor);
            flareReplicateEffect.SetInput(0,flareRenderTargetInitial);
            flareReplicateEffect.SetInput(1, blurredBright);
            flareReplicateEffect.SetOutput(flareRenderTarget);
            flareReplicateEffect.Draw(contextParameters);

            // Adds the result to the scene
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Additive);
            Scaler.SetInput(flareRenderTarget);
            Scaler.SetOutput(output);
            Scaler.Draw(contextParameters);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
        }
    }
}