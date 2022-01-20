using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public class PackageImporter : MonoBehaviour
{
    public static void ImportPackages()
    {
        var projectDir = Directory.GetParent(Application.dataPath);
        var packageDir = $"{projectDir.ToString()}/plugin_build";
        Debug.Log($"Looking for packages in {packageDir}");
        var packageNames = Directory.GetFiles(packageDir, "*.unitypackage");
        Debug.Log($"Found {packageNames.Length} packages");
        foreach (var package in packageNames)
        {
            Debug.Log($"Importing {package}");
            UnityEditor.AssetDatabase.ImportPackage(package, false);
        }
    }
}
