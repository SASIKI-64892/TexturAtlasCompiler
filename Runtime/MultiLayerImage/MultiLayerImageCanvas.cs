using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using net.rs64.TexTransCore.BlendTexture;
using net.rs64.TexTransTool.Utils;
using UnityEditor;
using UnityEngine;
using static net.rs64.TexTransCore.BlendTexture.TextureBlend;

namespace net.rs64.TexTransTool.MultiLayerImage
{

    [AddComponentMenu("TexTransTool/MultiLayer/TTT MultiLayerImageCanvas")]
    public sealed class MultiLayerImageCanvas : TexTransRuntimeBehavior, ITTTChildExclusion
    {
        public RelativeTextureSelector TextureSelector;
        public Vector2Int TextureSize = new Vector2Int(2048, 2048);

        internal override List<Renderer> GetRenderers => new List<Renderer>() { TextureSelector.TargetRenderer };

        internal override bool IsPossibleApply => TextureSelector.TargetRenderer != null;

        internal override TexTransPhase PhaseDefine => TexTransPhase.BeforeUVModification;

        internal override void Apply([NotNull] IDomain domain)
        {
            if (!IsPossibleApply) { throw new TTTNotExecutable(); }
            var canvasContext = new CanvasContext(TextureSize, domain.GetTextureManager());

            var replaceTarget = TextureSelector.GetTexture();
            if (replaceTarget == null) { return; }

            var Layers = transform.GetChildren()
            .Select(I => I.GetComponent<AbstractLayer>())
            .Where(I => I != null)
            .Reverse();
            foreach (var layer in Layers) { layer.EvaluateTexture(canvasContext); }


            if (canvasContext.RootLayerStack.Stack.Count == 0) { return; }

            var firstLayer = canvasContext.RootLayerStack.Stack[0];
            firstLayer.BlendTextures.BlendTypeKey = "NotBlend";
            canvasContext.RootLayerStack.Stack[0] = firstLayer;

            foreach (var layer in canvasContext.RootLayerStack.GetLayers)
            {
                domain.AddTextureStack(replaceTarget, layer);
            }

        }
        internal class CanvasContext
        {
            public Vector2Int CanvasSize;
            public LayerStack RootLayerStack;
            public ITextureManager TextureManager;

            public CanvasContext(Vector2Int canvasSize, ITextureManager textureManager)
            {
                CanvasSize = canvasSize;
                RootLayerStack = new();
                TextureManager = textureManager;
            }
            public CanvasContext CreateSubCanvas => new CanvasContext(CanvasSize, TextureManager);
        }

        internal class LayerStack
        {
            public List<BlendLayer> Stack = new List<BlendLayer>();

            public IEnumerable<BlendTexturePair> GetLayers => Stack.Where(I => I.BlendTextures.Texture != null).Select(I => I.BlendTextures);



            public void AddRtForClipping(AbstractLayer abstractLayer, RenderTexture tex, string blendTypeKey)
            {
                var index = Stack.Count;
                index -= 1;
                if (index >= 0)
                {
                    var downLayer = Stack[index];
                    if (downLayer.RefLayer is LayerFolder layerFolder && layerFolder.PassThrough) { index = -1; }
                }

                if (index >= 0)
                {
                    var refBlendLayer = Stack[index];
                    var ClippingDist = refBlendLayer.BlendTextures.Texture as RenderTexture;
                    if (ClippingDist == null) { return; }
                    ClippingDist.BlendBlit(tex, blendTypeKey, true);
                }
            }

            public void AddRenderTexture(AbstractLayer abstractLayer, RenderTexture tex, string blendTypeKey)
            {
                Stack.Add(new BlendLayer(abstractLayer, tex, blendTypeKey));
            }
        }

        internal struct BlendLayer
        {
            public AbstractLayer RefLayer;
            public BlendTexturePair BlendTextures;

            public BlendLayer(AbstractLayer refLayer, Texture layer, string blendTypeKey)
            {
                RefLayer = refLayer;
                BlendTextures = new BlendTexturePair(layer, blendTypeKey);
            }

        }
    }
}