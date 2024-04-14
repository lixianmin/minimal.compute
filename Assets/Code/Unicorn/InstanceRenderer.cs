/********************************************************************
created:    2024-03-16
author:     lixianmin

 https://github.com/ThousandAnt/ta-frustrum-culling.git

Copyright (C) - All Rights Reserved
*********************************************************************/

using System;
using Unicorn.Collections;
using UnityEngine;

namespace Unicorn
{
    public class InstanceRenderer
    {
        public InstanceRenderer(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }
            
            var material = renderer.sharedMaterial;
            _renderParams = new RenderParams(material);
            renderer.enabled = false;

            var meshFilter = renderer.GetComponent<MeshFilter>();
            _sharedMesh = meshFilter.sharedMesh;
        }

        public void RenderMeshInstanced(Slice<Matrix4x4> visibleMatrices)
        {
            // 单次推送的上限就是1023个
            // https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshInstanced.html
            const int maxBatchSize = 1023;
            var total = visibleMatrices.Size;

            for (var i = 0; i < total; i += maxBatchSize)
            {
                var size = Math.Min(maxBatchSize, total - i);
                Graphics.RenderMeshInstanced(_renderParams, _sharedMesh, 0, visibleMatrices.Items, size, i);
            }
        }
        
        private readonly RenderParams _renderParams;
        private readonly Mesh _sharedMesh;
    }
}