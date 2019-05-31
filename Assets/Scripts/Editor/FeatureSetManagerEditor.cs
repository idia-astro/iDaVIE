using UnityEngine;
using System.Collections;
using DataFeatures;
using UnityEditor;


[CustomEditor(typeof(FeatureSetManager))]
public class FeatureSetManagerEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FeatureSetManager featureSetManager = (FeatureSetManager)target;
        if (GUILayout.Button("Import Feature Set"))
        {
            featureSetManager.ImportFeatureSet();
        }
    }
}