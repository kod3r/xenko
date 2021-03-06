﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Xenko Shader Mixin Code Generator.
// To generate it yourself, please install SiliconStudio.Xenko.VisualStudio.Package .vsix
// and re-save the associated .xkfx.
// </auto-generated>

using System;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Shaders;
using SiliconStudio.Core.Mathematics;
using Buffer = SiliconStudio.Xenko.Graphics.Buffer;

namespace SiliconStudio.Xenko.Rendering
{
    public static partial class TransformationKeys
    {
        public static readonly ParameterKey<Matrix> View = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> ViewInverse = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> Projection = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> ProjectionInverse = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> ViewProjection = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Vector2> ProjScreenRay = ParameterKeys.New<Vector2>();
        public static readonly ParameterKey<Vector4> Eye = ParameterKeys.New<Vector4>();
        public static readonly ParameterKey<Vector4> EyeMS = ParameterKeys.New<Vector4>();
        public static readonly ParameterKey<Matrix> World = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> WorldInverseTranspose = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> WorldInverse = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> WorldView = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> WorldViewInverse = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Matrix> WorldViewProjection = ParameterKeys.New<Matrix>();
        public static readonly ParameterKey<Vector3> WorldScale = ParameterKeys.New<Vector3>();
    }
}
