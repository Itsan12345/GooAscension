using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SunkenSpiresGenerator))]
public class SunkenSpiresGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SunkenSpiresGenerator gen = (SunkenSpiresGenerator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Tools", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("⚙  Generate Level", GUILayout.Height(36)))
        {
            Undo.RecordObject(gen.groundTilemap, "Generate Level");
            gen.GenerateLevel();
            EditorUtility.SetDirty(gen.groundTilemap);
            if (gen.platformTilemap)   EditorUtility.SetDirty(gen.platformTilemap);
            if (gen.waterTilemap)      EditorUtility.SetDirty(gen.waterTilemap);
            if (gen.backgroundTilemap) EditorUtility.SetDirty(gen.backgroundTilemap);
        }

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("✕  Clear Level", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Clear Level",
                "This will erase all generated tiles and objects. Are you sure?", "Clear", "Cancel"))
            {
                gen.ClearAll();
                EditorUtility.SetDirty(gen.groundTilemap);
            }
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Setup checklist before generating:\n" +
            "1. Create a Grid GameObject (2D Object → Grid)\n" +
            "2. Add 4 Tilemap children: Ground, Platform, Water, Background\n" +
            "3. On Ground Tilemap: add Tilemap Collider 2D + Composite Collider 2D\n" +
            "4. On Platform Tilemap: add Tilemap Collider 2D + Platform Effector 2D\n" +
            "5. On Water Tilemap: add Tilemap Collider 2D → set as Trigger, tag = 'Water'\n" +
            "6. Assign all Tilemap references and at least solidTile here\n" +
            "7. Click Generate Level!",
            MessageType.Info);
    }
}
