using System.Collections;
using UnityEngine;

namespace Gameville
{
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance;
        private Transform camTransform;
        private Vector3 originalPos;
        private Quaternion originalRot;

        [Header("Tilt")]
        public bool enableTilt = true;
        public float tiltFactor = 5f; // degrees per intensity unit

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

            Camera cam = Camera.main;
            if (cam != null)
            {
                camTransform = cam.transform;
                originalPos = camTransform.localPosition;
                originalRot = camTransform.localRotation;
            }
        }

        public void Shake(float intensity, float duration)
        {
            if (camTransform == null)
                return;
            StopAllCoroutines();
            StartCoroutine(DoShake(intensity, duration));
        }

        private IEnumerator DoShake(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = (Random.value * 2f - 1f) * intensity;
                float y = (Random.value * 2f - 1f) * intensity;
                camTransform.localPosition = originalPos + new Vector3(x, y, 0f);
                if (enableTilt)
                {
                    float tiltX = (Random.value * 2f - 1f) * intensity * tiltFactor;
                    float tiltY = (Random.value * 2f - 1f) * intensity * tiltFactor;
                    camTransform.localRotation = originalRot * Quaternion.Euler(tiltX, tiltY, 0f);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            camTransform.localPosition = originalPos;
            if (enableTilt)
                camTransform.localRotation = originalRot;
        }
    }
}
