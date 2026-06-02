using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Gameville
{
    [System.Serializable]
    public class SkinDefinition
    {
        public string name;
        public string textureFile;
        public int type; // 0 = color, 1 = texture
        public float r, g, b, a = 1f;

        public Color GetColor()
        {
            return new Color(r, g, b, a);
        }
    }

    public class SkinManager : MonoBehaviour
    {
        [System.Serializable]
        public class SkinSet
        {
            public List<SkinDefinition> skins = new List<SkinDefinition>();
        }

        public List<SkinDefinition> availableSkins = new List<SkinDefinition>();
        private Dictionary<int, Texture2D> loadedTextures = new Dictionary<int, Texture2D>();
        private Renderer skinRenderer;
        private int currentSkinIndex;
        private static bool skinsInitialized = false;

        private void Awake()
        {
            CreateSkinMesh();
            if (!skinsInitialized)
            {
                InitializeSkins();
            }
            LoadSkinsFromFile();
            if (availableSkins.Count == 0)
            {
                CreateDefaultSkins();
            }
            ApplySkin(0);
        }

        public static void InitializeSkins()
        {
            if (skinsInitialized)
                return;

            string skinsPath = Path.Combine(Application.streamingAssetsPath, "Skins/skins.json");
            if (File.Exists(skinsPath))
            {
                try
                {
                    string json = File.ReadAllText(skinsPath);
                    SkinSet skinSet = JsonUtility.FromJson<SkinSet>(json);
                    Debug.Log($"[SkinManager] Initialized {skinSet.skins.Count} skins from file.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SkinManager] Failed to initialize skins: {ex.Message}");
                }
            }
            skinsInitialized = true;
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

        private void LoadSkinsFromFile()
        {
            string skinsPath = Path.Combine(Application.streamingAssetsPath, "Skins/skins.json");
            if (File.Exists(skinsPath))
            {
                try
                {
                    string json = File.ReadAllText(skinsPath);
                    SkinSet skinSet = JsonUtility.FromJson<SkinSet>(json);
                    availableSkins = skinSet.skins;
                    Debug.Log($"Loaded {availableSkins.Count} skins from file.");

                    for (int i = 0; i < availableSkins.Count; i++)
                    {
                        if (availableSkins[i].type == 1 && !string.IsNullOrEmpty(availableSkins[i].textureFile))
                        {
                            LoadTexture(i);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load skins: {ex.Message}");
                }
            }
        }

        private void LoadTexture(int skinIndex)
        {
            if (skinIndex < 0 || skinIndex >= availableSkins.Count)
                return;

            SkinDefinition skin = availableSkins[skinIndex];
            if (string.IsNullOrEmpty(skin.textureFile))
                return;

            string texturePath = Path.Combine(Application.streamingAssetsPath, "Skins", skin.textureFile);
            if (File.Exists(texturePath))
            {
                byte[] textureData = File.ReadAllBytes(texturePath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(textureData))
                {
                    loadedTextures[skinIndex] = tex;
                    Debug.Log($"Loaded texture for skin '{skin.name}'");
                }
                else
                {
                    Debug.LogWarning($"Failed to load texture image: {texturePath}");
                }
            }
            else
            {
                Debug.LogWarning($"Texture file not found: {texturePath}");
            }
        }

        private void CreateDefaultSkins()
        {
            availableSkins.Add(new SkinDefinition { name = "White", type = 0, r = 1f, g = 1f, b = 1f });
            availableSkins.Add(new SkinDefinition { name = "Red", type = 0, r = 1f, g = 0f, b = 0f });
            availableSkins.Add(new SkinDefinition { name = "Blue", type = 0, r = 0f, g = 0f, b = 1f });
            availableSkins.Add(new SkinDefinition { name = "Green", type = 0, r = 0f, g = 1f, b = 0f });
            availableSkins.Add(new SkinDefinition { name = "Orange", type = 0, r = 1f, g = 0.6f, b = 0.2f });
        }

        public int NextSkin()
        {
            if (availableSkins.Count == 0)
                return currentSkinIndex;

            currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Count;
            ApplySkin(currentSkinIndex);
            return currentSkinIndex;
        }

        public void ApplySkin(int skinIndex)
        {
            if (availableSkins.Count == 0)
                return;

            currentSkinIndex = Mathf.Clamp(skinIndex, 0, availableSkins.Count - 1);
            SkinDefinition skin = availableSkins[currentSkinIndex];

            if (skinRenderer != null)
            {
                Material mat = new Material(skinRenderer.material);

                if (skin.type == 1 && loadedTextures.ContainsKey(currentSkinIndex))
                {
                    mat.mainTexture = loadedTextures[currentSkinIndex];
                }
                else
                {
                    mat.color = skin.GetColor();
                }

                skinRenderer.material = mat;
            }
        }

        public int GetCurrentSkinIndex()
        {
            return currentSkinIndex;
        }

        public string GetCurrentSkinName()
        {
            if (currentSkinIndex >= 0 && currentSkinIndex < availableSkins.Count)
                return availableSkins[currentSkinIndex].name;
            return "Unknown";
        }

        public int GetSkinCount()
        {
            return availableSkins.Count;
        }
    }
}
