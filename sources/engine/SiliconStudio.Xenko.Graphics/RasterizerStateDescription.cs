﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Runtime.InteropServices;

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Graphics
{
    /// <summary>
    /// Describes a rasterizer state.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct RasterizerStateDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RasterizerStateDescription"/> class.
        /// </summary>
        /// <param name="cullMode">The cull mode.</param>
        public RasterizerStateDescription(CullMode cullMode) : this()
        {
            SetDefault();
            CullMode = cullMode;
        }

        /// <summary>
        /// Determines the fill mode to use when rendering (see <see cref="FillMode"/>).
        /// </summary>
        public FillMode FillMode;

        /// <summary>
        /// Indicates triangles facing the specified direction are not drawn (see <see cref="CullMode"/>).
        /// </summary>
        public CullMode CullMode;

        /// <summary>
        /// Determines if a triangle is front- or back-facing. If this parameter is true, then a triangle will be considered front-facing if its vertices are counter-clockwise on the render target and considered back-facing if they are clockwise. If this parameter is false then the opposite is true.
        /// </summary>
        public bool FrontFaceCounterClockwise;

        /// <summary>
        /// Depth value added to a given pixel. 
        /// </summary>
        public int DepthBias;

        /// <summary>
        /// Gets or sets the depth bias for polygons, which is the amount of bias to apply to the depth of a primitive to alleviate depth testing problems for primitives of similar depth. The default value is 0.
        /// </summary>
        public float DepthBiasClamp;

        /// <summary>
        /// Scalar on a given pixel's slope. 
        /// </summary>
        public float SlopeScaleDepthBias;

        /// <summary>
        /// Enable clipping based on distance. 
        /// </summary>
        public bool DepthClipEnable;

        /// <summary>
        /// Enable scissor-rectangle culling. All pixels ouside an active scissor rectangle are culled.
        /// </summary>
        public bool ScissorTestEnable;

        /// <summary>
        /// Enable multisample antialiasing.
        /// </summary>
        public bool MultiSampleAntiAlias;

        /// <summary>
        /// Enable line antialiasing; only applies if doing line drawing and MultisampleEnable is false.
        /// </summary>
        public bool MultiSampleAntiAliasLine;

        /// <summary>
        /// Sets default values for this instance.
        /// </summary>
        public void SetDefault()
        {
            CullMode = CullMode.Back;
            FillMode = FillMode.Solid;
            DepthClipEnable = true;
            FrontFaceCounterClockwise = false;
            ScissorTestEnable = false;
            MultiSampleAntiAlias = false;
            MultiSampleAntiAliasLine = false;
            DepthBias = 0;
            DepthBiasClamp = 0f;
            SlopeScaleDepthBias = 0f;
        }

        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        public static RasterizerStateDescription Default
        {
            get
            {
                var desc = new RasterizerStateDescription();
                desc.SetDefault();
                return desc;
            }
        }
    }
}