using UnityEngine;
using System.Collections;
using VolumeData;
using UnityEditor;


[CustomEditor(typeof(VolumeSpeechController))]
public class VolumeSpeechControllerEditor : Editor
{

    public ColorMapEnum colormap;

    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        VolumeSpeechController volumeSpeechController = (VolumeSpeechController)target;

        EditorGUILayout.LabelField("Transform");
        if (GUILayout.Button("Reset Transform"))
        {
            volumeSpeechController.resetTransform();
        }
        EditorGUILayout.LabelField("Threshold");
        if (GUILayout.Button("Reset Threshold"))
        {
            volumeSpeechController.resetThreshold();
        }

        if (GUILayout.Button("Edit Min Threshold"))
        {
            volumeSpeechController.ChangeSpeechControllerState(VolumeSpeechController.SpeechControllerState.EditThresholdMin);
        }

        if (GUILayout.Button("Edit Max Threshold"))
        {
            volumeSpeechController.ChangeSpeechControllerState(VolumeSpeechController.SpeechControllerState.EditThresholdMax);
        }

        if (GUILayout.Button("Save Threshold"))
        {
            volumeSpeechController.ChangeSpeechControllerState(VolumeSpeechController.SpeechControllerState.Idle);
        }

        EditorGUILayout.LabelField("Color Map");

        colormap = (ColorMapEnum)EditorGUILayout.EnumPopup("Color:", colormap);

        if (GUILayout.Button("Change ColorMap"))
        {
            volumeSpeechController.setColorMap(colormap);
        }

        EditorGUILayout.LabelField("Cropping");

        if (GUILayout.Button("Crop to Region"))
        {
            volumeSpeechController.cropDataSet();
        }

        if (GUILayout.Button("Reset Crop"))
        {
            volumeSpeechController.resetCropDataSet();
        }

        EditorGUILayout.LabelField("Masking");

        if (GUILayout.Button("Mask On"))
        {
            volumeSpeechController.setMask(MaskMode.Enabled);
        }

        if (GUILayout.Button("Mask Off"))
        {
            volumeSpeechController.setMask(MaskMode.Disabled);
        }

        if (GUILayout.Button("Mask Invert"))
        {
            volumeSpeechController.setMask(MaskMode.Inverted);
        }

        if (GUILayout.Button("Mask Isolate"))
        {
            volumeSpeechController.setMask(MaskMode.Isolated);
        }
        
        if (GUILayout.Button("Projection Maximum"))
        {
            volumeSpeechController.setProjection(ProjectionMode.MaximumIntensityProjection);
        }
        
        if (GUILayout.Button("Projection Average"))
        {
            volumeSpeechController.setProjection(ProjectionMode.AverageIntensityProjection);
        }
    }
}