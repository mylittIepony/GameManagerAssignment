using System;
using SymmetryBreakStudio.TastyGrassShader.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SymmetryBreakStudio.TastyGrassShader.Editor
{
    public static class SharedEditorUI
    {
        public static void SharedEditorGUIHeader()
        {
            bool tgsHdrpInstalled = SharedEditorTools.IsHdrpPackageInstalled();
            bool tgsUrpInstalled = SharedEditorTools.IsUrpPackageInstalled();
            if (!tgsHdrpInstalled && !tgsUrpInstalled)
            {
                EditorGUILayout.HelpBox("No HDRP or URP package for Tasty Grass Shader installed.", MessageType.Error);
                if (GUILayout.Button("Open Setup"))
                {
                    TgsSetup.RunSetup();
                }
            }

            UpdateHandler.DisplayUpdateBox();
        }

        public static void SharedEditorFooter()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.PrefixLabel("Links: ");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("FAQ & Troubleshooting..."))
                {
                    Application.OpenURL("https://github.com/SymmetryBreakStudio/TastyGrassShader/wiki/3.-FAQ");
                }
                if (GUILayout.Button("Documentation..."))
                {
                    Application.OpenURL("https://github.com/SymmetryBreakStudio/TastyGrassShader/wiki");
                }
                if (GUILayout.Button("Discord..."))
                {
                    Application.OpenURL("https://discord.symmetrybreak.com/");
                }
            }
        }

        public static void SharedEditorMemoryStats(SharedTools.TgsStats stats)
        {
            long totalVramUsageBytes = 0;
            long totalBlades = 0;
            int totalActiveChunks = 0;
            foreach (var allInstance in TgsInstance.AllInstances)
            {
                totalVramUsageBytes += allInstance.Value.GetGrassBufferMemoryByteSize();
                int bladeCount =  allInstance.Value.actualBladeCount;
                totalBlades += bladeCount;
                if (bladeCount > 0)
                {
                    totalActiveChunks++;
                }
            }

            double grassGraphicsRamMb = stats.GrassMeshBytes / (1024.0 * 1024.0);
            double totalGraphicsUsageMb = totalVramUsageBytes / (1024.0 * 1024.0);

            double grassGraphicsRamOfTotal = (grassGraphicsRamMb / SystemInfo.graphicsMemorySize) * 100.0;
            double totalGraphicsRamOfTotal = (totalGraphicsUsageMb / SystemInfo.graphicsMemorySize) * 100.0;

            EditorGUILayout.HelpBox(
                $"Graphics Memory:\n" +
                $"\tthis\t{grassGraphicsRamMb:F2} MB ({grassGraphicsRamOfTotal:F1}% of {SystemInfo.graphicsMemorySize:F2} MB)\n" +
                $"\tall\t{totalGraphicsUsageMb:F2} MB ({totalGraphicsRamOfTotal:F1}% of {SystemInfo.graphicsMemorySize:F2} MB)\n" +
                $"Chunks:\n" +
                $"\tthis\t{stats.ChunkCountWithGrass} (With Grass) / {stats.ChunkCount} (Total)\n"+
                $"\tall\t{totalActiveChunks} (With Grass) / {TgsInstance.AllInstances.Count} (Total)\n"+
                $"Grass Blades:\n"+
                $"\tthis\t{stats.TotalBlades:n0}\n" +
                $"\tall\t{totalBlades:n0}",
                MessageType.Info);
        }
    
        // NOTE-Julian: I use delegates in this case over interfaces,
        // because interfaces can't take all methods that are dealing with layers without introducing templates.
        // Once templates are introduced, it's not possible to have a common function to draw the inspector anymore, since the type MUST be specified.
        // Delegates are more flexible in that regard. 
        public delegate int LayerAdd();

        public delegate int LayerGetCount();

        public delegate bool LayerGetHideStatusAt(int index);
    
        public delegate string LayerGetName(int index);

        public delegate void LayerRemoveAt(int index);

        public delegate void LayerSetHideStatusAt(int index, bool hide);

        public delegate object LayerGetAt(int index);

        public enum LayerEditorType
        {
            TgsTerrainLayer,
            TgsMeshLayer,
        }
        
        public static void DrawLayerInspector(
            SerializedProperty layers,
            GameObject instanceAsObject,
            LayerEditorType type,
            LayerGetName layerGetName,
            LayerRemoveAt layerRemoveAt,
            LayerGetAt layerGetAt,
            LayerAdd layerAdd,
            LayerGetCount getLayerCount,
            LayerGetHideStatusAt getHideStatusAt,
            LayerSetHideStatusAt setHideStatusAt)
        {
            GUIStyle style = GUI.skin.FindStyle("FrameBox");
            layers.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(layers.isExpanded, "Layers");

            if (layers.isExpanded)
            {
                EditorGUI.indentLevel += 1;
                using (new GUILayout.VerticalScope(style))
                {
                    for (int i = 0; i < layers.arraySize; i++)
                    {

                        SerializedProperty arrayItem = layers.GetArrayElementAtIndex(i);
                        using (new GUILayout.HorizontalScope())
                        {
                            switch (type)
                            {
                                case LayerEditorType.TgsTerrainLayer:
                                    DrawLayerGUITerrain(arrayItem, new GUIContent(layerGetName(i)), (TgsTerrainLayer)layerGetAt(i), instanceAsObject, i);
                                    break;
                                default:
                                    EditorGUILayout.PropertyField(arrayItem, new GUIContent(layerGetName(i)));
                                    break;
                            }
                            
                            setHideStatusAt(i,
                                GUILayout.Toggle(getHideStatusAt(i), "Hide", GUILayout.Width(60.0f)));
                            if (GUILayout.Button(new GUIContent("X", null, "Delete this layer."),
                                    GUILayout.Width(20.0f)))
                            {
                                Undo.RecordObject(instanceAsObject, "Remove Layer");
                                layerRemoveAt(i);
                                break;
                            }


                        }

                    }

                    EditorGUILayout.Space();
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"Layers: {getLayerCount()}");
                        if (GUILayout.Button("Add New Layer"))
                        {
                            Undo.RecordObject(instanceAsObject, "Add Layer");
                            layerAdd();
                        }
                    }
                }

                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        


        // Manual GUI for the TgsTerrain Layer, since we need to pass in the scene name and the terrain layer for the migration system to work.
        // (It is not really possible to that via custom GUI attributes alone, since a reference to the underlying object can't be passed.)
        static void DrawLayerGUITerrain(SerializedProperty property, GUIContent label, TgsTerrainLayer terrainLayer, GameObject sourceGameObject, int index)
        {
            EditorGUILayout.BeginVertical();
            property.isExpanded =  EditorGUILayout.Foldout(property.isExpanded, label);
            EditorGUI.indentLevel++;
            if (property.isExpanded)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(TgsTerrainLayer.settings)),
                    new GUIContent("Settings"));

                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(TgsTerrainLayer.targetTerrainLayer)),
                    new GUIContent("Target Terrain Layer"));
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(TgsTerrainLayer.distribution)),
                    new GUIContent("Distribution"));

                var distribution = (TgsTerrainLayer.TerrainLayerDistribution)property
                    .FindPropertyRelative(nameof(TgsTerrainLayer.distribution)).enumValueIndex;
                Texture2D storageScene = (Texture2D)property
                    .FindPropertyRelative(nameof(TgsTerrainLayer.paintedDensityMapStorage))
                    .objectReferenceValue;
                EditorGUI.indentLevel++;
                switch (distribution)
                {
                    case TgsTerrainLayer.TerrainLayerDistribution.Fill:
                        EditorGUILayout.LabelField(new GUIContent("Grass is everywhere with 100% density."));
                        break;
                    case TgsTerrainLayer.TerrainLayerDistribution.TastyGrassShaderPaintTool:

                        if (storageScene && !AssetDatabase.IsNativeAsset(storageScene))
                        {
                            EditorGUILayout.HelpBox(
                                "To support prefabs, create an asset from the texture. (Paint Texture is currently stored in the scene file.)",
                                MessageType.Warning, true);
                            if (terrainLayer.paintedDensityMapStorage &&
                                GUILayout.Button("Enable Prefab Support (Save As Unity Asset)"))
                            {
                                SharedEditorTools.CreateAssetInSceneFolder(sourceGameObject, storageScene,
                                    terrainLayer.GetEditorName(index));
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Prefabs supported. (Texture is an Asset.)", MessageType.Info,
                                true);
                        }

                        EditorGUILayout.PropertyField(
                            property.FindPropertyRelative(nameof(TgsTerrainLayer.paintedDensityMapStorage)),
                            new GUIContent("Paint Texture"));
                        break;
                    case TgsTerrainLayer.TerrainLayerDistribution.ByTerrainLayer:
                        EditorGUILayout.LabelField(new GUIContent("Target Terrain Layer is used."));
                        break;
                    case TgsTerrainLayer.TerrainLayerDistribution.ByCustomTexture:
                        EditorGUILayout.PropertyField(
                            property.FindPropertyRelative(nameof(TgsTerrainLayer.distributionTexture)),
                            new GUIContent("Distribution Texture"));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(TgsTerrainLayer.scaling)),
                            new GUIContent("Scaling"));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(TgsTerrainLayer.offset)),
                            new GUIContent("Offset"));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(TgsTerrainLayer.colorMask)),
                            new GUIContent("Color Mask"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (storageScene && distribution != TgsTerrainLayer.TerrainLayerDistribution.TastyGrassShaderPaintTool)
                {
                    EditorGUILayout.HelpBox(
                        "A paint texture is still referenced for this layer. You may want to remove the reference.",
                        MessageType.Warning, true);
                    if (GUILayout.Button("Remove Paint Texture"))
                    {
                        property.FindPropertyRelative(nameof(TgsTerrainLayer.paintedDensityMapStorage))
                            .objectReferenceValue = null;
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        
    }
}
