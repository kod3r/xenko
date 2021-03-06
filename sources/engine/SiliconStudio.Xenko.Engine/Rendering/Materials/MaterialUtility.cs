// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Globalization;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering.Materials.Processor.Visitors
{
    /// <summary>
    /// Class MaterialUtility.
    /// </summary>
    internal class MaterialUtility
    {
        public const string BackgroundCompositionName = "color1";

        public const string ForegroundCompositionName = "color2";

        public static int GetTextureIndex(TextureCoordinate texcoord)
        {
            switch (texcoord)
            {
                case TextureCoordinate.Texcoord0:
                    return 0;
                case TextureCoordinate.Texcoord1:
                    return 1;
                case TextureCoordinate.Texcoord2:
                    return 2;
                case TextureCoordinate.Texcoord3:
                    return 3;
                case TextureCoordinate.Texcoord4:
                    return 4;
                case TextureCoordinate.Texcoord5:
                    return 5;
                case TextureCoordinate.Texcoord6:
                    return 6;
                case TextureCoordinate.Texcoord7:
                    return 7;
                case TextureCoordinate.Texcoord8:
                    return 8;
                case TextureCoordinate.Texcoord9:
                    return 9;
                case TextureCoordinate.TexcoordNone:
                default:
                    throw new ArgumentOutOfRangeException("texcoord");
            }
        }

        public static string GetAsShaderString(ColorChannel channel)
        {
            return channel.ToString().ToLowerInvariant();
        }

        public static string GetAsShaderString(Vector2 v)
        {
            return String.Format(CultureInfo.InvariantCulture, "float2({0}, {1})", v.X, v.Y);
        }

        public static string GetAsShaderString(Vector3 v)
        {
            return String.Format(CultureInfo.InvariantCulture, "float3({0}, {1}, {2})", v.X, v.Y, v.Z);
        }

        public static string GetAsShaderString(Vector4 v)
        {
            return String.Format(CultureInfo.InvariantCulture, "float4({0}, {1}, {2}, {3})", v.X, v.Y, v.Z, v.W);
        }

        public static string GetAsShaderString(Color4 c)
        {
            return String.Format(CultureInfo.InvariantCulture, "float4({0}, {1}, {2}, {3})", c.R, c.G, c.B, c.A);
        }

        public static string GetAsShaderString(float f)
        {
            return String.Format(CultureInfo.InvariantCulture, "float4({0}, {0}, {0}, {0})", f);
        }

        public static string GetAsShaderString(object obj)
        {
            return obj.ToString();
        }

        /// <summary>
        /// Build a encapsulating ShaderMixinSource if necessary.
        /// </summary>
        /// <param name="shaderSource">The input ShaderSource.</param>
        /// <returns>A ShaderMixinSource</returns>
        public static ShaderMixinSource GetShaderMixinSource(ShaderSource shaderSource)
        {
            if (shaderSource is ShaderClassSource)
            {
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add((ShaderClassSource)shaderSource);
                return mixin;
            }
            if (shaderSource is ShaderMixinSource)
                return (ShaderMixinSource)shaderSource;

            return null;
        }

        /// <summary>
        /// Get the ParameterKey of generic sampler.
        /// </summary>
        /// <param name="i">The id of the texture.</param>
        /// <returns>The corresponding ParameterKey.</returns>
        public static ParameterKey<SamplerState> GetDefaultSamplerKey(int i)
        {
            switch (i)
            {
                case 0:
                    return TexturingKeys.Sampler0;
                case 1:
                    return TexturingKeys.Sampler1;
                case 2:
                    return TexturingKeys.Sampler2;
                case 3:
                    return TexturingKeys.Sampler3;
                case 4:
                    return TexturingKeys.Sampler4;
                case 5:
                    return TexturingKeys.Sampler5;
                case 6:
                    return TexturingKeys.Sampler6;
                case 7:
                    return TexturingKeys.Sampler7;
                case 8:
                    return TexturingKeys.Sampler8;
                case 9:
                    return TexturingKeys.Sampler9;
                default:
                    throw new ArgumentOutOfRangeException("Asked for " + i + " but no more than 10 default textures are currently supported");
            }
        }
    }
}