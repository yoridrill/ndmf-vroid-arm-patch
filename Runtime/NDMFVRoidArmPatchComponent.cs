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
    [AddComponentMenu("NDMF VRoid Arm Patch/NDMF VRoid Arm Patch")]
    public sealed class NDMFVRoidArmPatchComponent : MonoBehaviour, IEditorOnly
    {
        [Header("Shoulder")]
        [Tooltip("Enable shoulder correction.")]
        public bool enableShoulderFix = true;

        [Tooltip("Shared shoulder position offset. Right side is mirrored internally.")]
        public Vector3 shoulderPositionOffset = Vector3.zero;

        [Tooltip("Shared shoulder rotation offset. Right side is mirrored internally.")]
        public Vector3 shoulderEulerOffset = new Vector3(0f, 0f, -10f);

        [Tooltip("Upper arm twist axis. Default is X.")]
        public TwistAxis upperArmTwistAxis = TwistAxis.X;

        [Tooltip("How strongly the twist axis follows the original upper arm.")]
        [Range(0f, 1f)]
        public float upperArmTwistWeight = 1f;

        [Header("Wrist")]
        [Tooltip("Enable wrist correction.")]
        public bool enableWristFix = true;

        [Tooltip("Shared forearm position offset.")]
        public Vector3 wristPositionOffset = Vector3.zero;

        [Tooltip("Forearm thickness scale.")]
        public float wristThicknessScale = 1f;

        [Tooltip("Forearm width scale.")]
        public float wristWidthScale = 0.92f;

        [Tooltip("Wrist twist axis. Default is X.")]
        public TwistAxis wristTwistAxis = TwistAxis.X;

        [Tooltip("How strongly wrist twist follows the hand.")]
        [Range(0f, 1f)]
        public float wristTwistWeight = 1f;

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
