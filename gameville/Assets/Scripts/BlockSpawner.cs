using UnityEngine;

namespace Gameville
{
    public class BlockSpawner : MonoBehaviour
    {
        public LuaManager luaManager;

        private void Start()
        {
            if (luaManager != null)
            {
                luaManager.CallLuaFunction("generate_world");
            }
        }
    }
}
