using UnityEditor;
using UnityEngine;
using VolumeData;

[CustomEditor(typeof(VolumeDataSet)), CanEditMultipleObjects]
public class VolumeDataSetEditor : Editor
{
	protected virtual void OnSceneGUI()
	{
		var targetDataSet = (VolumeDataSetRenderer)target;

		Vector3 offset = Vector3.zero;
		
		EditorGUI.BeginChangeCheck();
		Vector3 sliceMinWorldSpace = targetDataSet.transform.TransformPoint(targetDataSet.SliceMin - offset);		
		Vector3 sliceMaxWorldSpace = targetDataSet.transform.TransformPoint(targetDataSet.SliceMax - offset);		
		sliceMinWorldSpace = Handles.PositionHandle(sliceMinWorldSpace, targetDataSet.transform.rotation);
		sliceMaxWorldSpace = Handles.PositionHandle(sliceMaxWorldSpace, targetDataSet.transform.rotation);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(targetDataSet, "Change Volume Slicing Bounds");
			Vector3 sliceMinLocalSpace = targetDataSet.transform.InverseTransformPoint(sliceMinWorldSpace) + offset;
			Vector3 sliceMaxLocalSpace = targetDataSet.transform.InverseTransformPoint(sliceMaxWorldSpace) + offset;
			sliceMaxLocalSpace = new Vector3(Mathf.Clamp(sliceMaxLocalSpace.x, -0.5f, 0.5f), Mathf.Clamp(sliceMaxLocalSpace.y, -0.5f, 0.5f), Mathf.Clamp(sliceMaxLocalSpace.z, -0.5f, 0.5f));
			sliceMinLocalSpace = new Vector3(Mathf.Clamp(sliceMinLocalSpace.x, -0.5f, sliceMaxLocalSpace.x), Mathf.Clamp(sliceMinLocalSpace.y, -0.5f, sliceMaxLocalSpace.y), Mathf.Clamp(sliceMinLocalSpace.z, -0.5f, sliceMaxLocalSpace.z));
			targetDataSet.SliceMin = sliceMinLocalSpace;
			targetDataSet.SliceMax = sliceMaxLocalSpace;
			targetDataSet.Update();
		}
	}
}