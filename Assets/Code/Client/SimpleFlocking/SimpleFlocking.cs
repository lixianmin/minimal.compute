/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System.Runtime.InteropServices;
using Unicorn;
using Unicorn.Collections;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using Random = UnityEngine.Random;

public class SimpleFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public Bounds localBounds;

        public Boid(Vector3 pos)
        {
            position = pos;
            direction = Vector3.forward;
            localBounds = default;
        }
    }

    public ComputeShader shader;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;

    private ComputeKernel _kernel;
    private RWStructuredBuffer<Boid> _boidsBuffer;

    private MeshInstanced _meshInstanced;
    private readonly Slice<Matrix4x4> _matrices = new();
    private readonly Plane[] _frustumPlane = new Plane[6];

    private void Awake()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid)));

        var meshRenderer = boidPrefab.GetComponent<MeshRenderer>();
        _meshInstanced = MeshInstanced.Create(meshRenderer);

        _InitBoids(meshRenderer);
        _InitShader();
    }

    private void _InitBoids(MeshRenderer meshRenderer)
    {
        var prefabBounds = meshRenderer.localBounds;
        var boidData = new Boid[boidsCount];
        for (var i = 0; i < boidsCount; i++)
        {
            var position = transform.position + Random.insideUnitSphere * spawnRadius;
            boidData[i] = new Boid(position);

            var matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            var localBounds = new Bounds(matrix.MultiplyPoint(prefabBounds.center), prefabBounds.size);
            boidData[i].localBounds = localBounds;
        }

        _kernel.SetBuffer(_boidsBuffer, boidData);
    }

    private void _InitShader()
    {
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    private void Update()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }
        
        GeometryUtility.CalculateFrustumPlanes(mainCamera, _frustumPlane);

        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);
        _kernel.Dispatch(boidsCount);

        var boidData = _boidsBuffer.GetDataAsync();
        _matrices.Clear();
        for (var i = 0; i < boidData.Length; i++)
        {
            var boid = boidData[i];
            var rotation = Quaternion.identity;
            if (boid.direction != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(boid.direction);
            }

            var matrix = Matrix4x4.TRS(boid.position, rotation, Vector3.one);
            var worldBounds = new Bounds(matrix.MultiplyPoint(boid.localBounds.center), boid.localBounds.size);
            if (InstanceTools.TestPlanesAABB(_frustumPlane, worldBounds))
            {
                _matrices.Add(matrix);
            }
        }

        _meshInstanced.Render(_matrices);
    }

    private void OnDestroy()
    {
        _boidsBuffer.Dispose();
    }
}