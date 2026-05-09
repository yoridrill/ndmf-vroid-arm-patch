using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

namespace NDMFVRoidArmPatch
{
    public enum TwistAxis
    {
        X,
        Y,
        Z
    }

    public enum WristTwistBoneType
    {
        None,
        AllTwist,
        SkinOnly
    }

    public enum WristTwistBoneCount
    {
        Count4 = 4,
        Count6 = 6,
        Count8 = 8,
        Count12 = 12
    }

    public enum ConstraintMode
    {
        VRChatConstraints,
        UnityConstraints
    }

    public enum PatchBuildOrder
    {
        AfterModularAvatar,
        BeforeModularAvatar
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("yoridrill/NDMF VRoid Arm Patch")]
    public sealed class NDMFVRoidArmPatchComponent : MonoBehaviour, IEditorOnly
    {
        [Header("Shoulder")]
        [Tooltip("Enable shoulder correction.")]
        public bool enableShoulderFix = true;

        [Tooltip("Shared shoulder position offset. Right side is mirrored internally.")]
        public Vector3 shoulderPositionOffset = Vector3.zero;

        [Tooltip("Shared shoulder rotation offset. Right side is mirrored internally.")]
        public Vector3 shoulderEulerOffset = new Vector3(0f, 0f, -10f);

        [Tooltip("Upper arm roll axis. Default is X.")]
        [FormerlySerializedAs("upperArmTwistAxis")]
        public TwistAxis upperArmRollAxis = TwistAxis.X;

        [Tooltip("How strongly the roll axis follows the original upper arm.")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("upperArmTwistWeight")]
        public float upperArmRollWeight = 1f;

        [Header("Wrist")]
        [Tooltip("Enable wrist correction.")]
        public bool enableWristFix = true;

        [Tooltip("Forearm thickness scale.")]
        public float wristThicknessScale = 1f;

        [Tooltip("Forearm width scale.")]
        public float wristWidthScale = 0.92f;

        [Tooltip("Wrist roll axis. Default is X.")]
        [FormerlySerializedAs("wristTwistAxis")]
        public TwistAxis wristRollAxis = TwistAxis.X;

        [Tooltip("How strongly wrist roll follows the hand.")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("wristTwistWeight")]
        public float wristRollWeight = 1f;


        [Tooltip("Wrist twist bone mode. None keeps current behavior.")]
        public WristTwistBoneType wristTwistBoneType = WristTwistBoneType.None;

        [Tooltip("Number of wrist twist bones to use.")]
        public WristTwistBoneCount wristTwistBoneCount = WristTwistBoneCount.Count8;

        [Tooltip("Skin material name used in SkinOnly mode.")]
        public string wristSkinMaterialName = string.Empty;

        [Header("Thumb")]
        [Tooltip("Enable thumb correction.")]
        public bool enableThumbFix = true;

        [Tooltip("Shared thumb rotation offset. Right side is mirrored internally.")]
        public Vector3 thumbEulerOffset = new Vector3(10f, 0f, 20f);

        [Tooltip("Constraint implementation used by this tool.")]
        public ConstraintMode constraintMode = ConstraintMode.VRChatConstraints;

        [Tooltip("When to run this tool relative to Modular Avatar.")]
        public PatchBuildOrder buildOrder = PatchBuildOrder.AfterModularAvatar;

        [Tooltip("Enable verbose logging.")]
        public bool verboseLog = false;
    }
}
