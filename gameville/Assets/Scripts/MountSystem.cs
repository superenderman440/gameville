using UnityEngine;
using System.Collections.Generic;

namespace Gameville
{
    public class MountSystem
    {
        public static bool IsMountable(int mobId)
        {
            // Shrieking Fence (ID 9) is mountable
            return mobId == 9;
        }

        public static float GetMountSpeedBoost(int mobId)
        {
            if (mobId == 9) // Shrieking Fence - Warden Nautilus
                return 1.5f; // 50% speed boost
            return 1f;
        }
    }

    public class MobMount : MonoBehaviour
    {
        public int mobId;
        public CharacterController playerController;
        public bool playerMounted = false;

        private Mob mobComponent;

        private void Start()
        {
            mobComponent = GetComponent<Mob>();
            if (mobComponent != null && mobComponent.definition != null)
            {
                mobId = mobComponent.definition.id;
            }
        }

        public bool TryMount(CharacterController controller)
        {
            if (!MountSystem.IsMountable(mobId) || playerMounted)
                return false;

            playerController = controller;
            playerMounted = true;
            Debug.Log($"Mounted {mobComponent.definition.name}!");
            return true;
        }

        public void Dismount()
        {
            playerMounted = false;
            playerController = null;
            Debug.Log($"Dismounted!");
        }
    }
}
