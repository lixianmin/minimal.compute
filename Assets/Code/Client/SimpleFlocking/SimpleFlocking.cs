﻿/********************************************************************
created:    2024-04-14
author:     lixianmin

Copyright (C) - All Rights Reserved
*********************************************************************/

using System.Runtime.InteropServices;
using Unicorn;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleFlocking : MonoBehaviour
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

    [BurstCompile]
    private struct TestVisibleJobFilter : IJobFilter
    {
        [ReadOnly] public UnsafeReadonlyArray<Plane> frustumPlanes;
        [ReadOnly] public UnsafeReadonlyArray<Boid> boids;

        public bool Execute(int index)
        {
            var boid = boids[index];
            var direction = boid.direction;

            var rotation = quaternion.identity;
            if (direction.x != 0 || direction.y != 0 || direction.z != 0)
            {
                rotation = quaternion.LookRotation(direction, new float3(0, 1, 0));
            }

            var matrix = float4x4.TRS(boid.position, rotation, new float3(1, 1, 1));
            var worldCenter = math.mul(matrix, new float4(boid.localBoundsCenter, 1)).xyz;
            var isVisible = FrustumTools.TestPlanesAABB(frustumPlanes, worldCenter, boid.localBoundsSize * 0.5f);
            return isVisible;
        }
    }

    [BurstCompile]
    private struct CollectMatrixJob : IJob
    {
        [ReadOnly] public UnsafeReadonlyArray<Boid> boids;
        [ReadOnly] public NativeList<int> visibleIndices;
        [WriteOnly] public NativeList<Matrix4x4> visibleMatrices;

        public void Execute()
        {
            for (var i = 0; i < visibleIndices.Length; i++)
            {
                var index = visibleIndices[i];
                var boid = boids[index];
                var direction = boid.direction;

                var rotation = quaternion.identity;
                if (direction.x != 0 || direction.y != 0 || direction.z != 0)
                {
                    var up = new float3(0, 1, 0);
                    rotation = quaternion.LookRotation(direction, up);
                }

                var one = new float3(1, 1, 1);
                var matrix = float4x4.TRS(boid.position, rotation, one);
                visibleMatrices.Add(matrix);
            }
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

    private void OnEnable()
    {
        _kernel = new ComputeKernel(shader, "CSMain");
        _boidsBuffer = _dog.Add(new RWStructuredBuffer<Boid>("boids_buffer", Marshal.SizeOf(typeof(Boid))));

        var meshRenderer = boidPrefab.GetComponent<MeshRenderer>();
        _meshInstanced = MeshInstanced.Create(meshRenderer);

        _InitBoids(meshRenderer);
        _InitShader();

        _unsafeFrustumPlanes = _dog.Add(new UnsafeReadonlyArray<Plane>(_frustumPlane));
        _nativeVisibleIndices = _dog.Add(new NativeList<int>(boidsCount, Allocator.Persistent));
        _nativeVisibleMatrices = _dog.Add(new NativeList<Matrix4x4>(boidsCount, Allocator.Persistent));
    }

    private void _InitBoids(MeshRenderer meshRenderer)
    {
        var prefabBounds = meshRenderer.localBounds;
        var boidData = new Boid[boidsCount];
        for (var i = 0; i < boidsCount; i++)
        {
            var position = transform.position + Random.insideUnitSphere * spawnRadius;
            var matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            var localBounds = new Bounds(matrix.MultiplyPoint(prefabBounds.center), prefabBounds.size);

            boidData[i] = new Boid(position, localBounds);
        }

        _kernel.SetBuffer(_boidsBuffer, boidData);
    }

    private void _InitShader()
    {
        shader.SetFloat("rotation_speed", rotationSpeed);
        shader.SetFloat("boid_speed", boidSpeed);
        shader.SetFloat("boid_speed_variation", boidSpeedVariation);
        shader.SetVector("flock_position", target.transform.position);
        shader.SetFloat("neighbour_distance", neighbourDistance);
        shader.SetInt("boids_count", boidsCount);
    }

    private void Update()
    {
        // 完成上一帧遗留的job, 比放到LateUpdate()中要快一些
        _jobHandle.Complete();
        _meshInstanced.Render(_nativeVisibleMatrices.AsArray());

        // 进行本帧的任务
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(mainCamera, _frustumPlane);

        shader.SetFloat(TimeId, Time.time);
        shader.SetFloat(DeltaTimeId, Time.deltaTime);
        _kernel.Dispatch(boidsCount);

        var boidArray = _boidsBuffer.GetDataAsync();
        _unsafeBoids.Dispose();
        _unsafeBoids = new UnsafeReadonlyArray<Boid>(boidArray);

        _nativeVisibleIndices.Clear();
        var testVisibleHandle = new TestVisibleJobFilter
        {
            frustumPlanes = _unsafeFrustumPlanes,
            boids = _unsafeBoids,
        }.ScheduleAppend(_nativeVisibleIndices, boidArray.Length);

        _nativeVisibleMatrices.Clear();
        _jobHandle = new CollectMatrixJob
        {
            boids = _unsafeBoids,
            visibleIndices = _nativeVisibleIndices,
            visibleMatrices = _nativeVisibleMatrices,
        }.Schedule(testVisibleHandle);
    }

    private void OnDisable()
    {
        _jobHandle.Complete();
        _dog.DisposeAndClear();

        _unsafeBoids.Dispose();
    }

    private readonly DisposeDog _dog = new();
    private ComputeKernel _kernel;
    private RWStructuredBuffer<Boid> _boidsBuffer;

    private MeshInstanced _meshInstanced;
    private JobHandle _jobHandle;

    private readonly Plane[] _frustumPlane = new Plane[6];
    private UnsafeReadonlyArray<Plane> _unsafeFrustumPlanes;
    private UnsafeReadonlyArray<Boid> _unsafeBoids;
    private NativeList<int> _nativeVisibleIndices;
    private NativeList<Matrix4x4> _nativeVisibleMatrices;

    private static readonly int TimeId = Shader.PropertyToID("time");
    private static readonly int DeltaTimeId = Shader.PropertyToID("delta_time");
}