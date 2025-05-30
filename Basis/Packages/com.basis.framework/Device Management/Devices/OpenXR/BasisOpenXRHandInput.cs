using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.XR;
public class BasisOpenXRHandInput : BasisInput
{
    public BasisFingerPose FingerCurls;
    public InputActionProperty DeviceActionPosition;
    public InputActionProperty DeviceActionRotation;
    public InputActionProperty PalmPosition;
    public InputActionProperty PalmRotation;
    public InputActionProperty Trigger;
    public InputActionProperty Grip;
    public InputActionProperty PrimaryButton;
    public InputActionProperty SecondaryButton;
    public InputActionProperty MenuButton;
    public InputActionProperty Primary2DAxis;
    public InputActionProperty Secondary2DAxis;
    public void Initialize(string UniqueID, string UnUniqueID, string subSystems, bool AssignTrackedRole, BasisBoneTrackedRole basisBoneTrackedRole)
    {
        InitalizeTracking(UniqueID, UnUniqueID, subSystems, AssignTrackedRole, basisBoneTrackedRole);
        string devicePath = basisBoneTrackedRole == BasisBoneTrackedRole.LeftHand ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
        string DevicePalmPath = basisBoneTrackedRole == BasisBoneTrackedRole.LeftHand ? "<PalmPose>{LeftHand}" : "<PalmPose>{RightHand}";
        SetupInputActions(devicePath);

        switch (basisBoneTrackedRole)
        {
            case BasisBoneTrackedRole.LeftHand:
                AvatarPositionOffset = new float3(0f, 0.05f, 0.05f);
                AvatarRotationOffset = new float3(-90f, 45f, 0f);
                break;
            case BasisBoneTrackedRole.RightHand:
                AvatarPositionOffset = new float3(0f, 0.05f, 0.05f);
                AvatarRotationOffset = new float3(-75f, -45f, 0f);
                break;
        }

        PalmPosition = new InputActionProperty(new InputAction($"{DevicePalmPath}/devicePosition", InputActionType.Value, $"{DevicePalmPath}/devicePosition", expectedControlType: "Vector3"));
        PalmRotation = new InputActionProperty(new InputAction($"{DevicePalmPath}/deviceRotation", InputActionType.Value, $"{DevicePalmPath}/deviceRotation", expectedControlType: "Quaternion"));

        PalmPosition.action.Enable();
        PalmRotation.action.Enable();

        DeviceActionPosition = new InputActionProperty(new InputAction($"{devicePath}/devicePosition", InputActionType.Value, $"{devicePath}/devicePosition", expectedControlType: "Vector3"));
        DeviceActionRotation = new InputActionProperty(new InputAction($"{devicePath}/deviceRotation", InputActionType.Value, $"{devicePath}/deviceRotation", expectedControlType: "Quaternion"));

        DeviceActionPosition.action.Enable();
        DeviceActionRotation.action.Enable();
    }
    private void SetupInputActions(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
        {
            Debug.LogError("Device path is null or empty.");
            return;
        }
        Trigger = new InputActionProperty(new InputAction(devicePath + "/trigger", InputActionType.Value, devicePath + "/trigger", expectedControlType: "Float"));
        Grip = new InputActionProperty(new InputAction(devicePath + "/grip", InputActionType.Value, devicePath + "/grip", expectedControlType: "Float"));
        PrimaryButton = new InputActionProperty(new InputAction(devicePath + "/primaryButton", InputActionType.Button, devicePath + "/primaryButton", expectedControlType: "Button"));
        SecondaryButton = new InputActionProperty(new InputAction(devicePath + "/secondaryButton", InputActionType.Button, devicePath + "/secondaryButton", expectedControlType: "Button"));
        MenuButton = new InputActionProperty(new InputAction(devicePath + "/menuButton", InputActionType.Button, devicePath + "/menuButton", expectedControlType: "Button"));
        Primary2DAxis = new InputActionProperty(new InputAction(devicePath + "/primary2DAxis", InputActionType.Value, devicePath + "/primary2DAxis", expectedControlType: "Vector2"));
        Secondary2DAxis = new InputActionProperty(new InputAction(devicePath + "/secondary2DAxis", InputActionType.Value, devicePath + "/secondary2DAxis", expectedControlType: "Vector2"));
        EnableInputActions();
    }
    private void EnableInputActions()
    {
        EnableInputAction(PalmPosition);
        EnableInputAction(PalmRotation);
        EnableInputAction(Trigger);
        EnableInputAction(Grip);
        EnableInputAction(PrimaryButton);
        EnableInputAction(SecondaryButton);
        EnableInputAction(MenuButton);
        EnableInputAction(Primary2DAxis);
        EnableInputAction(Secondary2DAxis);
    }
    private void DisableInputActions()
    {
        DisableInputAction(PalmPosition);
        DisableInputAction(PalmRotation);
        DisableInputAction(Trigger);
        DisableInputAction(Grip);
        DisableInputAction(PrimaryButton);
        DisableInputAction(SecondaryButton);
        DisableInputAction(MenuButton);
        DisableInputAction(Primary2DAxis);
        DisableInputAction(Secondary2DAxis);
    }
    private void EnableInputAction(InputActionProperty actionProperty) => actionProperty.action?.Enable();
    private void DisableInputAction(InputActionProperty actionProperty) => actionProperty.action?.Disable();

    public new void OnDestroy()
    {
        DisableInputActions();
        base.OnDestroy();
    }
    public override void DoPollData()
    {
        LocalRawPosition = DeviceActionPosition.action.ReadValue<Vector3>();
        LocalRawRotation = DeviceActionRotation.action.ReadValue<Quaternion>();

        TransformFinalPosition = LocalRawPosition * BasisLocalPlayer.Instance.CurrentHeight.SelectedAvatarToAvatarDefaultScale;
        TransformFinalRotation = LocalRawRotation;

        InputState.Primary2DAxis = Primary2DAxis.action?.ReadValue<Vector2>() ?? Vector2.zero;
        InputState.Secondary2DAxis = Secondary2DAxis.action?.ReadValue<Vector2>() ?? Vector2.zero;

        InputState.GripButton = Grip.action?.ReadValue<float>() > 0.5f;
        InputState.SecondaryTrigger = Grip.action?.ReadValue<float>() ?? 0f;
        InputState.SystemOrMenuButton = MenuButton.action?.ReadValue<float>() > 0.5f;
        InputState.PrimaryButtonGetState = PrimaryButton.action?.ReadValue<float>() > 0.5f;
        InputState.SecondaryButtonGetState = SecondaryButton.action?.ReadValue<float>() > 0.5f;

        InputState.Trigger = Trigger.action?.ReadValue<float>() ?? 0f;
        if (hasRoleAssigned)
        {
            if (Control.HasTracked != BasisHasTracked.HasNoTracker)
            {
                // Apply position offset using math.mul for quaternion-vector multiplication
                Control.IncomingData.position = TransformFinalPosition - math.mul(TransformFinalRotation, AvatarPositionOffset * BasisLocalPlayer.Instance.CurrentHeight.SelectedAvatarToAvatarDefaultScale);

                // Apply rotation offset using math.mul for quaternion multiplication
                Control.IncomingData.rotation = math.mul(TransformFinalRotation, Quaternion.Euler(AvatarRotationOffset));
            }
        }
        CalculateFingerCurls();
        UpdatePlayerControl();
    }

    private void CalculateFingerCurls()
    {
        FingerCurls.ThumbPercentage = new Vector2(InputState.GripButton ? -1f : 0.7f, 0);
        FingerCurls.IndexPercentage = new Vector2(BasisBaseMuscleDriver.MapValue(InputState.Trigger, 0, 1, -1f, 0.7f), 0);
        FingerCurls.MiddlePercentage = new Vector2(InputState.PrimaryButtonGetState ? -1f : 0.7f, 0);
        FingerCurls.RingPercentage = new Vector2(InputState.SecondaryButtonGetState ? -1f : 0.7f, 0);
        FingerCurls.LittlePercentage = new Vector2(InputState.SystemOrMenuButton ? 1 - 1f : 0.7f, 0);
    }
    public override void ShowTrackedVisual()
    {
        if (BasisVisualTracker == null && LoadedDeviceRequest == null)
        {
            BasisDeviceMatchSettings Match = BasisDeviceManagement.Instance.BasisDeviceNameMatcher.GetAssociatedDeviceMatchableNames(CommonDeviceIdentifier);
            if (Match.CanDisplayPhysicalTracker)
            {
                InputDeviceCharacteristics Hand = InputDeviceCharacteristics.None;
                if (TryGetRole(out BasisBoneTrackedRole HandRole))
                {
                    switch (HandRole)
                    {
                        case BasisBoneTrackedRole.LeftHand:
                            Hand = InputDeviceCharacteristics.Left;
                            break;
                        case BasisBoneTrackedRole.RightHand:
                            Hand = InputDeviceCharacteristics.Right;
                            break;
                        default:
                            useFallback();
                            return;
                    }
                    InputDeviceCharacteristics input = Hand | InputDeviceCharacteristics.Controller;
                    List<UnityEngine.XR.InputDevice> inputDevices = new List<UnityEngine.XR.InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(input, inputDevices);
                    if (inputDevices.Count != 0)
                    {
                        UnityEngine.XR.InputDevice Device = inputDevices[0];
                        string LoadRequest;
                        string HandString = Hand.ToString().ToLower();
                        switch (Device.name)
                        {
                            case "Oculus Touch Controller OpenXR":
                                LoadRequest = $"oculus_quest_plus_controller_{HandString}";
                                break;
                            case "Valve Index Controller OpenXR":
                                LoadRequest = $"valve_controller_knu_{HandString}";
                                break;
                            case "Meta Quest Touch Pro Controller OpenXR":
                                LoadRequest = $"oculus_quest_pro_controller_{HandString}";
                                break;
                            case "Meta Quest Touch Plus Controller OpenXR":
                                LoadRequest = $"oculus_quest_plus_controller_{HandString}";
                                break;
                            default:
                                LoadRequest = $"valve_controller_knu_{HandString}";
                                break;
                        }

                        BasisDebug.Log("name was found to be " + LoadRequest + " for device " + Device.name + " picked from " + inputDevices.Count, BasisDebug.LogTag.Device);

                        var op = Addressables.LoadAssetAsync<GameObject>(LoadRequest);
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
                        useFallback();
                    }
                }
                else
                {
                    useFallback();
                }
            }
            else
            {
                if (UseFallbackModel())
                {
                    useFallback();
                }
            }
        }
    }
    public void useFallback()
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
