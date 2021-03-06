using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering;
using System;

namespace SiliconStudio.Xenko.Physics
{
    public class PhysicsDebugShapeRendering : IDisposable
    {
        private readonly Material triggerMaterial;
        private readonly Material staticMaterial;
        private readonly Material dynamicMaterial;
        private readonly Material kinematicMaterial;
        private readonly Material characterMaterial;
        private readonly RasterizerState rasterizer;
        private readonly GraphicsDevice graphicsDevice;

        public PhysicsDebugShapeRendering(GraphicsDevice device)
        {
            graphicsDevice = device;

            triggerMaterial = PhysicsDebugShapeMaterial.Create(graphicsDevice, Color.AdjustSaturation(Color.Purple, 0.77f), 1);
            staticMaterial = PhysicsDebugShapeMaterial.Create(graphicsDevice, Color.AdjustSaturation(Color.Red, 0.77f), 1);
            dynamicMaterial = PhysicsDebugShapeMaterial.Create(graphicsDevice, Color.AdjustSaturation(Color.Green, 0.77f), 1);
            kinematicMaterial = PhysicsDebugShapeMaterial.Create(graphicsDevice, Color.AdjustSaturation(Color.Blue, 0.77f), 1);
            characterMaterial = PhysicsDebugShapeMaterial.Create(graphicsDevice, Color.AdjustSaturation(Color.Yellow, 0.77f), 1);

            rasterizer = RasterizerState.New(graphicsDevice, new RasterizerStateDescription(CullMode.None) { FillMode = FillMode.Wireframe });

            triggerMaterial.Parameters.Set(Effect.RasterizerStateKey, rasterizer);
            staticMaterial.Parameters.Set(Effect.RasterizerStateKey, rasterizer);
            dynamicMaterial.Parameters.Set(Effect.RasterizerStateKey, rasterizer);
            kinematicMaterial.Parameters.Set(Effect.RasterizerStateKey, rasterizer);
            characterMaterial.Parameters.Set(Effect.RasterizerStateKey, rasterizer);
        }

        public Entity CreateDebugEntity(PhysicsElementBase physicsElement)
        {
            if (physicsElement?.ColliderShape == null) return null;

            if (physicsElement.DebugEntity != null) return null;

            var debugEntity = new Entity();

            var colliderEntity = CreateChildEntity(physicsElement.ColliderShape, physicsElement.Type, true);
            if (colliderEntity == null) return null;

            debugEntity.AddChild(colliderEntity);

            if (physicsElement.CanScaleShape)
            {
                debugEntity.Transform.Scale = physicsElement.ColliderShape.Scaling;
            }

            var skinnedElement = physicsElement as PhysicsSkinnedElementBase;
            if (skinnedElement != null && skinnedElement.BoneIndex != -1)
            {
                Vector3 scale, pos;
                Quaternion rot;
                skinnedElement.BoneWorldMatrixOut.Decompose(out scale, out rot, out pos);
                debugEntity.Transform.Position = pos;
                debugEntity.Transform.Rotation = rot;
            }
            else
            {
                Vector3 scale, pos;
                Quaternion rot;
                physicsElement.Data.TransformComponent.WorldMatrix.Decompose(out scale, out rot, out pos);
                debugEntity.Transform.Position = pos;
                debugEntity.Transform.Rotation = rot;
            }

            return debugEntity;
        }

        private Entity CreateChildEntity(ColliderShape shape, PhysicsElementBase.Types type, bool addOffset = false)
        {
            if (shape == null)
                return null;

            switch (shape.Type)
            {
                case ColliderShapeTypes.StaticPlane:
                    {
                        //Hmm TODO maybe can draw an infinite plane??
                        return null;
                    }
                case ColliderShapeTypes.Compound:
                    {
                        var entity = new Entity();

                        //We got to recurse
                        var compound = (CompoundColliderShape)shape;
                        for (var i = 0; i < compound.Count; i++)
                        {
                            var subShape = compound[i];
                            var subEntity = CreateChildEntity(subShape, type, true);

                            subEntity.Transform.UseTRS = false;
                            entity.AddChild(subEntity);
                        }

                        entity.Transform.LocalMatrix = Matrix.Identity;
                        entity.Transform.UseTRS = false;

                        return entity;
                    }
                default:
                    {
                        Material mat;
                        switch (type)
                        {
                            case PhysicsElementBase.Types.PhantomCollider:
                                mat = triggerMaterial;
                                break;

                            case PhysicsElementBase.Types.StaticCollider:
                            case PhysicsElementBase.Types.StaticRigidBody:
                                mat = staticMaterial;
                                break;

                            case PhysicsElementBase.Types.DynamicRigidBody:
                                mat = dynamicMaterial;
                                break;

                            case PhysicsElementBase.Types.KinematicRigidBody:
                                mat = kinematicMaterial;
                                break;

                            case PhysicsElementBase.Types.CharacterController:
                                mat = characterMaterial;
                                break;

                            default:
                                throw new ArgumentOutOfRangeException("type", type, null);
                        }

                        var entity = new Entity
                        {
                            new ModelComponent
                            {
                                Model = new Model
                                {
                                    mat,
                                    new Mesh
                                    {
                                        Draw = shape.CreateDebugPrimitive(graphicsDevice)
                                    }
                                }
                            }
                        };

                        var offset = addOffset ? Matrix.RotationQuaternion(shape.LocalRotation)*Matrix.Translation(shape.LocalOffset) : Matrix.Identity;

                        if (shape.Type == ColliderShapeTypes.ConvexHull)
                        {
                            var hullDesc = (ConvexHullColliderShape)shape;

                            entity.Transform.LocalMatrix = shape.DebugPrimitiveMatrix * Matrix.Scaling(hullDesc.Scaling) * offset;
                        }
                        else
                        {
                            entity.Transform.LocalMatrix = shape.DebugPrimitiveMatrix * offset;
                        }
                        entity.Transform.UseTRS = false;

                        return entity;
                    }
            }
        }

        public void Dispose()
        {
            rasterizer.Dispose();
        }
    }
}