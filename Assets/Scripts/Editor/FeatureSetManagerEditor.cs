using UnityEngine;
using System.Collections;
using DataFeatures;
using UnityEditor;


[CustomEditor(typeof(FeatureSetManager))]
public class LevelScriptEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetManager featureSetManagerScript = (FeatureSetManager)target;
        if (GUILayout.Button("Import Feature Set"))
        {
            featureSetManagerScript.ImportFeatureSet();
        }
    }
}