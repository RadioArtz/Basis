using Basis.Scripts.Addressable_Driver;
using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.Drivers;
using System;
using System.Threading;
using UnityEngine;
namespace Basis.Scripts.BasisSdk.Players
{
    public abstract partial class BasisPlayer : MonoBehaviour
    {
        public bool IsLocal { get; set; }
        public string DisplayName;
        public string UUID;
        public BasisAvatar Avatar;
        public AddressableGenericResource AvatarAddressableGenericResource;
        public BasisLoadableBundle AvatarMetaData;
        public bool HasAvatarDriver;
        public event Action OnAvatarSwitched;
        public event Action OnAvatarSwitchedFallBack;
        public BasisProgressReport ProgressReportAvatarLoad = new BasisProgressReport();
        public const byte LoadModeNetworkDownloadable = 0;
        public const byte LoadModeLocal = 1;
        public const byte LoadModeError = 2;
        public bool FaceisVisible;
        public BasisMeshRendererCheck FaceRenderer;
        public CancellationToken CurrentAvatarsCancellationToken;
        public byte AvatarLoadMode;//0 downloading 1 local

        public BasisProgressReport AvatarProgress = new BasisProgressReport();
        public CancellationToken CancellationToken;
        public BasisAvatarStrainJiggleDriver BasisAvatarStrainJiggleDriver;
        public Action<bool> AudioReceived;
        public void InitalizeIKCalibration(BasisAvatarDriver BasisAvatarDriver)
        {
            if (BasisAvatarDriver != null)
            {
                HasAvatarDriver = true;
            }
            else
            {
                Debug.LogError("Mising CharacterIKCalibration");
                HasAvatarDriver = false;
            }
            BasisAvatarStrainJiggleDriver = BasisHelpers.GetOrAddComponent<BasisAvatarStrainJiggleDriver>(this.gameObject);
            BasisAvatarStrainJiggleDriver.OnCalibration();
            if (BasisAvatarStrainJiggleDriver != null)
            {
                BasisAvatarStrainJiggleDriver.OnCalibration();
            }
        }
        private void UpdateFaceVisibility(bool State)
        {
            FaceisVisible = State;
        }
        public void AvatarSwitchedFallBack()
        {
            UpdateFaceRenderer();//dont process face for fallback
            OnAvatarSwitchedFallBack?.Invoke();
        }
        public void AvatarSwitched()
        {
            UpdateFaceRenderer();
            OnAvatarSwitched?.Invoke();
        }
        public void UpdateFaceRenderer()
        {
            FaceisVisible = false;
            if (Avatar == null)
            {
                Debug.LogError("Missing Avatar");
            }
            if (Avatar.FaceVisemeMesh == null)
            {
                Debug.Log("Missing Face for " + DisplayName);
            }
            UpdateFaceVisibility(Avatar.FaceVisemeMesh.isVisible);
            if (FaceRenderer != null)
            {
                GameObject.Destroy(FaceRenderer);
            }
            FaceRenderer = BasisHelpers.GetOrAddComponent<BasisMeshRendererCheck>(Avatar.FaceVisemeMesh.gameObject);
            FaceRenderer.Check += UpdateFaceVisibility;
        }
    }
}