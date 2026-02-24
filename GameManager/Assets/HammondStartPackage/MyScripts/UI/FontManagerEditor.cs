/*

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(FontManager))]
public class FontManagerEditor : Editor
{
    bool _showPreview;
    List<FontManager.FontPreviewEntry> _previewEntries;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FontManager fm = (FontManager)target;

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("apply fonts", GUILayout.Height(32)))
        {
            Undo.RecordObject(fm.gameObject, "apply fonts");

            var allText = fm.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var t in allText)
                Undo.RecordObject(t, "apply Fonts");

            fm.ApplyFonts();
            EditorUtility.SetDirty(fm);
        }

        GUI.backgroundColor = Color.white;
        if (GUILayout.Button(_showPreview ? "hide preview" : "preview changes", GUILayout.Height(24)))
        {
            _showPreview = !_showPreview;
            if (_showPreview)
                _previewEntries = fm.PreviewChanges();
        }

        if (_showPreview && _previewEntries != null)
        {
            EditorGUILayout.Space(5);

            if (_previewEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("all fonts are already up to date.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"{_previewEntries.Count} changes pending:", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                foreach (var entry in _previewEntries)
                {
                    string objName = entry.textComponent != null ? entry.textComponent.gameObject.name : "(null)";
                    string from = entry.currentFont != null ? entry.currentFont.name : "(none)";
                    string to = entry.targetFont != null ? entry.targetFont.name : "(none)";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{entry.role}]", GUILayout.Width(70));

                    if (GUILayout.Button(objName, EditorStyles.linkLabel))
                    {
                        if (entry.textComponent != null)
                            EditorGUIUtility.PingObject(entry.textComponent.gameObject);
                    }

                    EditorGUILayout.LabelField($"{from} --> {to}");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
*/