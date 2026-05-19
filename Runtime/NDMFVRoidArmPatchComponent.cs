using UnityEngine;
using VRC.SDKBase;

namespace NDMFVRoidArmPatch
{
    public enum TwistAxis
    {
        X,
        Y,
        Z
    }

    public enum ForearmTwistBoneType
    {
        None,
        AllTwist,
        SkinOnly
    }

    public enum ForearmTwistBoneCount
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
        public TwistAxis upperArmRollAxis = TwistAxis.X;

        [Tooltip("How strongly the roll axis follows the original upper arm.")]
        [Range(0f, 1f)]
        public float upperArmRollWeight = 1f;

        [Header("Forearm")]
        [Tooltip("Enable forearm correction.")]
        public bool enableForearmFix = true;

        [Tooltip("Forearm thickness scale.")]
        public float forearmThicknessScale = 1f;

        [Tooltip("Forearm width scale.")]
        public float forearmWidthScale = 0.92f;

        [Tooltip("Forearm roll axis. Default is X.")]
        public TwistAxis forearmRollAxis = TwistAxis.X;

        [Tooltip("Forearm pitch axis used for twist extractor up vector.")]
        public TwistAxis forearmPitchAxis = TwistAxis.Z;

        [Tooltip("How strongly forearm roll follows the hand.")]
        [Range(0f, 1f)]
        public float forearmRollWeight = 1f;


        [Tooltip("Forearm twist bone mode. None keeps current behavior.")]
        public ForearmTwistBoneType forearmTwistBoneType = ForearmTwistBoneType.None;

        [Tooltip("Number of forearm twist bones to use.")]
        public ForearmTwistBoneCount forearmTwistBoneCount = ForearmTwistBoneCount.Count8;

        [Tooltip("Skin material name used in Forearm SkinOnly mode.")]
        public string forearmSkinMaterialName = string.Empty;

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
