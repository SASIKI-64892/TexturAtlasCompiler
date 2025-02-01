using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool.MultiLayerImage;
using net.rs64.TexTransTool.MultiLayerImage.Importer;
namespace net.rs64.TexTransTool.Editor.MultiLayerImage
{
    [CustomEditor(typeof(RasterLayer))]
    [CanEditMultipleObjects]
    internal class RasterLayerEditor : AbstractLayerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var thisTarget = target as RasterLayer;
            if (targets.Length != 1) { return; }
            if (thisTarget.RasterTexture != null) { EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(GUILayout.Height(400)), thisTarget.RasterTexture, ScaleMode.ScaleToFit); }
        }


    }
    [CustomEditor(typeof(RasterImportedLayer))]
    [CanEditMultipleObjects]
    internal class RasterImportedLayerEditor : AbstractLayerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var thisTarget = target as RasterImportedLayer;
            if (targets.Length != 1) { return; }
            if (thisTarget.ImportedImage != null && CanvasImportedImagePreviewManager.GetPreview(thisTarget.ImportedImage) != null)
            {
                EditorGUI.DrawTextureTransparent(EditorGUILayout.GetControlRect(GUILayout.Height(400)), CanvasImportedImagePreviewManager.GetPreview(thisTarget.ImportedImage), ScaleMode.ScaleToFit);
            }
        }


    }
}
