// LICENSE
//  See end of file for license information.
//
// AUTHOR
//   Forrest Smith

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace fts
{

    // ------------------------------------------------------------------------
    // Native API for loading/unloading NativePlugins
    // ------------------------------------------------------------------------
    static class SystemLibrary
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static public extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static public extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        static public extern uint GetLastError();

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static public extern bool SetDllDirectory(string dirName);
    }


    // ------------------------------------------------------------------------
    // Singleton class to help with loading and unloading of native plugins
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class NativePluginLoader : MonoBehaviour, ISerializationCallbackReceiver
    {
        // Constants
        const string EXT = ".dll";

        // Static fields
        static NativePluginLoader _singleton;

        // Private fields
        Dictionary<string, IntPtr> _loadedPlugins = new Dictionary<string, IntPtr>();
        string _path;

        // Static Properties
        static NativePluginLoader singleton {
            get {
                if (_singleton == null) {
                    var go = new GameObject("PluginLoader");
                    var pl = go.AddComponent<NativePluginLoader>();
                    Debug.Assert(_singleton == pl);
                }
                return _singleton;
            }
        }

        // Methods
        void Awake() {
            if (_singleton != null)
            {
                Debug.LogError(
                    string.Format("Created multiple NativePluginLoader objects. Destroying duplicate created on GameObject [{0}]",
                    this.gameObject.name));
                Destroy(this);
                return;
            }

            _singleton = this;
            DontDestroyOnLoad(this.gameObject);
            _path = Application.dataPath + "/Plugins/";
            if (!Application.isEditor)
                _path += "x86_64/";
            LoadAll();
        }

        void OnDestroy() {
            UnloadAll();
            _singleton = null;
        }

        void UnloadAll()
        {
            foreach (var kvp in _loadedPlugins) {
                bool result = SystemLibrary.FreeLibrary(kvp.Value);
            }
            _loadedPlugins.Clear();
        }

        void LoadAll() {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                foreach (var type in assembly.GetTypes()) {
                    var typeAttributes = type.GetCustomAttributes(typeof(PluginAttr), true);
                    if (typeAttributes.Length > 0)
                    {
                        Debug.Assert(typeAttributes.Length == 1);

                        var typeAttribute = typeAttributes[0] as PluginAttr;
                        var pluginName = typeAttribute.pluginName;
                        IntPtr pluginHandle = IntPtr.Zero;
                        if (!_loadedPlugins.TryGetValue(pluginName, out pluginHandle)) {
                            var pluginPath = _path + pluginName + EXT;
                            if (!SystemLibrary.SetDllDirectory(_path))
                                throw new System.Exception("Failed to set dll directory [" + pluginPath + "]");
                            pluginHandle = SystemLibrary.LoadLibrary(pluginPath);
                            if (pluginHandle == IntPtr.Zero)
                                throw new System.Exception("Failed to load plugin [" + pluginPath + "]");

                            _loadedPlugins.Add(pluginName, pluginHandle);
                        }

                        var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        foreach (var field in fields) {
                            var fieldAttributes = field.GetCustomAttributes(typeof(PluginFunctionAttr), true);
                            if (fieldAttributes.Length > 0) {
                                Debug.Assert(fieldAttributes.Length == 1);

                                var fieldAttribute = fieldAttributes[0] as PluginFunctionAttr;
                                var functionName = fieldAttribute.functionName;

                                var fnPtr = SystemLibrary.GetProcAddress(pluginHandle, functionName);
                                if (fnPtr == IntPtr.Zero) {
                                    Debug.LogError(string.Format("Failed to find function [{0}] in plugin [{1}]. Err: [{2}]", functionName, pluginName, SystemLibrary.GetLastError()));
                                    continue;
                                }

                                var fnDelegate = Marshal.GetDelegateForFunctionPointer(fnPtr, field.FieldType);
                                field.SetValue(null, fnDelegate);
                            }
                        }
                    }
                }
            }
        }

        bool _reloadAfterDeserialize = false;
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            if (_loadedPlugins.Count > 0) {
                UnloadAll();
                _reloadAfterDeserialize = true;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()  {
            if (_reloadAfterDeserialize) {
                LoadAll();
                _reloadAfterDeserialize = false;
            }
        }
    }


    // ------------------------------------------------------------------------
    // Attribute for Plugin APIs
    // ------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PluginAttr : System.Attribute
    {
        public string pluginName { get; private set; }
        public PluginAttr(string pluginName) {
            this.pluginName = pluginName;
        }
    }


    // ------------------------------------------------------------------------
    // Attribute for functions inside a Plugin API
    // ------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PluginFunctionAttr : System.Attribute
    {
        public string functionName { get; private set; }
        public PluginFunctionAttr(string functionName) {
            this.functionName = functionName;
        }
    }

} // namespace fts
