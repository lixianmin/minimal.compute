/********************************************************************
created:    2024-04-12
author:     lixianmin

https://www.youtube.com/watch?v=4CNad5V9wD8

Copyright (C) - All Rights Reserved
*********************************************************************/

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unicorn
{
    public class MbWaterEffect : MonoBehaviour
    {
        private void Awake()
        {
            _copyTextureKernel = new ComputeKernel(computeShader, "CopyTexture");
            _mainKernel = new ComputeKernel(computeShader, "CSMain");

            _idEffect = Shader.PropertyToID("effect");
            _idDispersion = Shader.PropertyToID("dispersion");
        }

        private void OnEnable()
        {
            _InitializeTexture(out NState);
            _InitializeTexture(out Nm1State);
            _InitializeTexture(out Np1State);

            _mainKernel.SetTexture("NState", NState);
            _mainKernel.SetTexture("Nm1State", Nm1State);
            _mainKernel.SetTexture("Np1State", Np1State);

            _copyTextureKernel.SetTexture("NState", NState);
            _copyTextureKernel.SetTexture("Nm1State", Nm1State);
            _copyTextureKernel.SetTexture("Np1State", Np1State);

            waveMaterial.mainTexture = NState;
        }

        private void OnDisable()
        {
            NState.Release();
            Nm1State.Release();
            Np1State.Release();
        }

        private void _InitializeTexture(out RenderTexture texture)
        {
            texture = new RenderTexture(textureSize, textureSize, 0,
                GraphicsFormat.R16G16B16A16_UNorm)
            {
                enableRandomWrite = true
            };
            texture.Create();
        }

        private void Update()
        {
            // Graphics.CopyTexture(NState, Nm1State);
            // Graphics.CopyTexture(Np1State, NState);
            _copyTextureKernel.Dispatch(textureSize, textureSize);

            computeShader.SetVector(_idEffect, effect);
            computeShader.SetFloat(_idDispersion, dispersion);

            _mainKernel.Dispatch(textureSize, textureSize);
        }

        public ComputeShader computeShader;
        public Material waveMaterial;
        public RenderTexture NState, Nm1State, Np1State;

        public int textureSize = 256;
        public Vector3 effect = new(128, 128, 1);
        public float dispersion = 0.98f;

        private ComputeKernel _mainKernel;
        private ComputeKernel _copyTextureKernel;

        private int _idEffect;
        private int _idDispersion;
    }
}