using Basis.Scripts.Addressable_Driver;
using Basis.Scripts.Addressable_Driver.Enums;
using Basis.Scripts.BasisSdk.Players;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Basis.Scripts.UI.UI_Panels
{
    public class BasisUIAddAvatar : BasisUIBase
    {
        public const string MenuString = "BasisUIAddAvatar";

        [SerializeField] public TMP_InputField ConnectorField;
        [SerializeField] public TMP_InputField PasswordField;
        [SerializeField] public TextMeshProUGUI ErrorMessage;
        [SerializeField] public TextMeshProUGUI StepMessage;
        [SerializeField] public Button StageOneButton;
        [SerializeField] public Button StageTwoButton;
        [SerializeField] public BasisProgressReport Report = new BasisProgressReport();
        public CancellationToken CancellationToken = new CancellationToken();
        public GameObject StageOne;
        public GameObject StageTwo;

        public void Start()
        {
            try
            {
                BasisDataStoreAvatarKeys.DisplayKeys();
                StageOneButton.onClick.AddListener(TaskOne);
                StageTwoButton.onClick.AddListener(TaskTwo);
                StageOne.SetActive(true);
                StageTwo.SetActive(false);
                StepMessage.text = "Step 1: Enter the URL";
                ErrorMessage.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                DisplayError($"Error during initialization: {ex.Message}");
            }
        }
        public static void OpenAddAvatarUI()
        {
            AddressableGenericResource resource = new AddressableGenericResource(MenuString, AddressableExpectedResult.SingleItem);
            OpenMenuNow(resource);
        }
        public override void InitalizeEvent()
        {
            try
            {
                BasisCursorManagement.UnlockCursor(MenuString);
                BasisUINeedsVisibleTrackers.Instance.Add(this);
            }
            catch (Exception ex)
            {
                DisplayError($"Error initializing event: {ex.Message}");
            }
        }

        public void DisplayError(string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                ErrorMessage.text = error;
                ErrorMessage.gameObject.SetActive(true);
                BasisDebug.LogError(error);
            }
            else
            {
                ErrorMessage.gameObject.SetActive(false);
            }
        }

        public void ClearDisplay()
        {
            ErrorMessage.text = string.Empty;
            ErrorMessage.gameObject.SetActive(false);
        }

        private void TaskOne()
        {
            try
            {
                ClearDisplay();

                if (string.IsNullOrEmpty(ConnectorField.text))
                {
                    DisplayError("URL Field Is Empty");
                    return;
                }

                // Trim leading and trailing whitespace from the URL
                string processedUrl = ConnectorField.text.Trim();

                ValidateURL(processedUrl, out string errorReason, out bool valid);
                if (!valid)
                {
                    DisplayError($"Invalid URL format: {errorReason}");
                    return;
                }

                StepMessage.text = "Step 2: Enter Password and Load Avatar";
                StageOne.SetActive(false);
                StageTwo.SetActive(true);
                ConnectorField.text = processedUrl;
            }
            catch (Exception ex)
            {
                DisplayError($"Unexpected error: {ex.Message}");
            }
        }

        private void ValidateURL(string url, out string errorReason, out bool valid)
        {
            valid = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult);
            if (!valid)
            {
                errorReason = "URL is not a valid absolute URI.";
                return;
            }

            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            {
                valid = false;
                errorReason = "URL must start with http:// or https://";
                return;
            }

            errorReason = string.Empty;
            valid = true;
        }

        private async void TaskTwo()
        {
            try
            {
                string url = ConnectorField.text;
                string password = PasswordField.text.Trim();
                if (string.IsNullOrEmpty(password))
                {
                    DisplayError("Password Field Is Empty.");
                    return;
                }

                List<BasisDataStoreAvatarKeys.AvatarKey> activeKeys = BasisDataStoreAvatarKeys.DisplayKeys();
                bool keyExists = activeKeys.Exists(key => key.Url == url && key.Pass == password);

                if (keyExists)
                {
                    DisplayError("The avatar key with the same URL and Password already exists. No duplicate will be added.");
                    return;
                }

                BasisLoadableBundle loadableBundle = new BasisLoadableBundle
                {
                    UnlockPassword = password,
                    BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle { CombinedURL = url },
                    BasisBundleConnector = new BasisBundleConnector(),
                    BasisLocalEncryptedBundle = new BasisStoredEncryptedBundle()
                };

                await BasisLocalPlayer.Instance.CreateAvatar(0, loadableBundle);
                var avatarKey = new BasisDataStoreAvatarKeys.AvatarKey { Url = url, Pass = password };
                await BasisDataStoreAvatarKeys.AddNewKey(avatarKey);

                CloseThisMenu();
            }
            catch (Exception ex)
            {
                DisplayError($"Error during avatar creation: {ex.Message}");
            }
        }

        public override void DestroyEvent()
        {
            try
            {
                BasisCursorManagement.LockCursor(MenuString);
                BasisUINeedsVisibleTrackers.Instance.Remove(this);
            }
            catch (Exception ex)
            {
                DisplayError($"Error during destruction: {ex.Message}");
            }
        }
    }
}
