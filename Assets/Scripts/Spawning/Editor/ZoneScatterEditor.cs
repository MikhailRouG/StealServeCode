using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZoneScatter))]
public class ZoneScatterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        var scatter = (ZoneScatter)target;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate"))
                scatter.Generate();

            if (GUILayout.Button("New Seed"))
            {
                Undo.RecordObject(scatter, "New Scatter Seed");
                scatter.Seed = Random.Range(int.MinValue, int.MaxValue);
                scatter.Generate();
            }

            if (GUILayout.Button("Clear"))
                scatter.Clear();
        }
    }
}
