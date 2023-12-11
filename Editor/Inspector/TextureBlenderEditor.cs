#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
namespace net.rs64.TexTransTool.Editor
{

    [CustomEditor(typeof(TextureBlender))]
    internal class TextureBlenderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TextureTransformerEditor.DrawerWarning("TextureBlender");

            var thisTarget = target as TextureBlender;
            var thisSObject = serializedObject;

            EditorGUI.BeginDisabledGroup(PreviewContext.IsPreviewing(thisTarget));

            DrawerRelativeTextureSelector(thisSObject.FindProperty("TargetTexture"));

            var sBlendTexture = thisSObject.FindProperty("BlendTexture");
            TextureTransformerEditor.DrawerObjectReference<Texture2D>(sBlendTexture);

            var sColor = thisSObject.FindProperty("Color");
            EditorGUILayout.PropertyField(sColor);

            var sBlendType = thisSObject.FindProperty("BlendType");
            EditorGUILayout.PropertyField(sBlendType);

            EditorGUI.EndDisabledGroup();


            PreviewContext.instance.DrawApplyAndRevert(thisTarget);
            thisSObject.ApplyModifiedProperties();
        }

        public static void DrawerRelativeTextureSelector(SerializedProperty sRelativeTextureSelector)
        {
            var sTargetRenderer = sRelativeTextureSelector.FindPropertyRelative("TargetRenderer");
            TextureTransformerEditor.DrawerObjectReference<Renderer>(sTargetRenderer, null, TextureTransformerEditor.RendererFiltering);


            var sMaterialSelect = sRelativeTextureSelector.FindPropertyRelative("MaterialSelect");

            var TargetRenderer = sTargetRenderer.objectReferenceValue as Renderer;
            var TargetMaterials = TargetRenderer?.sharedMaterials;

            sMaterialSelect.intValue = ArraySelector(sMaterialSelect.intValue, TargetMaterials);

            var sTargetPropertyName = sRelativeTextureSelector.FindPropertyRelative("TargetPropertyName");
            PropertyNameEditor.DrawInspectorGUI(sTargetPropertyName);
            if (TargetMaterials != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ReplaceTexturePreview".GetLocalize());
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(GUILayout.Height(64f)), TargetMaterials[sMaterialSelect.intValue].GetTexture(sTargetPropertyName.FindPropertyRelative("_propertyName").stringValue) as Texture2D, ScaleMode.ScaleToFit);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static int ArraySelector<T>(int Select, T[] Array) where T : UnityEngine.Object
        {
            if (Array == null) return Select;
            int SelectCount = 0;
            int DistSelect = Select;
            int NewSelect = Select;
            foreach (var ArrayValue in Array)
            {
                EditorGUILayout.BeginHorizontal();

                if (EditorGUILayout.Toggle(SelectCount == Select, GUILayout.Width(20)) && DistSelect != SelectCount) NewSelect = SelectCount;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(ArrayValue, typeof(Material), true);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                SelectCount += 1;
            }
            return NewSelect;
        }
        public static void DrawerSummary(TextureBlender target)
        {
            var sobj = new SerializedObject(target);
            var sTargetRenderer = sobj.FindProperty("TargetRenderer");
            EditorGUILayout.PropertyField(sTargetRenderer);
            var sBlendTexture = sobj.FindProperty("BlendTexture");
            TextureTransformerEditor.DrawerObjectReference<Texture2D>(sBlendTexture);

            sobj.ApplyModifiedProperties();
        }
    }
}
#endif
