﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using Rs.TexturAtlasCompiler.VRCBulige;
namespace Rs.TexturAtlasCompiler.VRCBulige.Editor
{
    [CustomEditor(typeof(AtlasSetAvatarTag))]
    public class AtlasSetAvatarTagEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var serialaizeatlaset = serializedObject.FindProperty("AtlasSet");
            var ClientSelect = serializedObject.FindProperty("ClientSelect");
            var SkindMesh = serialaizeatlaset.FindPropertyRelative("AtlasTargetMeshs");
            var Staticmesh = serialaizeatlaset.FindPropertyRelative("AtlasTargetStaticMeshs");
            var TextureSize = serialaizeatlaset.FindPropertyRelative("AtlasTextureSize");
            var Pading = serialaizeatlaset.FindPropertyRelative("Pading");
            var PadingType = serialaizeatlaset.FindPropertyRelative("PadingType");
            var SortingType = serialaizeatlaset.FindPropertyRelative("SortingType");
            var Contenar = serialaizeatlaset.FindPropertyRelative("Contenar");
            var PostPorsesars = serializedObject.FindProperty("PostPrcess");

            var AtlasSetAvatarTag = target as AtlasSetAvatarTag;
            var IsAppry = AtlasSetAvatarTag.AtlasSet.IsAppry;

            EditorGUI.BeginDisabledGroup(IsAppry);
            EditorGUILayout.PropertyField(SkindMesh);
            EditorGUILayout.PropertyField(Staticmesh);
            EditorGUILayout.PropertyField(TextureSize);
            EditorGUILayout.PropertyField(Pading);
            EditorGUILayout.PropertyField(PadingType);
            EditorGUILayout.PropertyField(SortingType);
            EditorGUILayout.PropertyField(ClientSelect);
            EditorGUILayout.PropertyField(Contenar);
            EditorGUILayout.PropertyField(PostPorsesars);
            EditorGUI.EndDisabledGroup();


            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(AtlasSetAvatarTag.AtlasSet.Contenar == null || IsAppry);
            if (GUILayout.Button("Appry"))
            {
                Undo.RecordObject(AtlasSetAvatarTag, "AtlasAppry");
                AtlasSetAvatarTag.AtlasSet.Appry();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!IsAppry);
            if (GUILayout.Button("Revart"))
            {
                Undo.RecordObject(AtlasSetAvatarTag, "AtlasRevart");
                AtlasSetAvatarTag.AtlasSet.Revart();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(IsAppry);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("TexturAtlasCompile!"))
            {
                if (!AtlasSetAvatarTag.AtlasSet.IsAppry)
                {
                    Undo.RecordObject(AtlasSetAvatarTag, "AtlasCompile");
                    if (AtlasSetAvatarTag.PostPrcess.Any())
                    {
                        foreach (var PostPrces in AtlasSetAvatarTag.PostPrcess)
                        {
                            AtlasSetAvatarTag.AtlasSet.AtlasCompilePostCallBack += (i) => PostPrces.Proses(i);
                        }
                    }
                    else
                    {
                        AtlasSetAvatarTag.AtlasSet.AtlasCompilePostCallBack = (i) => { };
                    }
                    Compiler.AtlasSetCompile(AtlasSetAvatarTag.AtlasSet, AtlasSetAvatarTag.ClientSelect, true);
                }
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

        }
    }
}
#endif