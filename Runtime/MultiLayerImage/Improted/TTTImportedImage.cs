#nullable enable
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using net.rs64.TexTransCore;
using System;

namespace net.rs64.TexTransTool.MultiLayerImage
{

    public abstract class TTTImportedImage : ScriptableObject
    {
        public TTTImportedCanvasDescription CanvasDescription = null!;// インポートされた時点で正しくキャンバスの情報に対する参照が入っていることを期待します。
        // public Texture2D PreviewTexture;


        // ここでの writeTarget は CanvasSize と同じことが前提
        public virtual void LoadImage<TTCE>(ITTImportedCanvasSource importSource, TTCE ttce, ITTRenderTexture writeTarget)
        where TTCE : ITexTransComputeKeyQuery
        , ITexTransGetComputeHandler
        , ITexTransCopyRenderTexture
        , ITexTransCreateTexture
        , ITexTransRenderTextureIO
        , ITexTransRenderTextureUploadToCreate
        , ITexTransDriveStorageBufferHolder
        {
            var ppB = EnginUtil.GetPixelParByte(CanvasDescription.ImportedImageFormat, TexTransCoreTextureChannel.RGBA);
            var length = CanvasDescription.Width * CanvasDescription.Height * ppB;
            using var na = new NativeArray<byte>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            LoadImage(importSource, na.AsSpan());
            ttce.UploadTexture<byte>(writeTarget, na.AsSpan(), CanvasDescription.ImportedImageFormat);
        }
        //　最低限こっちの関数が動けばよいが、上の関数が高速に動かせるならそっちを実装したほうが良い。
        // もし、上の関数が実装できるならこの関数は実装しなくてもよい
        protected abstract void LoadImage(ITTImportedCanvasSource importSource, Span<byte> writeTarget);
        //  { throw new NotImplementedException(); }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(TTTImportedImage))]
    public class TTTImportedPngEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            EditorGUILayout.LabelField("Debugなどでこのオブジェクトの中身を見ようとしないでください!!!、UnityEditorが停止します!!!");
            var thisTarget = target as TTTImportedImage;
            // if (thisTarget.PreviewTexture != null) { EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(GUILayout.Height(400)), thisTarget.PreviewTexture, ScaleMode.ScaleToFit); }

        }
    }
#endif
}
