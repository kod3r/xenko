﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;
using SiliconStudio.Paradox.Effects;

namespace SiliconStudio.Paradox.Engine.Graphics
{
    /// <summary>
    /// A default implementation for a <see cref="IEntityComponentRenderer"/>.
    /// </summary>
    public abstract class EntityComponentRendererCoreBase : RendererCoreBase, IEntityComponentRendererCore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityComponentRendererCoreBase" /> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">services</exception>
        protected EntityComponentRendererCoreBase()
        {
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets the entity system.
        /// </summary>
        /// <value>The entity system.</value>
        public SceneInstance SceneInstance { get; private set; }

        /// <summary>
        /// Gets the effect system.
        /// </summary>
        /// <value>The effect system.</value>
        public EffectSystem EffectSystem { get; private set; }

        /// <summary>
        /// Gets the camera renderer.
        /// </summary>
        /// <value>The camera renderer.</value>
        public SceneCameraRenderer SceneCameraRenderer { get; private set; }

        /// <summary>
        /// Gets the current render frame. Only valid from <see cref="RendererBase.DrawCore"/> method.
        /// </summary>
        /// <value>The current render frame.</value>
        public RenderFrame CurrentRenderFrame { get; private set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();
            Services = Context.Services;
            EffectSystem = Services.GetSafeServiceAs<EffectSystem>();
            SceneInstance = Context.Tags.GetSafe(SceneInstance.Current);
            SceneCameraRenderer = Context.Tags.GetSafe(SceneCameraRenderer.Current);
        }

        protected override void PreDrawCore(RenderContext context)
        {
            base.PreDrawCore(context);
            CurrentRenderFrame = context.Tags.GetSafe(RenderFrame.Current);
        }

        protected override void PostDrawCore(RenderContext context)
        {
            base.PostDrawCore(context);
            CurrentRenderFrame = null;
        }
    }
}