using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GameAssets
{
    public class PlayerFlashlightToggle : MonoBehaviour
    {
        [Header("Flashlight")]
        [SerializeField] private Light spotLight;
        [SerializeField] private GameObject flashlightRoot;
        [SerializeField] private bool showFlashlightModel;
        [SerializeField] private bool startEnabled;
        [SerializeField] private bool attachLightToCamera = true;
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 cameraLocalEulerAngles = Vector3.zero;

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.R;

        private bool isEnabled;
        private Transform followTarget;
        private Quaternion cameraLocalRotation = Quaternion.identity;

        private void OnEnable()
        {
            SetupCameraLightFollow();
            SetFlashlight(startEnabled);
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            KeyControl key = Keyboard.current[toggleKey];
            if (key != null && key.wasPressedThisFrame)
            {
                SetFlashlight(!isEnabled);
            }
        }

        private void LateUpdate()
        {
            if (attachLightToCamera == false || spotLight == null || followTarget == null)
            {
                return;
            }

            Transform lightTransform = spotLight.transform;
            lightTransform.localPosition = cameraLocalPosition;
            lightTransform.localRotation = cameraLocalRotation;
        }

        public void SetFlashlight(bool enabled)
        {
            isEnabled = enabled;

            if (spotLight != null)
            {
                spotLight.enabled = enabled;
                spotLight.gameObject.SetActive(enabled);
            }

            if (flashlightRoot != null)
            {
                SetRenderersVisible(flashlightRoot, enabled && showFlashlightModel);
            }
        }

        private void SetupCameraLightFollow()
        {
            Camera mainCamera = Camera.main;
            followTarget = mainCamera != null ? mainCamera.transform : null;

            if (spotLight == null || IsOldFlashlightLight(spotLight))
            {
                spotLight = FindCameraSpotLight(followTarget);
            }

            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>(true);
            }

            if (spotLight == null)
            {
                return;
            }

            if (attachLightToCamera && followTarget != null)
            {
                spotLight.transform.SetParent(followTarget, false);
            }

            cameraLocalRotation = Quaternion.Euler(cameraLocalEulerAngles);
            ApplyCameraOffset();
        }

        private void ApplyCameraOffset()
        {
            if (spotLight == null || followTarget == null)
            {
                return;
            }

            Transform lightTransform = spotLight.transform;
            lightTransform.localPosition = cameraLocalPosition;
            lightTransform.localRotation = cameraLocalRotation;
        }

        private static Light FindCameraSpotLight(Transform cameraTransform)
        {
            if (cameraTransform == null)
            {
                return null;
            }

            Light[] cameraLights = cameraTransform.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < cameraLights.Length; i++)
            {
                if (cameraLights[i] != null && cameraLights[i].type == LightType.Spot)
                {
                    return cameraLights[i];
                }
            }

            return null;
        }

        private static bool IsOldFlashlightLight(Light light)
        {
            if (light == null)
            {
                return false;
            }

            Transform current = light.transform;
            while (current != null)
            {
                if (current.name.Contains("FlashLight") || current.name.Contains("Flash Light"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void SetRenderersVisible(GameObject root, bool visible)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = visible;
            }
        }
    }
}
