using UnityEngine;

namespace Gameville
{
    [DisallowMultipleComponent]
    public class PlayerSkinController : MonoBehaviour
    {
        public Color[] skinColors = new Color[]
        {
            Color.white,
            Color.red,
            Color.blue,
            Color.green,
            new Color(1f, 0.6f, 0.2f)
        };

        public int currentSkinIndex;

        private Renderer skinRenderer;

        private void Awake()
        {
            CreateSkinMesh();
            ApplySkin(currentSkinIndex);
        }

        private void CreateSkinMesh()
        {
            Transform existing = transform.Find("SkinMesh");
            if (existing != null)
            {
                skinRenderer = existing.GetComponent<Renderer>();
                return;
            }

            GameObject skin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            skin.name = "SkinMesh";
            skin.transform.SetParent(transform, false);
            skin.transform.localPosition = Vector3.zero;
            skin.transform.localRotation = Quaternion.identity;
            skin.transform.localScale = new Vector3(0.6f, 1.8f, 0.4f);

            skinRenderer = skin.GetComponent<Renderer>();
            Destroy(skin.GetComponent<Collider>());
        }

        public int NextSkin()
        {
            if (skinColors == null || skinColors.Length == 0)
                return currentSkinIndex;

            currentSkinIndex = (currentSkinIndex + 1) % skinColors.Length;
            ApplySkin(currentSkinIndex);
            return currentSkinIndex;
        }

        public void ApplySkin(int skinIndex)
        {
            if (skinColors == null || skinColors.Length == 0)
                return;

            currentSkinIndex = Mathf.Clamp(skinIndex, 0, skinColors.Length - 1);
            if (skinRenderer != null)
            {
                skinRenderer.material.color = skinColors[currentSkinIndex];
            }
        }
    }
}
