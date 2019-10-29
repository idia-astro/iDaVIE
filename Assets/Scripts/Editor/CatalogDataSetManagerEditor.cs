using UnityEngine;
using CatalogData;
using UnityEditor;


[CustomEditor(typeof(CatalogDataSetManager))]
public class CatalogDataSetManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CatalogDataSetManager catalogDataSetManager = (CatalogDataSetManager)target;

        /*
        for (int i = 1; i < catalogDataSetManager.NumberDataSets; i++)
        {
            catalogDataSetManager.SelectNextSet();
            catalogDataSetManager.SetActiveSetVisibility(false);
        }
        catalogDataSetManager.SelectNextSet();
        */

        EditorGUILayout.TextField("Active Data Set:", catalogDataSetManager.GetActiveSetName());


        if (GUILayout.Button("Select Next Set"))
        {
            catalogDataSetManager.SelectNextSet();
        }

        if (GUILayout.Button("Set Visibility On"))
        {
            catalogDataSetManager.SetActiveSetVisibility(true);
        }


        if (GUILayout.Button("Set Visibility Off"))
        {
            catalogDataSetManager.SetActiveSetVisibility(false);
        }

        if (GUILayout.Button("Reset Position"))
        {
            /*
            Transform[] children = catalogDataSetManager.transform.GetComponentsInChildren<Transform>();

            foreach (Transform t in children)
                t.position = catalogDataSetManager.transform.TransformPoint(new Vector3(0, 0, 0));
                */
            CatalogDataSetRenderer[] catalogDataSetRenderers = catalogDataSetManager.GetComponentsInChildren<CatalogDataSetRenderer>();
            foreach (CatalogDataSetRenderer catalogDataSetRenderer in catalogDataSetRenderers)
            {
                catalogDataSetRenderer.resetLocalPosition();
            }
        }

    }
}