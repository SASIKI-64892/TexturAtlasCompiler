using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool.MultiLayerImage;

namespace net.rs64.TexTransTool.Editor.MultiLayerImage
{
    [CustomEditor(typeof(AbstractLayer), true)]
    [CanEditMultipleObjects]
    internal class AbstractLayerEditor : UnityEditor.Editor
    {
        private List<System.Type> _layerTypes;
        private string[] _layerNames;
        private int _selectedIndex = 0;

        void OnEnable()
        {
            _layerTypes = TypeCache.GetTypesDerivedFrom<AbstractLayer>()
                .Where(l => !l.IsAbstract)
                .ToList();
            _layerNames = _layerTypes.Select(t => t.Name).ToArray();
        }

        public override void OnInspectorGUI()
        {
            TextureTransformerEditor.DrawerWarning("MultiImageLayer".GetLocalize());
            base.OnInspectorGUI();

            if (_layerNames.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                _selectedIndex = EditorGUILayout.Popup(_selectedIndex, _layerNames);
                if (GUILayout.Button("Add Layer"))
                {
                    var selectedType = _layerTypes[_selectedIndex];
                    var currentLayer = (AbstractLayer)target;
                    var parent = currentLayer.transform.parent;
                    var newGameObject = new GameObject(selectedType.Name);
                    Undo.RegisterCreatedObjectUndo(newGameObject, $"Add {selectedType.Name}");
                    newGameObject.transform.SetParent(parent, false);
                    newGameObject.AddComponent(selectedType);
                    int siblingIndex = currentLayer.transform.GetSiblingIndex();
                    newGameObject.transform.SetSiblingIndex(siblingIndex);
                    Selection.activeGameObject = newGameObject;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
