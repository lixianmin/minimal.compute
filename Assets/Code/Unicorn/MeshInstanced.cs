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
    public class MeshInstanced
    {
        public static MeshInstanced Create(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return null;
            }

            var sharedMesh = meshFilter.sharedMesh;
            var sharedMaterials = renderer.sharedMaterials;
            if (sharedMesh == null || sharedMaterials is not { Length: 1 } || !sharedMaterials[0].enableInstancing)
            {
                return null;
            }

            // 启用了static batching, 就不要再使用instancing了
            var name = sharedMesh.name;
            if (name.StartsWith("Combined Mesh"))
            {
                return null;
            }

            renderer.enabled = false;

            var renderParams = _CreateRenderParams(sharedMaterials[0], renderer);
            var my = new MeshInstanced(renderParams, sharedMesh);
            return my;
        }

        private MeshInstanced(RenderParams renderParams, Mesh sharedMesh)
        {
            _renderParams = renderParams;
            _sharedMesh = sharedMesh;
        }

        public void Render(Slice<Matrix4x4> visibleMatrices)
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

        private static RenderParams _CreateRenderParams(Material material, Renderer renderer, Camera camera = null)
        {
            var renderParams = new RenderParams(material);
            if (renderer != null)
            {
                renderParams.shadowCastingMode = renderer.shadowCastingMode;
            }

            // 如果不设置, 就会在所有camera中渲染一遍, 这包括UICamera, 所以在release的时候必须要设置
            // 但如果一直设置, 在Editor中的Scene窗口就看不到了, 所以在Editor中倾向于不设置
            if (!Application.isEditor)
            {
                renderParams.camera = camera;
            }

            return renderParams;
        }

        private readonly RenderParams _renderParams;
        private readonly Mesh _sharedMesh;
    }
}