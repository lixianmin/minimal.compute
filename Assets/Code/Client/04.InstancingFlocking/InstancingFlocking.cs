/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System.Runtime.InteropServices;
using Unicorn;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class InstancingFlocking : MonoBehaviour
{
    [BurstCompile]
    private struct Boid
    {
        public readonly float3 position;
        public readonly float3 direction;

        public readonly float3 localBoundsCenter;
        public readonly float3 localBoundsSize;

        public Boid(Vector3 position, Bounds localBounds)
        {
            this.position = position;
            direction = Vector3.forward;

            localBoundsCenter = localBounds.center;
            localBoundsSize = localBounds.size;
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

    public Mesh boidMesh;
    public Material boidMaterial;

    private void OnEnable()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = _dog.Add(new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid))));

        _bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        _argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            _args[0] = (uint)boidMesh.GetIndexCount(0);
            _args[1] = (uint)boidsCount;
        }

        _argsBuffer.SetData(_args);

        var meshRenderer = boidPrefab.GetComponent<MeshRenderer>();
        _InitBoids(meshRenderer);
        _InitShader();
    }

    private void _InitBoids(MeshRenderer meshRenderer)
    {
        var prefabBounds = meshRenderer.localBounds;
        var boidArray = new Boid[boidsCount];
        for (var i = 0; i < boidsCount; i++)
        {
            var position = transform.position + Random.insideUnitSphere * spawnRadius;
            var matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            var localBounds = new Bounds(matrix.MultiplyPoint(prefabBounds.center), prefabBounds.size);

            boidArray[i] = new Boid(position, localBounds);
        }

        _kernel.SetBuffer(_boidsBuffer, boidArray);
    }

    private void _InitShader()
    {
        shader.SetFloat("rotation_speed", rotationSpeed);
        shader.SetFloat("boid_speed", boidSpeed);
        shader.SetFloat("boid_speed_variation", boidSpeedVariation);
        shader.SetVector("flock_position", target.transform.position);
        shader.SetFloat("neighbour_distance", neighbourDistance);
        shader.SetInt("boids_count", boidsCount);
        
        boidMaterial.SetBuffer("boids_buffer", _boidsBuffer.GetBuffer());
    }

    private void Update()
    {
        shader.SetFloat(TimeId, Time.time);
        shader.SetFloat(DeltaTimeId, Time.deltaTime);
        _kernel.Dispatch(boidsCount);

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, _bounds, _argsBuffer);
    }

    private void OnDisable()
    {
        _dog.DisposeAndClear();
        _argsBuffer.Dispose();
    }

    private readonly DisposeDog _dog = new();
    private ComputeKernel _kernel;
    private RWStructuredBuffer<Boid> _boidsBuffer;

    private readonly uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
    private ComputeBuffer _argsBuffer;
    private Bounds _bounds;

    private static readonly int TimeId = Shader.PropertyToID("time");
    private static readonly int DeltaTimeId = Shader.PropertyToID("delta_time");
}