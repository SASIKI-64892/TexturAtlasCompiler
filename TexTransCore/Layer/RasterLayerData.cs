using System;
using UnityEngine;

namespace net.rs64.TexTransCore.Layer
{
    [Serializable]
    public class RasterLayerData : AbstractLayerData
    {
        public Texture2D RasterTexture;
        public Vector2Int TexturePivot;
    }
}