﻿// Copyright (c) 2014-2015 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Xenko.Physics
{
    class XenkoMotionState : BulletSharp.SharpMotionState
    {
        private readonly RigidBody rigidBody;

        public XenkoMotionState(RigidBody rb)
        {
            rigidBody = rb;
        }

        public override void GetWorldTransform(out Matrix transform)
        {
            if (rigidBody.GetWorldTransformCallback != null)
            {
                rigidBody.GetWorldTransformCallback(out transform);
            }
            else
            {
                transform = Matrix.Identity;
            }
        }

        public override void SetWorldTransform(Matrix transform)
        {
            if (rigidBody.SetWorldTransformCallback != null)
            {
                rigidBody.SetWorldTransformCallback(transform);
            }
        }
    }
}