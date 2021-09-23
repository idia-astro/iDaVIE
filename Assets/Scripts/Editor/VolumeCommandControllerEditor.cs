using UnityEngine;
using System.Collections;
using VolumeData;
using UnityEditor;


[CustomEditor(typeof(VolumeCommandController))]
public class VolumeCommandControllerEditor : Editor
{

    public ColorMapEnum colormap;

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        VolumeCommandController volumeCommandController = (VolumeCommandController)target;

        EditorGUILayout.LabelField("Transform");
        if (GUILayout.Button("Reset Transform"))
        {
            volumeCommandController.resetTransform();
        }
        EditorGUILayout.LabelField("Threshold");
        if (GUILayout.Button("Reset Threshold"))
        {
            volumeCommandController.resetThreshold();
        }

        if (GUILayout.Button("Edit Min Threshold"))
        {
            volumeCommandController.startThresholdEditing(false);
        }

        if (GUILayout.Button("Edit Max Threshold"))
        {
            volumeCommandController.startThresholdEditing(true);
        }

        if (GUILayout.Button("Save Threshold"))
        {
            volumeCommandController.endThresholdEditing();
        }

        EditorGUILayout.LabelField("Color Map");

        colormap = (ColorMapEnum)EditorGUILayout.EnumPopup("Color:", colormap);

        if (GUILayout.Button("Change ColorMap"))
        {
            volumeCommandController.setColorMap(colormap);
        }

        EditorGUILayout.LabelField("Cropping");

        if (GUILayout.Button("Crop to Region"))
        {
            volumeCommandController.cropDataSet();
        }

        if (GUILayout.Button("Reset Crop"))
        {
            volumeCommandController.resetCropDataSet();
        }

        EditorGUILayout.LabelField("Masking");

        if (GUILayout.Button("Mask On"))
        {
            volumeCommandController.setMask(MaskMode.Enabled);
        }

        if (GUILayout.Button("Mask Off"))
        {
            volumeCommandController.setMask(MaskMode.Disabled);
        }

        if (GUILayout.Button("Mask Invert"))
        {
            volumeCommandController.setMask(MaskMode.Inverted);
        }

        if (GUILayout.Button("Mask Isolate"))
        {
            volumeCommandController.setMask(MaskMode.Isolated);
        }
        
        if (GUILayout.Button("Projection Maximum"))
        {
            volumeCommandController.setProjection(ProjectionMode.MaximumIntensityProjection);
        }
        
        if (GUILayout.Button("Projection Average"))
        {
            volumeCommandController.setProjection(ProjectionMode.AverageIntensityProjection);
        }
        
        if (GUILayout.Button("Sampling mode Maximum"))
        {
            volumeCommandController.SetSamplingMode(true);
        }
        
        if (GUILayout.Button("Sampling mode Average"))
        {
            volumeCommandController.SetSamplingMode(false);
        }
    }
}