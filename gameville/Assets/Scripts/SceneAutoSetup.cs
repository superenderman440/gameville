using UnityEngine;

namespace Gameville
{
    [DefaultExecutionOrder(-100)]
    public class SceneAutoSetup : MonoBehaviour
    {
        public MultiplayerManager multiplayerManager;
        public LuaManager luaManager;
        public PlayerController playerController;
        public PlayerSkinController playerSkinController;
        public Transform worldRoot;

        private void Awake()
        {
            if (playerController == null)
            {
                GameObject playerObject = GameObject.Find("Player");
                if (playerObject != null)
                    playerController = playerObject.GetComponent<PlayerController>();
            }

            if (playerSkinController == null && playerController != null)
            {
                playerSkinController = playerController.GetComponent<PlayerSkinController>();
                if (playerSkinController == null)
                {
                    playerSkinController = playerController.gameObject.AddComponent<PlayerSkinController>();
                }
            }

            if (luaManager == null)
            {
                GameObject luaManagerObject = GameObject.Find("LuaGameManager");
                if (luaManagerObject != null)
                    luaManager = luaManagerObject.GetComponent<LuaManager>();
            }

            if (multiplayerManager == null)
            {
                GameObject managerObject = GameObject.Find("MultiplayerManager");
                if (managerObject != null)
                    multiplayerManager = managerObject.GetComponent<MultiplayerManager>();
            }

            if (worldRoot == null)
            {
                GameObject rootObject = GameObject.Find("WorldRoot");
                if (rootObject == null)
                {
                    rootObject = new GameObject("WorldRoot");
                }
                worldRoot = rootObject.transform;
            }

            if (playerController != null)
            {
                if (playerController.cameraTransform == null)
                {
                    GameObject cameraObject = GameObject.Find("Main Camera");
                    if (cameraObject != null)
                        playerController.cameraTransform = cameraObject.transform;
                }

                if (playerController.worldRoot == null)
                {
                    playerController.worldRoot = worldRoot;
                }
            }

            if (luaManager != null && luaManager.worldRoot == null)
            {
                luaManager.worldRoot = worldRoot;
            }

            if (multiplayerManager != null)
            {
                if (multiplayerManager.localPlayer == null && playerController != null)
                    multiplayerManager.localPlayer = playerController;

                if (multiplayerManager.playerPrefab == null && playerController != null)
                    multiplayerManager.playerPrefab = playerController.gameObject;

                if (multiplayerManager.worldRoot == null)
                    multiplayerManager.worldRoot = worldRoot;

                if (playerController != null)
                    playerController.SetMultiplayerManager(multiplayerManager);
            }

            if (luaManager != null && luaManager.blockPrefab == null)
            {
                GameObject blockObject = GameObject.Find("Block");
                if (blockObject != null)
                    luaManager.blockPrefab = blockObject;
            }

            if (playerController != null && playerController.blockPrefab == null)
            {
                GameObject blockObject = GameObject.Find("Block");
                if (blockObject != null)
                    playerController.blockPrefab = blockObject;
            }
        }
    }
}
