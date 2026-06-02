using System;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Gameville
{
    public class LuaManager : MonoBehaviour
    {
        [Header("Lua")]
        public TextAsset luaScript;

        [Header("World")]
        public GameObject blockPrefab;
        public Transform worldRoot;

        private Script script;

        private void Awake()
        {
            UserData.RegisterAssembly();
            script = new Script(CoreModules.Preset_HardSandbox);
            script.Globals["SpawnBlock"] = (Action<double, double, double>)SpawnBlock;
            script.Globals["Log"] = (Action<string>)Debug.Log;

            if (luaScript != null)
            {
                script.DoString(luaScript.text);
            }
        }

        private void Start()
        {
            CallLuaFunction("generate_world");
        }

        public void CallLuaFunction(string functionName)
        {
            DynValue function = script.Globals.Get(functionName);
            if (function.IsNotNil() && function.Type == DataType.Function)
            {
                script.Call(function);
            }
        }

        public void SpawnBlock(double x, double y, double z)
        {
            if (blockPrefab == null || worldRoot == null)
                return;

            Vector3 position = new Vector3((float)x, (float)y, (float)z);
            Instantiate(blockPrefab, position, Quaternion.identity, worldRoot);
        }
    }
}
