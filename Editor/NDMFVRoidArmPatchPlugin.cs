using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;

[assembly: ExportsPlugin(typeof(NDMFVRoidArmPatch.Editor.NDMFVRoidArmPatchPlugin))]

namespace NDMFVRoidArmPatch.Editor
{
    public sealed class NDMFVRoidArmPatchPlugin : Plugin<NDMFVRoidArmPatchPlugin>
    {
        public override string QualifiedName => "jp.yoridrill.ndmf-vroid-arm-patch";
        public override string DisplayName => "NDMF VRoid Arm Patch";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Build NDMF VRoid Arm Patch rig (Before MA)", ctx =>
                {
                    ApplyFix(ctx, PatchBuildOrder.BeforeModularAvatar);
                });

            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("Build NDMF VRoid Arm Patch rig (After MA)", ctx =>
                {
                    ApplyFix(ctx, PatchBuildOrder.AfterModularAvatar);
                });

            InPhase(BuildPhase.Optimizing)
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .Run("Reset preview and remove patch components", ctx =>
                {
                    NDMFVRoidArmPatchPreviewUtility.ResetAllPreviewArtifacts();
                    RemovePatchComponentsBeforeAO(ctx);
                });
        }

        private static void ApplyFix(BuildContext ctx, PatchBuildOrder currentPassOrder)
        {
            if (ctx == null || ctx.AvatarRootObject == null) return;

            var components = ctx.AvatarRootObject.GetComponentsInChildren<NDMFVRoidArmPatchComponent>(true);
            if (components == null || components.Length == 0) return;

            var settings = Aggregate(components);
            if (settings.buildOrder != currentPassOrder) return;

            var animator = ctx.AvatarRootObject.GetComponentInChildren<Animator>(true);
            if (!IsValidHumanoid(animator))
            {
                Debug.LogWarning("[NDMF VRoid Arm Patch] Humanoid Animator not found. Skipped.");
                return;
            }

            var replaceMap = new Dictionary<Transform, Transform>();

            if (settings.enableShoulderFix)
            {
                BuildShoulderFix(animator, settings, replaceMap);
            }

            if (settings.enableWristFix)
            {
                BuildWristFix(animator, settings, replaceMap);
            }

            if (settings.enableThumbFix)
            {
                BuildThumbFix(animator, settings, replaceMap);
            }

            RebindRenderers(ctx.AvatarRootObject, replaceMap, settings.verboseLog);
            RemoveComponents(components);

            if (settings.verboseLog)
            {
                Debug.Log(
                    $"[NDMF VRoid Arm Patch] Finished. replaceMapCount={replaceMap.Count}, " +
                    $"mode={settings.constraintMode}, order={settings.buildOrder}");
            }
        }

        private static void RemovePatchComponentsBeforeAO(BuildContext ctx)
        {
            if (ctx == null || ctx.AvatarRootObject == null) return;

            var components = ctx.AvatarRootObject.GetComponentsInChildren<NDMFVRoidArmPatchComponent>(true);
            RemoveComponents(components);
        }

        public static void BuildPatchRig(GameObject avatarRoot, NDMFVRoidArmPatchComponent component, bool verboseLog = false)
        {
            if (avatarRoot == null || component == null) return;

            var animator = avatarRoot.GetComponentInChildren<Animator>(true);
            if (!IsValidHumanoid(animator))
            {
                Debug.LogWarning("[NDMF VRoid Arm Patch] Preview skipped. Humanoid Animator not found.");
                return;
            }

            var settings = new AggregatedSettings
            {
                enableShoulderFix = component.enableShoulderFix,
                shoulderPositionOffset = component.shoulderPositionOffset,
                shoulderEulerOffset = component.shoulderEulerOffset,
                upperArmTwistAxis = component.upperArmTwistAxis,
                upperArmTwistWeight = component.upperArmTwistWeight,
                enableWristFix = component.enableWristFix,
                wristPositionOffset = component.wristPositionOffset,
                wristThicknessScale = component.wristThicknessScale,
                wristWidthScale = component.wristWidthScale,
                wristTwistAxis = component.wristTwistAxis,
                wristTwistWeight = component.wristTwistWeight,
                enableThumbFix = component.enableThumbFix,
                thumbEulerOffset = component.thumbEulerOffset,
                constraintMode = component.constraintMode,
                buildOrder = component.buildOrder,
                verboseLog = verboseLog || component.verboseLog
            };

            var replaceMap = new Dictionary<Transform, Transform>();

            if (settings.enableShoulderFix)
            {
                BuildShoulderFix(animator, settings, replaceMap);
            }

            if (settings.enableWristFix)
            {
                BuildWristFix(animator, settings, replaceMap);
            }

            if (settings.enableThumbFix)
            {
                BuildThumbFix(animator, settings, replaceMap);
            }

            RebindRenderers(avatarRoot, replaceMap, settings.verboseLog);
        }

        private static bool IsValidHumanoid(Animator animator)
        {
            return animator != null && animator.avatar != null && animator.avatar.isHuman;
        }

        private static void BuildShoulderFix(
            Animator animator,
            AggregatedSettings settings,
            Dictionary<Transform, Transform> replaceMap)
        {
            BuildShoulderSide(
                "L",
                animator.GetBoneTransform(HumanBodyBones.LeftShoulder),
                animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                settings.shoulderPositionOffset,
                settings.shoulderEulerOffset,
                settings.upperArmTwistAxis,
                settings.upperArmTwistWeight,
                settings.constraintMode,
                settings.verboseLog,
                replaceMap
            );

            BuildShoulderSide(
                "R",
                animator.GetBoneTransform(HumanBodyBones.RightShoulder),
                animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
                settings.shoulderPositionOffset,
                MirrorOffsetForRight(settings.shoulderEulerOffset),
                settings.upperArmTwistAxis,
                settings.upperArmTwistWeight,
                settings.constraintMode,
                settings.verboseLog,
                replaceMap
            );
        }

        private static void BuildShoulderSide(
            string sideLabel,
            Transform originalShoulder,
            Transform originalUpperArm,
            Transform originalLowerArm,
            Vector3 shoulderPositionOffset,
            Vector3 shoulderEulerOffset,
            TwistAxis twistAxis,
            float twistWeight,
            ConstraintMode constraintMode,
            bool verboseLog,
            Dictionary<Transform, Transform> replaceMap)
        {
            if (originalShoulder == null || originalUpperArm == null || originalLowerArm == null)
            {
                Debug.LogWarning($"[NDMF VRoid Arm Patch] [{sideLabel}] Shoulder fix skipped. Required bones not found.");
                return;
            }

            var shoulderLocalOffset = ConvertParentSpaceOffsetToChildLocal(originalShoulder, shoulderPositionOffset);

            var shoulderDef = CreateChildOffsetBone(
                originalShoulder.name + "_Def",
                originalShoulder,
                shoulderLocalOffset,
                shoulderEulerOffset
            );

            var upperArmAim = CreateChildCopiedLocalBone(
                originalUpperArm.name + "_Aim",
                shoulderDef,
                originalUpperArm
            );

            var upperArmDef = CreateChildAlignedBone(
                originalUpperArm.name + "_Def",
                upperArmAim
            );

            if (constraintMode == ConstraintMode.VRChatConstraints)
            {
                AddVRCUpperArmAimConstraint(upperArmAim, originalLowerArm, sideLabel);
                AddVRCUpperArmTwistConstraint(upperArmDef, originalUpperArm, twistAxis, twistWeight);
            }
            else
            {
                AddUnityUpperArmAimConstraint(upperArmAim, originalLowerArm, sideLabel);
                AddUnityUpperArmTwistConstraint(upperArmDef, originalUpperArm, twistAxis, twistWeight);
            }

            replaceMap[originalShoulder] = shoulderDef;
            replaceMap[originalUpperArm] = upperArmDef;

            if (verboseLog)
            {
                Debug.Log(
                    $"[NDMF VRoid Arm Patch] [{sideLabel}] Shoulder fix created. " +
                    $"pos={shoulderPositionOffset}, rot={shoulderEulerOffset}, twistAxis={twistAxis}, twistWeight={twistWeight:F2}");
            }
        }

        private static void BuildWristFix(
            Animator animator,
            AggregatedSettings settings,
            Dictionary<Transform, Transform> replaceMap)
        {
            BuildWristSide(
                "L",
                animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                animator.GetBoneTransform(HumanBodyBones.LeftHand),
                settings.wristPositionOffset,
                settings.wristThicknessScale,
                settings.wristWidthScale,
                settings.wristTwistAxis,
                settings.wristTwistWeight,
                settings.constraintMode,
                settings.verboseLog,
                replaceMap
            );

            BuildWristSide(
                "R",
                animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
                animator.GetBoneTransform(HumanBodyBones.RightHand),
                MirrorOffsetForRight(settings.wristPositionOffset),
                settings.wristThicknessScale,
                settings.wristWidthScale,
                settings.wristTwistAxis,
                settings.wristTwistWeight,
                settings.constraintMode,
                settings.verboseLog,
                replaceMap
            );
        }

        private static void BuildWristSide(
            string sideLabel,
            Transform originalLowerArm,
            Transform originalHand,
            Vector3 wristPositionOffset,
            float thicknessScale,
            float widthScale,
            TwistAxis wristTwistAxis,
            float wristTwistWeight,
            ConstraintMode constraintMode,
            bool verboseLog,
            Dictionary<Transform, Transform> replaceMap)
        {
            if (originalLowerArm == null)
            {
                Debug.LogWarning($"[NDMF VRoid Arm Patch] [{sideLabel}] Wrist fix skipped. LowerArm not found.");
                return;
            }

            var wristLocalOffset = ConvertParentSpaceOffsetToChildLocal(originalLowerArm, wristPositionOffset);

            var wristDef = CreateChildOffsetBone(
                originalLowerArm.name + "_Wrist_Def",
                originalLowerArm,
                wristLocalOffset,
                Vector3.zero
            );

            wristDef.localScale = BuildWristScaleVector(wristTwistAxis, thicknessScale, widthScale);

            if (originalHand == null)
            {
                Debug.LogWarning($"[NDMF VRoid Arm Patch] [{sideLabel}] Wrist rotate part skipped. Hand not found.");
            }
            else if (constraintMode == ConstraintMode.VRChatConstraints)
            {
                AddVRCWristRotateConstraint(wristDef, originalHand, wristTwistAxis, wristTwistWeight);
            }
            else
            {
                AddUnityWristRotateConstraint(wristDef, originalHand, wristTwistAxis, wristTwistWeight);
            }

            replaceMap[originalLowerArm] = wristDef;

            if (verboseLog)
            {
                Debug.Log(
                    $"[NDMF VRoid Arm Patch] [{sideLabel}] Wrist fix created. " +
                    $"mode={constraintMode}, pos={wristPositionOffset}, thickness={thicknessScale:F3}, width={widthScale:F3}, " +
                    $"twistAxis={wristTwistAxis}, twistWeight={wristTwistWeight:F2}");
            }
        }

        private static void BuildThumbFix(
            Animator animator,
            AggregatedSettings settings,
            Dictionary<Transform, Transform> replaceMap)
        {
            BuildThumbSide(
                "L",
                animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal),
                animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate),
                animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal),
                settings.thumbEulerOffset,
                settings.constraintMode,
                settings.verboseLog,
                replaceMap
            );

            BuildThumbSide(
                "R",
                animator.GetBoneTransform(HumanBodyBones.RightThumbProximal),
                animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate),
                animator.GetBoneTransform(HumanBodyBones.RightThumbDistal),
                MirrorOffsetForRight(settings.thumbEulerOffset),
                settings.constraintMode,
                settings.verboseLog,
                replaceMap
            );
        }

        private static void BuildThumbSide(
            string sideLabel,
            Transform originalProximal,
            Transform originalIntermediate,
            Transform originalDistal,
            Vector3 eulerOffset,
            ConstraintMode constraintMode,
            bool verboseLog,
            Dictionary<Transform, Transform> replaceMap)
        {
            if (originalProximal == null || originalIntermediate == null || originalDistal == null)
            {
                Debug.LogWarning($"[NDMF VRoid Arm Patch] [{sideLabel}] Thumb fix skipped. Required thumb bones not found.");
                return;
            }

            var proximalParent = originalProximal.parent;
            if (proximalParent == null)
            {
                Debug.LogWarning($"[NDMF VRoid Arm Patch] [{sideLabel}] Thumb fix skipped. Thumb parent not found.");
                return;
            }

            var proximalDef = CreateSiblingBone(originalProximal.name + "_Def", proximalParent, originalProximal);
            var intermediateDef = CreateSiblingBone(originalIntermediate.name + "_Def", proximalDef, originalIntermediate);
            var distalDef = CreateSiblingBone(originalDistal.name + "_Def", intermediateDef, originalDistal);

            if (constraintMode == ConstraintMode.VRChatConstraints)
            {
                AddVRCRotationConstraintAllAxes(proximalDef, originalProximal, eulerOffset);
                AddVRCRotationConstraintAllAxes(intermediateDef, originalIntermediate, eulerOffset);
                AddVRCRotationConstraintAllAxes(distalDef, originalDistal, eulerOffset);
            }
            else
            {
                AddUnityRotationConstraintAllAxes(proximalDef, originalProximal, eulerOffset);
                AddUnityRotationConstraintAllAxes(intermediateDef, originalIntermediate, eulerOffset);
                AddUnityRotationConstraintAllAxes(distalDef, originalDistal, eulerOffset);
            }

            replaceMap[originalProximal] = proximalDef;
            replaceMap[originalIntermediate] = intermediateDef;
            replaceMap[originalDistal] = distalDef;

            if (verboseLog)
            {
                Debug.Log($"[NDMF VRoid Arm Patch] [{sideLabel}] Thumb constraints created. mode={constraintMode}, rot={eulerOffset}");
            }
        }

        // Bone creation helpers

        private static Vector3 ConvertParentSpaceOffsetToChildLocal(Transform childParent, Vector3 parentSpaceOffset)
        {
            if (childParent == null) return parentSpaceOffset;

            var parent = childParent.parent;
            if (parent == null) return parentSpaceOffset;

            Vector3 worldOffset = parent.TransformVector(parentSpaceOffset);
            return childParent.InverseTransformVector(worldOffset);
        }

        private static Transform CreateBoneWithLocal(
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            t.localScale = localScale;
            return t;
        }

        private static Transform CreateSiblingBone(string name, Transform parent, Transform source)
        {
            return CreateBoneWithLocal(
                name,
                parent,
                source.localPosition,
                source.localRotation,
                source.localScale
            );
        }

        private static Transform CreateChildAlignedBone(string name, Transform parent)
        {
            return CreateBoneWithLocal(
                name,
                parent,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one
            );
        }

        private static Transform CreateChildCopiedLocalBone(string name, Transform parent, Transform source)
        {
            return CreateBoneWithLocal(
                name,
                parent,
                source.localPosition,
                source.localRotation,
                source.localScale
            );
        }

        private static Transform CreateChildOffsetBone(string name, Transform parent, Vector3 localPositionOffset, Vector3 localEulerOffset)
        {
            return CreateBoneWithLocal(
                name,
                parent,
                localPositionOffset,
                Quaternion.Euler(localEulerOffset),
                Vector3.one
            );
        }

        // VRChat constraints

        private static void AddVRCUpperArmAimConstraint(
            Transform target,
            Transform lowerArm,
            string sideLabel)
        {
            var constraint = target.gameObject.AddComponent<VRCAimConstraint>();

            var localAim = lowerArm.localPosition;
            if (localAim.sqrMagnitude < 1e-8f)
            {
                localAim = sideLabel == "L" ? Vector3.right : Vector3.left;
            }

            constraint.IsActive = true;
            constraint.GlobalWeight = 1f;
            constraint.Locked = true;
            constraint.SolveInLocalSpace = false;
            constraint.FreezeToWorld = false;
            constraint.RebakeOffsetsWhenUnfrozen = false;

            constraint.AffectsRotationX = true;
            constraint.AffectsRotationY = true;
            constraint.AffectsRotationZ = true;

            constraint.AimAxis = localAim.normalized;
            constraint.UpAxis = Vector3.up;
            constraint.WorldUp = VRCConstraintBase.WorldUpType.SceneUp;
            constraint.WorldUpVector = Vector3.up;

            constraint.Sources.Clear();
            constraint.Sources.Add(new VRCConstraintSource(lowerArm, 1f));

            constraint.ApplyConfigurationChanges();
        }

        private static void AddVRCUpperArmTwistConstraint(
            Transform target,
            Transform source,
            TwistAxis twistAxis,
            float twistWeight)
        {
            var constraint = target.gameObject.AddComponent<VRCRotationConstraint>();

            constraint.IsActive = true;
            constraint.GlobalWeight = twistWeight;
            constraint.Locked = true;
            constraint.SolveInLocalSpace = false;
            constraint.FreezeToWorld = false;
            constraint.RebakeOffsetsWhenUnfrozen = false;

            constraint.RotationAtRest = target.localEulerAngles;
            constraint.RotationOffset = Vector3.zero;

            SetVRCRotationAxis(constraint, twistAxis);

            constraint.Sources.Clear();
            constraint.Sources.Add(new VRCConstraintSource(source, 1f));

            constraint.ApplyConfigurationChanges();
        }

        private static void AddVRCWristRotateConstraint(
            Transform target,
            Transform handSource,
            TwistAxis twistAxis,
            float twistWeight)
        {
            var constraint = target.gameObject.AddComponent<VRCRotationConstraint>();

            constraint.IsActive = true;
            constraint.GlobalWeight = twistWeight;
            constraint.Locked = true;
            constraint.SolveInLocalSpace = false;
            constraint.FreezeToWorld = false;
            constraint.RebakeOffsetsWhenUnfrozen = false;

            constraint.RotationAtRest = target.localEulerAngles;
            constraint.RotationOffset = Vector3.zero;

            SetVRCRotationAxis(constraint, twistAxis);

            constraint.Sources.Clear();
            constraint.Sources.Add(new VRCConstraintSource(handSource, 1f));

            constraint.ApplyConfigurationChanges();
        }

        private static void AddVRCRotationConstraintAllAxes(Transform target, Transform source, Vector3 eulerOffset)
        {
            var constraint = target.gameObject.AddComponent<VRCRotationConstraint>();

            constraint.IsActive = true;
            constraint.GlobalWeight = 1f;
            constraint.Locked = true;
            constraint.SolveInLocalSpace = false;
            constraint.FreezeToWorld = false;
            constraint.RebakeOffsetsWhenUnfrozen = false;

            constraint.RotationAtRest = target.localEulerAngles;
            constraint.RotationOffset = eulerOffset;

            constraint.AffectsRotationX = true;
            constraint.AffectsRotationY = true;
            constraint.AffectsRotationZ = true;

            constraint.Sources.Clear();
            constraint.Sources.Add(new VRCConstraintSource(source, 1f));

            constraint.ApplyConfigurationChanges();
        }

        private static void SetVRCRotationAxis(VRCRotationConstraint constraint, TwistAxis axis)
        {
            constraint.AffectsRotationX = axis == TwistAxis.X;
            constraint.AffectsRotationY = axis == TwistAxis.Y;
            constraint.AffectsRotationZ = axis == TwistAxis.Z;
        }

        // Unity constraints

        private static void AddUnityUpperArmAimConstraint(
            Transform target,
            Transform lowerArm,
            string sideLabel)
        {
            var constraint = target.gameObject.AddComponent<AimConstraint>();
            constraint.constraintActive = false;
            constraint.locked = false;
            constraint.weight = 1f;
            constraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;

            var src = new ConstraintSource
            {
                sourceTransform = lowerArm,
                weight = 1f
            };
            constraint.AddSource(src);

            var localAim = lowerArm.localPosition;
            if (localAim.sqrMagnitude < 1e-8f)
            {
                localAim = sideLabel == "L" ? Vector3.right : Vector3.left;
            }

            constraint.aimVector = localAim.normalized;
            constraint.upVector = Vector3.up;
            constraint.worldUpType = AimConstraint.WorldUpType.SceneUp;
            constraint.constraintActive = true;
            constraint.locked = true;
        }

        private static void AddUnityUpperArmTwistConstraint(
            Transform target,
            Transform source,
            TwistAxis twistAxis,
            float twistWeight)
        {
            var constraint = target.gameObject.AddComponent<RotationConstraint>();
            constraint.constraintActive = false;
            constraint.locked = false;
            constraint.weight = twistWeight;
            constraint.rotationAxis = GetUnityRotationAxis(twistAxis);

            var src = new ConstraintSource
            {
                sourceTransform = source,
                weight = 1f
            };

            constraint.AddSource(src);
            constraint.rotationOffset = Vector3.zero;
            constraint.constraintActive = true;
            constraint.locked = true;
        }

        private static void AddUnityWristRotateConstraint(
            Transform target,
            Transform handSource,
            TwistAxis twistAxis,
            float twistWeight)
        {
            var constraint = target.gameObject.AddComponent<RotationConstraint>();
            constraint.constraintActive = false;
            constraint.locked = false;
            constraint.weight = twistWeight;
            constraint.rotationAxis = GetUnityRotationAxis(twistAxis);

            var src = new ConstraintSource
            {
                sourceTransform = handSource,
                weight = 1f
            };

            constraint.AddSource(src);
            constraint.rotationOffset = Vector3.zero;
            constraint.constraintActive = true;
            constraint.locked = true;
        }

        private static void AddUnityRotationConstraintAllAxes(Transform target, Transform source, Vector3 eulerOffset)
        {
            var constraint = target.gameObject.AddComponent<RotationConstraint>();
            constraint.constraintActive = false;
            constraint.locked = false;
            constraint.rotationAxis = Axis.X | Axis.Y | Axis.Z;
            constraint.weight = 1f;

            var src = new ConstraintSource
            {
                sourceTransform = source,
                weight = 1f
            };

            constraint.AddSource(src);
            constraint.rotationOffset = eulerOffset;
            constraint.constraintActive = true;
            constraint.locked = true;
        }

        private static Axis GetUnityRotationAxis(TwistAxis axis)
        {
            switch (axis)
            {
                case TwistAxis.X: return Axis.X;
                case TwistAxis.Y: return Axis.Y;
                case TwistAxis.Z: return Axis.Z;
                default: return Axis.X;
            }
        }

        // Misc helpers

        private static Vector3 BuildWristScaleVector(TwistAxis twistAxis, float thicknessScale, float widthScale)
        {
            switch (twistAxis)
            {
                case TwistAxis.X:
                    return new Vector3(1f, thicknessScale, widthScale);

                case TwistAxis.Y:
                    return new Vector3(widthScale, 1f, thicknessScale);

                case TwistAxis.Z:
                    return new Vector3(widthScale, thicknessScale, 1f);

                default:
                    return new Vector3(1f, thicknessScale, widthScale);
            }
        }

        private static Vector3 MirrorOffsetForRight(Vector3 leftLikeOffset)
        {
            return new Vector3(leftLikeOffset.x, -leftLikeOffset.y, -leftLikeOffset.z);
        }

        private static void RebindRenderers(GameObject avatarRoot, Dictionary<Transform, Transform> replaceMap, bool verboseLog)
        {
            var renderers = avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int changedRendererCount = 0;

            foreach (var smr in renderers)
            {
                if (smr == null || smr.bones == null || smr.bones.Length == 0) continue;

                bool changed = false;
                var bones = smr.bones;

                for (int i = 0; i < bones.Length; i++)
                {
                    var bone = bones[i];
                    if (bone == null) continue;

                    if (replaceMap.TryGetValue(bone, out var replacement) && replacement != null)
                    {
                        bones[i] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                {
                    smr.bones = bones;
                    changedRendererCount++;

                    if (verboseLog)
                    {
                        Debug.Log($"[NDMF VRoid Arm Patch] Rebound renderer: {GetPath(smr.transform)}");
                    }
                }
            }

            if (verboseLog)
            {
                Debug.Log($"[NDMF VRoid Arm Patch] Renderer rebinding finished. changedRendererCount={changedRendererCount}");
            }
        }

        private static AggregatedSettings Aggregate(NDMFVRoidArmPatchComponent[] components)
        {
            if (components.Length > 1)
            {
                Debug.LogWarning("[NDMF VRoid Arm Patch] Multiple components found. Last one will be used.");
            }

            var c = components[components.Length - 1];

            return new AggregatedSettings
            {
                enableShoulderFix = c.enableShoulderFix,
                shoulderPositionOffset = c.shoulderPositionOffset,
                shoulderEulerOffset = c.shoulderEulerOffset,
                upperArmTwistAxis = c.upperArmTwistAxis,
                upperArmTwistWeight = c.upperArmTwistWeight,
                enableWristFix = c.enableWristFix,
                wristPositionOffset = c.wristPositionOffset,
                wristThicknessScale = c.wristThicknessScale,
                wristWidthScale = c.wristWidthScale,
                wristTwistAxis = c.wristTwistAxis,
                wristTwistWeight = c.wristTwistWeight,
                enableThumbFix = c.enableThumbFix,
                thumbEulerOffset = c.thumbEulerOffset,
                constraintMode = c.constraintMode,
                buildOrder = c.buildOrder,
                verboseLog = c.verboseLog
            };
        }

        private static void RemoveComponents(NDMFVRoidArmPatchComponent[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                {
                    Object.DestroyImmediate(components[i]);
                }
            }
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        private struct AggregatedSettings
        {
            public bool enableShoulderFix;
            public Vector3 shoulderPositionOffset;
            public Vector3 shoulderEulerOffset;
            public TwistAxis upperArmTwistAxis;
            public float upperArmTwistWeight;
            public bool enableWristFix;
            public Vector3 wristPositionOffset;
            public float wristThicknessScale;
            public float wristWidthScale;
            public TwistAxis wristTwistAxis;
            public float wristTwistWeight;
            public bool enableThumbFix;
            public Vector3 thumbEulerOffset;
            public ConstraintMode constraintMode;
            public PatchBuildOrder buildOrder;
            public bool verboseLog;
        }
    }
}
