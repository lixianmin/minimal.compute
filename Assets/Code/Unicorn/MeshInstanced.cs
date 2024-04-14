/********************************************************************
created:    2024-03-16
author:     lixianmin

 https://github.com/ThousandAnt/ta-frustrum-culling.git

Copyright (C) - All Rights Reserved
*********************************************************************/

using System;
using Unicorn.Collections;
using Unity.Collections;
using Unity.Mathematics;
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

            var renderParams = InstanceTools.CreateRenderParams(sharedMaterials[0], renderer);
            var my = new MeshInstanced(renderParams, sharedMesh);
            return my;
        }

        private MeshInstanced(RenderParams renderParams, Mesh sharedMesh)
        {
            _renderParams = renderParams;
            _sharedMesh = sharedMesh;
        }

        public void Render<T>(Slice<T> visibleMatrices) where T : unmanaged
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

        public void Render<T>(NativeArray<T> visibleMatrices) where T : unmanaged
        {
            // 单次推送的上限就是1023个
            // https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshInstanced.html
            const int maxBatchSize = 1023;
            var total = visibleMatrices.Length;

            for (var i = 0; i < total; i += maxBatchSize)
            {
                var size = Math.Min(maxBatchSize, total - i);
                Graphics.RenderMeshInstanced(_renderParams, _sharedMesh, 0, visibleMatrices, size, i);
            }
        }

        private readonly RenderParams _renderParams;
        private readonly Mesh _sharedMesh;
    }
}