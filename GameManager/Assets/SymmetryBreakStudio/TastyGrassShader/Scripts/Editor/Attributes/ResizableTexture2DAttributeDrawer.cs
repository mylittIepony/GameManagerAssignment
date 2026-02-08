using UnityEditor;
using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader.Editor
{
    [CustomPropertyDrawer(typeof(ResizableTexture2DAttribute))]
    public class ResizableTexture2DAttributeDrawer : PropertyDrawer
    {
        

        void ChangeResolution(int newResolution, SerializedProperty property)
        {
            Texture2D texture2D = (Texture2D) property.objectReferenceValue;
            RenderTexture tmp = RenderTexture.GetTemporary(newResolution, newResolution);
            Graphics.Blit(texture2D, tmp);
            texture2D.Reinitialize(newResolution, newResolution);
            SharedTools.StoreRenderTexture(tmp, texture2D);
            RenderTexture.ReleaseTemporary(tmp);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Texture2D texture2D = (Texture2D) property.objectReferenceValue;
            Rect pos = position;

            pos.height = EditorGUIUtility.singleLineHeight * 2;
            EditorGUI.HelpBox(EditorGUI.IndentedRect(pos), "Rescaling is not covered by Undo/Redo.", MessageType.Info);
            pos.y += pos.height + EditorGUIUtility.standardVerticalSpacing;
            
            pos.width *= 0.75f;
            pos.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(pos, property, label);
            pos.x += pos.width;
            pos.width = position.width * 0.25f;
            if (texture2D)
            {
                if(GUI.Button(pos, new GUIContent($"Rescale ({texture2D.width}x{texture2D.height})...")))
                {
                    GenericMenu menu = new GenericMenu();

                    for (int i = 4; i < 11; i++)
                    {
                        int resolution = 1 << i;
                        menu.AddItem(new GUIContent($"{resolution}x{resolution}"), texture2D.width == resolution, () => ChangeResolution(resolution, property));
                    }
                
                    menu.ShowAsContext();
                }
            }


            
        }
    }
}