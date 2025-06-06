using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management.Devices.OpenVR.Structs;
using Basis.Scripts.TransformBinders.BoneControl;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Valve.VR;

namespace Basis.Scripts.Device_Management.Devices.OpenVR
{
    ///only used for trackers!
    public class BasisOpenVRInput : BasisInput
    {
        [SerializeField]
        public OpenVRDevice Device;
        public TrackedDevicePose_t devicePose = new TrackedDevicePose_t();
        public TrackedDevicePose_t deviceGamePose = new TrackedDevicePose_t();
        public SteamVR_Utils.RigidTransform deviceTransform;
        public EVRCompositorError result;
        public bool HasInputSource = false;
        public SteamVR_Input_Sources inputSource;
        public void Initialize(OpenVRDevice device, string UniqueID, string UnUniqueID, string subSystems, bool AssignTrackedRole, BasisBoneTrackedRole basisBoneTrackedRole)
        {
            Device = device;
            InitalizeTracking(UniqueID, UnUniqueID, subSystems, AssignTrackedRole, basisBoneTrackedRole);
        }
        public override void DoPollData()
        {
            if (SteamVR.active)
            {
                result = SteamVR.instance.compositor.GetLastPoseForTrackedDeviceIndex(Device.deviceIndex, ref devicePose, ref deviceGamePose);
                if (result == EVRCompositorError.None)
                {
                    if (deviceGamePose.bPoseIsValid)
                    {
                        deviceTransform = new SteamVR_Utils.RigidTransform(deviceGamePose.mDeviceToAbsoluteTracking);
                        LocalRawPosition = deviceTransform.pos;
                        LocalRawRotation = deviceTransform.rot;

                        TransformFinalPosition = LocalRawPosition * BasisLocalPlayer.Instance.CurrentHeight.SelectedAvatarToAvatarDefaultScale;
                        TransformFinalRotation = LocalRawRotation;
                        if (hasRoleAssigned)
                        {
                            if (Control.HasTracked != BasisHasTracked.HasNoTracker)
                            {
                                // Apply the position offset using math.mul for quaternion-vector multiplication
                                Control.IncomingData.position = TransformFinalPosition - math.mul(TransformFinalRotation, AvatarPositionOffset * BasisLocalPlayer.Instance.CurrentHeight.SelectedAvatarToAvatarDefaultScale);

                                // Apply the rotation offset using math.mul for quaternion multiplication
                                Control.IncomingData.rotation = math.mul(TransformFinalRotation, Quaternion.Euler(AvatarRotationOffset));
                            }
                        }
                        if (HasInputSource)
                        {
                            InputState.Primary2DAxis = SteamVR_Actions._default.Joystick.GetAxis(inputSource);
                            InputState.PrimaryButtonGetState = SteamVR_Actions._default.A_Button.GetState(inputSource);
                            InputState.SecondaryButtonGetState = SteamVR_Actions._default.B_Button.GetState(inputSource);
                            InputState.Trigger = SteamVR_Actions._default.Trigger.GetAxis(inputSource);
                        }
                        UpdatePlayerControl();
                    }
                }
                else
                {
                    BasisDebug.LogError("Error getting device pose: " + result);
                }
            }
        }
        public override void ShowTrackedVisual()
        {
            if (BasisVisualTracker == null && LoadedDeviceRequest == null)
            {
                BasisDeviceMatchSettings Match = BasisDeviceManagement.Instance.BasisDeviceNameMatcher.GetAssociatedDeviceMatchableNames(CommonDeviceIdentifier);
                if (Match.CanDisplayPhysicalTracker)
                {
                    var op = Addressables.LoadAssetAsync<GameObject>(Match.DeviceID);
                    GameObject go = op.WaitForCompletion();
                    GameObject gameObject = Object.Instantiate(go);
                    gameObject.name = CommonDeviceIdentifier;
                    gameObject.transform.parent = this.transform;
                    if (gameObject.TryGetComponent(out BasisVisualTracker))
                    {
                        BasisVisualTracker.Initialization(this);
                    }
                }
                else
                {
                    if (UseFallbackModel())
                    {
                        UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> op = Addressables.LoadAssetAsync<GameObject>(FallbackDeviceID);
                        GameObject go = op.WaitForCompletion();
                        GameObject gameObject = Object.Instantiate(go);
                        gameObject.name = CommonDeviceIdentifier;
                        gameObject.transform.parent = this.transform;
                        if (gameObject.TryGetComponent(out BasisVisualTracker))
                        {
                            BasisVisualTracker.Initialization(this);
                        }
                    }
                }
            }
        }
    }
}
